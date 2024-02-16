using System.Collections.Generic;

namespace Fashion.Models
{
    public class ShoppingCartViewModel
    {
        public List<OrderDetail> OrderDetails { get; set; }
        public List<ProductSize> ProductSizes { get; set; }
        public List<Size> Sizes { get; set; }
    }
}