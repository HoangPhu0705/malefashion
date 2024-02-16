namespace Fashion.Models
{
    public class CheckoutViewModel
    {
        public Customer Customer { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
        public string ErrorMessage { get; set; }
    }
}
