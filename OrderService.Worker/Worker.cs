using Microsoft.Extensions.Configuration;
using OrderService.Worker.Data;
using OrderService.Worker.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;


namespace OrderService.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private IConnection _connection;
        private IModel _channel;

        public Worker(IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var hostName = _configuration["RabbitMQ:HostName"] ?? "localhost";

            var factory = new ConnectionFactory() { HostName = hostName };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            //_channel.QueueDeclare(queue: "order-queue",
            //                      durable: false,
            //                      exclusive: false,
            //                      autoDelete: false,
            //                      arguments: null);

            // DLX ve DLQ Setup
            _channel.ExchangeDeclare("order_exchange", ExchangeType.Direct);
            _channel.QueueDeclare("order_queue", durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                { "x-dead-letter-exchange", "order_dl_exchange" },
                { "x-dead-letter-routing-key", "order_dl" }
                });
            _channel.QueueBind("order_queue", "order_exchange", "order");

            _channel.ExchangeDeclare("order_dl_exchange", ExchangeType.Direct);
            _channel.QueueDeclare("order_dl_queue", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind("order_dl_queue", "order_dl_exchange", "order_dl");

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var order = JsonSerializer.Deserialize<Order>(message);

                try
                {
                    db.Orders.Add(order);
                    await db.SaveChangesAsync();
                    Console.WriteLine($"[✔] Saved to DB: {order.CustomerName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] DB Error: {ex.Message}");
                }
            };

            _channel.BasicConsume(queue: "order-queue",
                                  autoAck: true,
                                  consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
