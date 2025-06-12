using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Worker
{
    public class DLQWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public DLQWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"]
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            // DLQ Kuyruğunu Dinle
            channel.QueueDeclare(queue: "order_dl_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DLQ] Hatalı Mesaj Alındı: {message}");
                Console.ResetColor();

                // İstersen bu veriyi ayrı tabloya veya log dosyasına yazabilirsin.
                // Şimdilik sadece acknowledge ediyoruz.
                channel.BasicAck(ea.DeliveryTag, false);

                await Task.CompletedTask;
            };

            channel.BasicConsume(queue: "order_dl_queue", autoAck: false, consumer: consumer);

            return Task.CompletedTask;
        }
    }
}
