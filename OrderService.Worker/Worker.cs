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
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Exchange ve Queue tanımları
            _channel.ExchangeDeclare("orders.exchange", ExchangeType.Direct, durable: true);
            _channel.QueueDeclare("orders.queue", durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", "retry.exchange" },
                    { "x-dead-letter-routing-key", "retry" }
                });
            _channel.QueueBind("orders.queue", "orders.exchange", "orders");

            _channel.ExchangeDeclare("retry.exchange", ExchangeType.Direct, durable: true);
            _channel.QueueDeclare("retry.queue", durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", "orders.exchange" },
                    { "x-dead-letter-routing-key", "orders" },
                    { "x-message-ttl", 5000 }
                });
            _channel.QueueBind("retry.queue", "retry.exchange", "retry");

            _channel.ExchangeDeclare("dlq.exchange", ExchangeType.Direct, durable: true);
            _channel.QueueDeclare("dlq.queue", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind("dlq.queue", "dlq.exchange", "dlq");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var order = JsonSerializer.Deserialize<Order>(message);

                int retryCount = 0;
                if (ea.BasicProperties.Headers != null &&
                    ea.BasicProperties.Headers.TryGetValue("x-retry", out var val))
                {
                    retryCount = Convert.ToInt32(Encoding.UTF8.GetString((byte[])val));
                }

                try
                {
                    if (order.Quantity <= 0)
                        throw new Exception("Quantity must be greater than 0");

                    db.Orders.Add(order);
                    await db.SaveChangesAsync();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[✔] Kaydedildi: {order.CustomerName}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    if (retryCount >= 2)
                    {
                        var dlqProps = _channel.CreateBasicProperties();
                        dlqProps.Persistent = true;
                        _channel.BasicPublish("dlq.exchange", "dlq", dlqProps, ea.Body.ToArray());
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[DLQ] Mesaj DLQ’ya gönderildi: {order.CustomerName}");
                        Console.ResetColor();
                    }
                    else
                    {
                        var retryProps = _channel.CreateBasicProperties();
                        retryProps.Persistent = true;
                        retryProps.Headers = new Dictionary<string, object>
                        {
                            { "x-retry", Encoding.UTF8.GetBytes((retryCount + 1).ToString()) }
                        };
                        _channel.BasicPublish("retry.exchange", "retry", retryProps, ea.Body.ToArray());
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[RETRY] {retryCount + 1}. kez denenecek: {order.CustomerName}");
                        Console.ResetColor();
                    }
                }
            };

            _channel.BasicConsume(queue: "orders.queue", autoAck: true, consumer: consumer);

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
