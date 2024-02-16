using System.ComponentModel.DataAnnotations;

namespace Fashion.Models
{
    public class Size
    {
        [Key]
        public int SizeID { get; set; }

        [Required]
        public string Name { get; set; }

        // Navigation properties
        public virtual ICollection<ProductSize> ProductSizes { get; set; }
    }
}
