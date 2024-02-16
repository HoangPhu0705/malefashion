using System.ComponentModel.DataAnnotations;

namespace Fashion.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; }
    }
}
