using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Fashion.Models
{
    public class OrderDetail
    {
        [Key, Column(Order = 0)]
        public int ProductID { get; set; }
        [Key, Column(Order = 1)]
        public int OrderID { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }

        public int SizeID { get; set; } 
        // Navigation properties
        public virtual Product Product { get; set; }
        public virtual Order Order { get; set; }
        public virtual Size Size { get; set; } // Thêm thuộc tính Size để tạo quan hệ với bảng Size
    }
}