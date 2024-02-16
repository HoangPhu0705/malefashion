using Fashion.DAL;
using Fashion.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Fashion.Controllers
{
    public class AdminController : Controller
    {
        private readonly FashionShopContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<Customer> _userManager;
        public AdminController(FashionShopContext db, IWebHostEnvironment webHostEnvironment, UserManager<Customer> userManager)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        public IActionResult Sales()
        {
            var customerId = HttpContext.Session.GetString("CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var user = _userManager.Users.FirstOrDefault(u => u.CustomerID.ToString() == customerId);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (user.Role != "admin")
            {
                return RedirectToAction("Login", "User");
            }
            var viewModel = new ChartViewModel();
            // Retrieve the list of orders from your data source
            viewModel.Orders = _db.Orders.ToList();
            viewModel.Products = _db.Products.ToList();
            viewModel.Customers = _db.Customers.Where(customer => customer.Role == "user").ToList();
            var orderIds = viewModel.Orders
                        .Where(order => order.OrderStatus && order.IsChecked)
                        .Select(order => order.OrderID)
                        .ToList();
            viewModel.OrderDetails = _db.OrderDetails
                .Where(detail => orderIds.Contains(detail.OrderID))
                .ToList();
            // Calculate the total number of products from checked orders
            int totalSales = viewModel.OrderDetails.Sum(detail => detail.Quantity);
            int incomes = viewModel.OrderDetails.Sum(detail => detail.Price);
            // Calculate the total number of orders
            int totalOrders = viewModel.Orders.Count;
            int totalProducts = viewModel.Products.Count;
            int totalCustomers = viewModel.Customers.Count;
            ViewBag.TotalOrders = totalOrders; // Pass the total orders to the view
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalSales = totalSales;
            ViewBag.TotalIncomes = incomes * 1.08;
            var topCustomers = viewModel.Customers
                        .OrderByDescending(customer =>
                            customer.Orders.Where(order => order.OrderStatus).Sum(order =>
                                order.OrderDetails.Sum(detail =>
                                    (detail.Quantity) * (detail.Price)
                                )
                            )
                        )
                        .ToList();

            if (topCustomers.Count > 6)
            {
                topCustomers = topCustomers.Take(6).ToList();
            }
            ViewBag.TopCustomers = topCustomers;
            var topProducts = viewModel.OrderDetails
                .GroupBy(detail => detail.Product)
                .OrderByDescending(group => group.Sum(detail => detail.Quantity))
                .Take(6)
                .Select(group => new
                {
                    Product = group.Key,
                    QuantitySold = group.Sum(detail => detail.Quantity)
                })
                .ToList();

            ViewBag.TopProducts = topProducts;
            

            var salesData = viewModel.OrderDetails
                .GroupBy(detail => detail.Order.OrderDay.Date)
                .Select(group => new
                {
                    Date = group.Key,
                    TotalSales = group.Sum(detail => detail.Quantity * detail.Price)
                })
                .OrderBy(entry => entry.Date)
                .ToList();

            ViewBag.SalesData = salesData;
            return View(viewModel);
        }

        public IActionResult Invoice()
        {
            var customerId = HttpContext.Session.GetString("CustomerId");

            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var user = _userManager.Users.FirstOrDefault(u => u.CustomerID.ToString() == customerId);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (user.Role != "admin")
            {
                return RedirectToAction("Login", "User");
            }


            var orders = _db.Orders.Include(o => o.Customer).ToList();
            return View(orders);
        }

        public string GetUserRole(string email)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                return user.Role;
            }

            return "";
        }
        [HttpPost]
        public IActionResult CheckedOrder(int orderId)
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }
            var order = _db.Orders.FirstOrDefault(o => o.OrderID == orderId);

            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatus = true;

            var orderDetails = _db.OrderDetails.Where(od => od.OrderID == orderId).ToList();
            foreach (var orderDetail in orderDetails)
            {
                var product = _db.Products.FirstOrDefault(p => p.ProductID == orderDetail.ProductID);
                if (product != null)
                {
                    orderDetail.Price = orderDetail.Quantity * product.Price;
                }
                else
                {
                    orderDetail.Price = 0;
                }
            }
            
            _db.SaveChanges();

            return RedirectToAction("Invoice");
        }
        [HttpPost]
        public IActionResult Invoice_Details(int orderID)
        {
            var order = _db.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.Product).FirstOrDefault(o => o.OrderID == orderID);
            if (order == null)
            {
                return RedirectToAction("Invoice");
            }

            var customer = _db.Customers.FirstOrDefault(c => c.CustomerID == order.CustomerID);
            if (customer == null)
            {
                return RedirectToAction("Invoice");
            }

            ViewBag.OrderDetails = order.OrderDetails.ToList();
            ViewBag.Customer = customer;
            return View(order);
        }
        public IActionResult Customer(string searchString)
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }
            var user = _userManager.Users.FirstOrDefault(u => u.CustomerID.ToString() == customerId);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (user.Role != "admin")
            {
                return RedirectToAction("Login", "User");
            }

            var customers = _db.Customers.Where(c => c.Role == "user");

            if (!string.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(c =>
                    c.FirstName.Contains(searchString) ||
                    c.LastName.Contains(searchString) ||
                    c.Phone.Contains(searchString) ||
                    c.Email.Contains(searchString) ||
                    c.Address.Contains(searchString)
                );
            }

            var filteredCustomers = customers.ToList();
            return View(filteredCustomers);
        }
        public async Task<IActionResult> Customer_Detail(int id)
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var user = _userManager.Users.FirstOrDefault(u => u.CustomerID.ToString() == customerId);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (user.Role != "admin")
            {
                return RedirectToAction("Login", "User");
            }

            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }
        public async Task<IActionResult> Product_Detail(int id)
        {

            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var user = _userManager.Users.FirstOrDefault(u => u.CustomerID.ToString() == customerId);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (user.Role != "admin")
            {
                return RedirectToAction("Login", "User");
            }
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(c => c.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            var images = await _db.ProductImages
                .Where(i => i.ProductID == id)
                .ToListAsync();

            var viewModel = new ProductViewModel
            {
                Product = product,
                Images = images
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {

            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }
            var product = await _db.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            return RedirectToAction("Product"); 
        }
        
        public IActionResult AddProduct()
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }

            var user = _userManager.Users.FirstOrDefault(u => u.CustomerID.ToString() == customerId);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (user.Role != "admin")
            {
                return RedirectToAction("Login", "User");
            }
            var model = new ProductViewModel();

            // Populate the necessary data for the view (e.g., categories, brands)
            model.Categories = _db.Categories.ToList();
            model.Brands = _db.Brands.ToList();

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> AddProduct(ProductViewModel model, List<IFormFile> productImages)
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }
            // Create a new Product object with the provided information
            var newProduct = new Product
            {
                ProductName = model.Product.ProductName,
                ProductDescription = model.Product.ProductDescription,
                Price = model.Product.Price,
                Quantity = model.Product.Quantity
            };

            // Get the Category and Brand objects based on the provided IDs
            var category = await _db.Categories.FindAsync(model.Product.CategoryID);
            var brand = await _db.Brands.FindAsync(model.Product.BrandID);

            // Check if the Category and Brand exist
            if (category == null || brand == null)
            {
                return NotFound();
            }

            // Associate the new product with the Category and Brand
            newProduct.Category = category;
            newProduct.Brand = brand;

            // Add the new product to the database
            _db.Products.Add(newProduct);
            await _db.SaveChangesAsync();

            // Process uploaded images
            foreach (var image in productImages)
            {
                if (image != null && image.Length > 0)
                {
                    // Save the image to a storage location
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img/product_img", $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}");
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    // Create a new ProductImage object and associate it with the new product
                    var newProductImage = new ProductImage
                    {
                        ImageUrl = image.FileName,
                        ProductID = newProduct.ProductID
                    };

                    // Add the new product image to the database
                    _db.ProductImages.Add(newProductImage);
                }
            }

            await _db.SaveChangesAsync();

            // Add product sizes to the ProductSize table
            var productSizes = new List<Size>();

            if (category.CategoryName == "Shoes")
            {
                // Retrieve sizes with IDs from 9 to 24
                productSizes = await _db.Sizes.Where(s => s.SizeID >= 9 && s.SizeID <= 24).ToListAsync();
            }
            else
            {
                // Retrieve sizes with IDs from 1 to 8
                productSizes = await _db.Sizes.Where(s => s.SizeID >= 1 && s.SizeID <= 8).ToListAsync();
            }

            // Associate the product with the retrieved sizes
            foreach (var size in productSizes)
            {
                var productSize = new ProductSize
                {
                    ProductID = newProduct.ProductID,
                    SizeID = size.SizeID
                };

                _db.ProductSizes.Add(productSize);
            }

            await _db.SaveChangesAsync();

            // Redirect the user to a page or action after successful addition
            return RedirectToAction("Product");
        }
        [HttpGet]
        public async Task<IActionResult> UpdateProduct(int id)
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }
            var user = _userManager.Users.FirstOrDefault(u => u.CustomerID.ToString() == customerId);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (user.Role != "admin")
            {
                return RedirectToAction("Login", "User");
            }
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(c => c.ProductID == id);
            if (product == null)
            {
                return NotFound();
            }
            var categories = await _db.Categories.ToListAsync();
            var brands = await _db.Brands.ToListAsync();
            var viewModel = new ProductViewModel
            {
                Product = product,
                Categories = categories,
                Brands = brands
            };
            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateProduct(ProductViewModel model, List<IFormFile> productImages)
        {
            

            // Find the existing product by productId
            var existingProduct = await _db.Products.Include(p => p.Category)
                                                    .Include(p => p.Brand)
                                                    .FirstOrDefaultAsync(p => p.ProductID == model.Product.ProductID);
            if (existingProduct == null)
            {
                return NotFound();
            }
            // Update the existing product with the provided information
            existingProduct.ProductName = model.Product.ProductName;
            existingProduct.ProductDescription = model.Product.ProductDescription;
            existingProduct.Price = model.Product.Price;
            existingProduct.Quantity = model.Product.Quantity;
            // Get the Category and Brand objects based on the provided IDs
            var category = await _db.Categories.FindAsync(model.Product.CategoryID);
            var brand = await _db.Brands.FindAsync(model.Product.BrandID);
            // Check if the Category and Brand exist
            if (category == null || brand == null)
            {
                return NotFound();
            }
            // Associate the existing product with the updated Category and Brand
            existingProduct.Category = category;
            existingProduct.Brand = brand;
            // Update the product in the database
            _db.Products.Update(existingProduct);
            await _db.SaveChangesAsync();
            // Process uploaded images
            foreach (var image in productImages)
            {
                if (image != null && image.Length > 0)
                {
                    // Save the image to a storage location
                    var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img/product_img", newFileName);
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    // Create a new ProductImage object and associate it with the existing product
                    var newProductImage = new ProductImage
                    {
                        ImageUrl = newFileName,
                        ProductID = existingProduct.ProductID
                    };
                    // Add the new product image to the database
                    _db.ProductImages.Add(newProductImage);
                }
            }
            await _db.SaveChangesAsync();
            // Update product sizes in the ProductSize table
            var productSizes = new List<Size>();
            if (category.CategoryName == "Shoes")
            {
                // Retrieve sizes with IDs from 9 to 24
                productSizes = await _db.Sizes.Where(s => s.SizeID >= 9 && s.SizeID <= 24).ToListAsync();
            }
            else
            {
                // Retrieve sizes with IDs from 1 to 8
                productSizes = await _db.Sizes.Where(s => s.SizeID >= 1 && s.SizeID <= 8).ToListAsync();
            }
            // Remove existing product sizes associated with the existing product

            var existingProductSizes = _db.ProductSizes.Where(ps => ps.ProductID == existingProduct.ProductID);
            _db.ProductSizes.RemoveRange(existingProductSizes);
            // Associate the existing product with the updated sizes
            foreach (var size in productSizes)
            {
                var productSize = new ProductSize
                {
                    ProductID = existingProduct.ProductID,
                    SizeID = size.SizeID
                };
                _db.ProductSizes.Add(productSize);
            }
            await _db.SaveChangesAsync();
            // Redirect the user to a page or action after successful update
            return RedirectToAction("Product");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCustomer(Customer model)
        {
            if (string.IsNullOrEmpty(model.Role) &&
                model.Orders == null &&
                model.Favorite_Products == null)
            {
                // Tìm khách hàng trong cơ sở dữ liệu
                var customer = await _db.Customers.FindAsync(model.CustomerID);

                if (customer == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin khách hàng
                customer.FirstName = model.FirstName;
                customer.LastName = model.LastName;
                customer.Phone = model.Phone;
                customer.NormalizedEmail = model.NormalizedEmail;
                customer.Address = model.Address;

                // Lưu thay đổi vào cơ sở dữ liệu
                await _db.SaveChangesAsync();

                // Chuyển hướng người dùng đến trang hoặc hành động khác sau khi cập nhật thành công
                return RedirectToAction("Customer");
            }

            // Nếu dữ liệu không hợp lệ, hiển thị form cập nhật lại cho người dùng
            return View("Customer_Detail", model);
        }



        [HttpPost]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _db.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            _db.Customers.Remove(customer);
            await _db.SaveChangesAsync();

            return RedirectToAction("Customer"); // Chuyển hướng người dùng đến trang hoặc hành động khác sau khi xóa thành công
        }


        public IActionResult Product(string searchQuery, int page = 1, int? categoryId = null, int? brandId = null)
        {
            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }
            var user = _userManager.Users.FirstOrDefault(u => u.CustomerID.ToString() == customerId);
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            if (user.Role != "admin")
            {
                return RedirectToAction("Login", "User");
            }
            int pageSize = 12;
            int skip = (page - 1) * pageSize;

            var query = _db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p => p.ProductName.Contains(searchQuery));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandID == brandId.Value);
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

            int totalProducts = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            var viewModel = new ShopViewModel
            {
                Products = products,
                Categories = categories,
                Brands = brands
            };

            return View(viewModel);
        }
    }
}
