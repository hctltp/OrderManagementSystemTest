using OrderService.Api.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderService.Api.Services
{
    public class RabbitMqPublisher
    {
        private readonly IConfiguration _configuration;
        private readonly string _queueName = "order-queue";

        public RabbitMqPublisher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Publish(Order order)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost"
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: _queueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            string json = JsonSerializer.Serialize(order);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "",
                                 routingKey: _queueName,
                                 basicProperties: null,
                                 body: body);
        }
    }
}
