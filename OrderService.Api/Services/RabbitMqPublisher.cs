using OrderService.Api.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderService.Api.Services
{
    public class RabbitMqPublisher
    {
        private readonly IConfiguration _configuration;

        public RabbitMqPublisher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Publish(Order order)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost"
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare("orders.exchange", ExchangeType.Direct, durable: true);

            var json = JsonSerializer.Serialize(order);
            var body = Encoding.UTF8.GetBytes(json);

            var props = channel.CreateBasicProperties();
            props.Persistent = true;

            channel.BasicPublish(
                exchange: "orders.exchange",
                routingKey: "orders",
                basicProperties: props,
                body: body
            );
        }
    }
}
