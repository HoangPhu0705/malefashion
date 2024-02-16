using System.Collections.Generic;

namespace Fashion.Models
{
    public class WishlistViewModel
    {
        public int CustomerId { get; set; }
        public List<Product> Products { get; set; }
    }
}