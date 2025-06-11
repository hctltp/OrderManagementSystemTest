namespace OrderService.Api.Models
{
    public class Order
    {
        public string CustomerName { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
    }
}
