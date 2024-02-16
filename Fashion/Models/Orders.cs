using System.ComponentModel.DataAnnotations;

namespace Fashion.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }
        public bool OrderStatus { get; set; }
        public DateTime OrderDay { get; set; }
        public DateTime ReceiveDay { get; set; }
        public int CustomerID { get; set; }
        public bool IsChecked { get; set; }

        // Navigation properties
        public virtual Customer Customer { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
