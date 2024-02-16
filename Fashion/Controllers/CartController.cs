using Fashion.DAL;
using Fashion.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fashion.Controllers
{
    public class CartController : Controller
    {


        private readonly FashionShopContext _db;
        private readonly UserManager<Customer> _userManager;


        public CartController(FashionShopContext db, UserManager<Customer> userManager)
        {
            _db = db;
            _userManager = userManager;
        }


        public IActionResult ShoppingCart()
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var orderDetails = _db.OrderDetails
                                .Include(od => od.Product)
                                .ThenInclude(p => p.ProductImages)
                                .Include(od => od.Size)
                                .Where(od => od.Order.CustomerID == int.Parse(customerId) && !od.Order.IsChecked)
                                .ToList();

            var productIds = orderDetails.Select(od => od.ProductID).ToList();

            var productSizes = _db.ProductSizes
                .Include(ps => ps.Product)
                .Include(ps => ps.Size)
                .Where(ps => productIds.Contains(ps.ProductID))
                .ToList();


            var viewModel = new ShoppingCartViewModel
            {   
                OrderDetails = orderDetails,
                ProductSizes = productSizes
            };

            return View(viewModel);
        }
        [HttpPost]
        public IActionResult UpdateCart(List<int> productIds, List<int> orderIds, List<int> quantities, List<int> sizeIds)
        {
            for (int i = 0; i < productIds.Count; i++)
            {
                int productId = productIds[i];
                int orderId = orderIds[i];
                int quantity = quantities[i];
                int sizeId = sizeIds[i];

                var orderDetail = _db.OrderDetails.FirstOrDefault(od => od.ProductID == productId && od.OrderID == orderId);
                if (orderDetail != null)
                {
                    orderDetail.Quantity = quantity;
                    orderDetail.SizeID = sizeId;
                    _db.SaveChanges();
                }
            }

            return RedirectToAction("ShoppingCart");
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId, int orderId)
        {
            var orderDetail = _db.OrderDetails.FirstOrDefault(od => od.ProductID == productId && od.OrderID == orderId);
            if (orderDetail != null)
            {
                _db.OrderDetails.Remove(orderDetail);
                _db.SaveChanges();
            }

            return Json(new { success = true });
        }

        public IActionResult Checkout()
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }
            var errorMessage = TempData["ErrorMessage"] as string;

            var customer = _db.Customers.FirstOrDefault(c => c.CustomerID == int.Parse(customerId));
            if (customer == null)
            {
                return RedirectToAction("Login", "User");
            }

            var orderDetails = _db.OrderDetails
                                  .Include(od => od.Product)
                                  .Where(od => od.Order.CustomerID == int.Parse(customerId) && !od.Order.IsChecked)
                                  .ToList();

            var viewModel = new CheckoutViewModel
            {
                Customer = customer,
                OrderDetails = orderDetails,
                ErrorMessage = errorMessage
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Checkout(string address, string password)
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var customer = _db.Customers.FirstOrDefault(c => c.CustomerID == int.Parse(customerId));
            if (customer == null)
            {
                return RedirectToAction("Login", "User");
            }

            var user = await _userManager.FindByNameAsync(customer.Email);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            // Check if the provided password matches the user's password
            var passwordCorrect = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordCorrect)
            {
                ViewData["ErrorMessage"] = "Incorrect password";
                ModelState.AddModelError("password", "Incorrect password");
                var orderDetails = _db.OrderDetails
                    .Include(od => od.Product)
                    .Where(od => od.Order.CustomerID == int.Parse(customerId) && !od.Order.IsChecked)
                    .ToList();

                var viewModel = new CheckoutViewModel
                {
                    Customer = customer,
                    OrderDetails = orderDetails
                };
                return View("Checkout", viewModel);
            }

            customer.Address = address;
            _db.SaveChanges();

            var orders = _db.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.CustomerID == customer.CustomerID && !o.IsChecked)
                .ToList();

            foreach (var order in orders)
            {
                order.IsChecked = true;

                foreach (var orderDetail in order.OrderDetails)
                {
                    var product = _db.Products.FirstOrDefault(p => p.ProductID == orderDetail.ProductID);
                    if (product != null)
                    {
                        // Giảm số lượng sản phẩm chỉ khi đủ số lượng cần giảm
                        if (product.Quantity >= orderDetail.Quantity)
                        {
                            product.Quantity -= orderDetail.Quantity;
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Not enough quantity available for one or more products.";

                            // Quay trở lại trang Checkout và hiển thị thông báo lỗi
                            return RedirectToAction("Checkout", "Cart");
                        }
                    }
                }
            }

            _db.SaveChanges();

            return RedirectToAction("OrderConfirmation");
        }

        public IActionResult OrderConfirmation()
        {
            return View();
        }

    }
}
