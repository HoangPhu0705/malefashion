using Fashion.DAL;
using Fashion.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fashion.Controllers
{
    public class ProductController : Controller
    {

        private readonly FashionShopContext _db;

        public ProductController(FashionShopContext db)
        {
            _db = db;
        }
        public IActionResult Product_Detail(int id)
        {
            var product = _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                    .ThenInclude(ps => ps.Size) 
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }


            return View(product);
        }



        [HttpPost]
        public IActionResult AddToCart(int productId, int sizeId, int quantity)
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var customer = _db.Customers.Find(int.Parse(customerId));
            var product = _db.Products.Find(productId);

            var order = _db.Orders.FirstOrDefault(o => o.CustomerID == customer.CustomerID && !o.IsChecked);
            if (order == null)
            {
                order = new Order
                {
                    CustomerID = customer.CustomerID,
                    OrderStatus = false,
                    OrderDay = DateTime.Now,
                    ReceiveDay = DateTime.Now.AddDays(5),
                    IsChecked = false
                };
                _db.Orders.Add(order);
                _db.SaveChanges();
            }

            var orderDetail = _db.OrderDetails
               .Include(od => od.Order)
               .FirstOrDefault(od => od.OrderID == order.OrderID && od.ProductID == product.ProductID);

            if (orderDetail == null)
            {
                // If the product is not in the order details, add a new OrderDetail with the selected sizeId and quantity
                orderDetail = new OrderDetail
                {
                    OrderID = order.OrderID,
                    ProductID = product.ProductID,
                    Quantity = quantity,
                    Price = product.Price,
                    SizeID = sizeId
                };
                _db.OrderDetails.Add(orderDetail);
            }
            else
            {
                // If the product is already in the order details, increase the quantity
                orderDetail.Quantity += quantity;
                orderDetail.Price *= orderDetail.Quantity;
            }

            _db.SaveChanges();

            ViewBag.SuccessMessage = "Product has been added successfully.";

            return RedirectToAction("Product_Detail", new { id = productId });
        }


    }
}
