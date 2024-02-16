namespace Fashion.Models
{
    public class ChartViewModel
    {
        public List<Order> Orders { get; set; }
        public List<Product> Products { get; set; }
        public List<Customer> Customers { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
    }
}
