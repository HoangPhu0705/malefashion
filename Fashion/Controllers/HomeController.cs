using Fashion.DAL;
using Fashion.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using System;
using System.Diagnostics;

namespace Fashion.Controllers
{
    public class HomeController : Controller
    {

		private readonly FashionShopContext _db;
		
		public HomeController(FashionShopContext db)
		{
			_db = db;
		}

        public IActionResult Index()
        {
            var hotSales = _db.OrderDetails
               .Include(od => od.Product)
               .ThenInclude(p => p.ProductImages)
               .GroupBy(od => od.Product.ProductID)
               .OrderByDescending(g => g.Count())
               .Take(4)
               .Select(g => g.First().Product)
               .ToList();

           
            var newArrivals = _db.Products
                .OrderByDescending(p => p.ProductID)
                .Take(5) 
                .Include(p => p.ProductImages)
                .ToList();

            var viewModel = new HomeViewModel
            {
                HotSales = hotSales,
                NewArrivals = newArrivals
            };

            return View(viewModel);
        }


        public IActionResult Shop(int page = 1, int? categoryId = null, int? brandId = null, int? minPrice = null, int? maxPrice = null, string search = null)
        {
            int pageSize = 12;
            int skip = (page - 1) * pageSize;

            var query = _db.Products.AsQueryable();




            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandID == brandId.Value);
            }


            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.ProductName.Contains(search));
            }


            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.ProductName.Contains(search));
            }



            var products = query
                    .OrderBy(p => p.ProductID)
                    .Skip(skip)
                    .Take(pageSize)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductImages)
                    .ToList();

            var categories = _db.Categories
                .Include(c => c.Products)
                .Select(c => new CategoryViewModel
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    ProductCount = c.Products.Count()
                })
                .ToList();

            var brands = _db.Brands.ToList();

            var filteredProducts = query.ToList();

            int totalProducts = filteredProducts.Count;
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalProduct = totalProducts;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalPages = totalPages;
            ViewBag.CategoryId = categoryId;
            ViewBag.BrandId = brandId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Search = search;

            var productImages = products.SelectMany(p => p.ProductImages).ToList();

            var viewModel = new ShopViewModel
            {
                Products = products,
                Categories = categories,
                Brands = brands,
                ProductImages = productImages,
                MinPrice = minPrice ?? 0,
                MaxPrice = maxPrice ?? 0,
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Search(string searchTerm)
        {
            var products = _db.Products
                .Where(p => p.ProductName.Contains(searchTerm))
                .ToList();

            return View("ProductSearch", products);
        }
        [HttpPost]
        public ActionResult RemoveFromFavorites(int productId)
        {
            // Tìm và xóa sản phẩm yêu thích từ cơ sở dữ liệu
            var favoriteProduct = _db.Favorite_Products.FirstOrDefault(fp => fp.ProductID == productId);
            if (favoriteProduct != null)
            {
                _db.Favorite_Products.Remove(favoriteProduct);
                _db.SaveChanges();
            }

            return Json(new { success = true });
        }
        [HttpGet]
        public IActionResult ViewFavorites(int page = 1)
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var pageSize = 12; 

            var favoriteProducts = _db.Favorite_Products
                .Where(fp => fp.CustomerID == int.Parse(customerId))
                .Include(fp => fp.Product)
                .Include(fp => fp.Product.ProductImages)
                .ToList();

            var totalItems = favoriteProducts.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var paginatedFavoriteProducts = favoriteProducts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(paginatedFavoriteProducts);
        }
        [HttpPost]
        public IActionResult Favorites(int productId)
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }
            var favoriteProduct = new Favorite_Product
            {
                ProductID = productId,
                CustomerID = int.Parse(customerId)
            };

            _db.Favorite_Products.Add(favoriteProduct);
            _db.SaveChanges();
            return RedirectToAction("Shop");
        }


        [HttpPost]
        public IActionResult AddToCart(int productId)
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



            var defaultSizeId = _db.ProductSizes
                .Where(ps => ps.ProductID == productId)
                .Select(ps => ps.SizeID)
                .FirstOrDefault();


            var orderDetail = _db.OrderDetails.FirstOrDefault(od => od.OrderID == order.OrderID && od.ProductID == product.ProductID);
            if (orderDetail == null)
            {
                // If the product is not in the order details, add a new OrderDetail with the default sizeID
                orderDetail = new OrderDetail
                {
                    OrderID = order.OrderID,
                    ProductID = product.ProductID,
                    Quantity = 1,
                    Price = product.Price,
                    SizeID = defaultSizeId
                };
                _db.OrderDetails.Add(orderDetail);
            }
            else
            {
                // If the product is already in the order details, increase the quantity
                orderDetail.Quantity += 1;
                orderDetail.Price *= orderDetail.Quantity;
            }

            _db.SaveChanges();

            return RedirectToAction("Shop");
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Blog()
        {
            return View();
        }

        public IActionResult Delivery()
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }


            var orders = _db.Orders
                .Where(o => o.CustomerID == int.Parse(customerId) && o.IsChecked)
                .Include(o => o.OrderDetails)
                .ThenInclude(p => p.Product)
                .ToList();

            return View(orders);

        }
    }
}