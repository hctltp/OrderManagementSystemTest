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
        private readonly IConfiguration _configuration;

        public DLQWorker(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost"
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare("dlq.queue", durable: true, exclusive: false, autoDelete: false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DLQ] Hatalı Mesaj Alındı: {message}");
                Console.ResetColor();

                // İsteğe bağlı olarak log tablosuna veya dosyaya yazılabilir
                channel.BasicAck(ea.DeliveryTag, false);
            };

            channel.BasicConsume(queue: "dlq.queue", autoAck: false, consumer: consumer);

            return Task.CompletedTask;
        }
    }
}
