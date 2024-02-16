using System.ComponentModel.DataAnnotations;

namespace Fashion.Models
{
    public class Brand
    {
        [Key]
        public int BrandID { get; set; }
        public string BrandName { get; set; }

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; }
    }
}
