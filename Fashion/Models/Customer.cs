using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fashion.Models
{
    public class Customer : IdentityUser
    {
        [Key]
        public int CustomerID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }

        [Required]
        public string Role { get; set; }
        public string Address { get; set; }

        public virtual ICollection<Favorite_Product> Favorite_Products { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
    }
}
