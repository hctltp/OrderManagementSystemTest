using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Models;
using OrderService.Api.Services;

namespace OrderService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly RabbitMqPublisher _publisher;

        public OrderController(RabbitMqPublisher publisher)
        {
            _publisher = publisher;
        }

        [HttpPost]
        public IActionResult PostOrder([FromBody] Order order)
        {
            _publisher.Publish(order);
            return Ok(new { message = "Order sent to queue!" });
        }
    }
}
