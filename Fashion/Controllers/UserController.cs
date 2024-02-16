using Fashion.DAL;
using Fashion.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NuGet.Common;
using Microsoft.AspNetCore.Authorization;

namespace Fashion.Controllers
{
    public class UserController : Controller
    {

		private readonly FashionShopContext _db;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<Customer> _userManager;
        private readonly SignInManager<Customer> _signInManager;
        public UserController(FashionShopContext db, IEmailSender emailSender, UserManager<Customer> userManager, SignInManager<Customer> signInManager)
		{
			_db = db;
            _emailSender = emailSender;
            _userManager = userManager;
            _signInManager = signInManager;

        }
        public IActionResult Login()
		{
			return View();
		}

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (!String.IsNullOrEmpty(email) && !String.IsNullOrEmpty(password))
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null && await _userManager.CheckPasswordAsync(user, password))
                {
                    string role = GetUserRole(email);
                    if (role == "admin")
                    {
                        HttpContext.Session.SetString("CustomerId", user.CustomerID.ToString());
                        HttpContext.Session.SetString("CustomerEmail", user.NormalizedEmail);
                        HttpContext.Session.SetString("CustomerLastName", user.LastName);
                        return RedirectToAction("Sales", "Admin");
                    }
                    else if (role == "user")
                    {
                        HttpContext.Session.SetString("CustomerId", user.CustomerID.ToString());
                        HttpContext.Session.SetString("CustomerEmail", user.NormalizedEmail);
                        HttpContext.Session.SetString("CustomerLastName", user.LastName);
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ViewBag.ThongBao = "Wrong username or password";
                }
            }

            return View();
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


        public IActionResult Favorites() 
        {
            return View();
        }


		public IActionResult Register()
		{
			return View();
		}

        [HttpPost]
        public async Task<IActionResult> Register(CustomerRegistrationModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new Customer
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    UserName = model.Email,
                    Phone = model.Phone,
                    Email = model.Email,
                    NormalizedEmail = model.Email,
                    Role = "user",
                    Address = ""
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login", "User");
        }


		public IActionResult ForgotPassword() 
		{
			return View();			
		}



        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return View();
            }

			string e = email;
            var user = await _db.Customers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return View("ForgotPasswordConfirmation");
            }



            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var callbackUrl = Url.Action("ResetPassword", "User", new { token = token, email = email }, Request.Scheme);

            // Send the email
            await _emailSender.SendEmailAsync(
                email,
                "Reset Password",
                $"Please reset your password by <a href='{ callbackUrl }'> clicking here</a>.");

            return View("ForgotPasswordConfirmation");	
        }

        public IActionResult ResetPassword(string token, string email)
        {
            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            //Console.WriteLine(user);
            //Console.WriteLine(model.Email);

            if (user == null)
            {
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "The password and confirmation password do not match.");
                return View(model);
            }

            
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                // Password reset successful, redirect to a confirmation view
                return RedirectToAction("ResetPasswordConfirmation");
            }

            // Password reset failed, display error messages
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }



        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {

            var customerId = HttpContext.Session.GetString("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "User");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword)
        {
            var customerId = HttpContext.Session.GetString("CustomerId");

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.CustomerID.ToString() == customerId);

            if (user == null)
            {
                return NotFound();
            }

            // Verify the old password
            var passwordChangeResult = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            if (!passwordChangeResult.Succeeded)
            {
                foreach (var error in passwordChangeResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View();
            }

            return RedirectToAction("ChangePasswordSuccess");
        }


        public IActionResult ChangePasswordSuccess()
        {
            return View();
        }










    }
}
