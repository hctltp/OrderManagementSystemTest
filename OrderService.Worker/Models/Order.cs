using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Worker.Models
{
    public class Order
    {
        public string CustomerName { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
    }
}
