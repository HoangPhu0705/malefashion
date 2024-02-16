using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Fashion.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }
        public int CategoryID { get; set; }
        public int BrandID { get; set; }
        [Required]
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }

        // Navigation properties
        public virtual Category Category { get; set; }
        public virtual Brand Brand { get; set; }
        public virtual ICollection<ProductImage> ProductImages { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        // Many-to-many relationship with Size
        public virtual ICollection<ProductSize> ProductSizes { get; set; }
    }
}
