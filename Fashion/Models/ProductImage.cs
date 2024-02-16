using System.ComponentModel.DataAnnotations;

namespace Fashion.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageID { get; set; }
        public string ImageUrl { get; set; }
        public int ProductID { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; }
    }
}
