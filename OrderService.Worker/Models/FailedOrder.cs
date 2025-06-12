using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Worker.Models
{
    public class FailedOrder
    {
        public int Id { get; set; }
        public string Payload { get; set; }  // Ham JSON mesaj
        public string ErrorMessage { get; set; }  // Hata açıklaması
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
