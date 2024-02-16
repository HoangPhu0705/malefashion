using System.ComponentModel.DataAnnotations;

namespace Fashion.Models
{
    public class ProductSize
    {
        // Foreign keys
        [Key]
        public int ProductID { get; set; }
        [Key]
        public int SizeID { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; }
        public virtual Size Size { get; set; }
    }
}