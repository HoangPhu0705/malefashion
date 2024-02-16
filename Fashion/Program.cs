using Fashion.DAL;
using Fashion.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Fashion.Models;
using System.Configuration;

namespace Fashion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<FashionShopContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<Customer, IdentityRole>()
                .AddEntityFrameworkStores<FashionShopContext>()
                .AddDefaultTokenProviders()
                .AddDefaultUI();

            var mailsettings = builder.Configuration.GetSection("MailSettings") ?? throw new InvalidOperationException("mailsettings not found");
            builder.Services.AddOptions();
            builder.Services.Configure<MailSettings>(mailsettings);
            builder.Services.AddTransient<IEmailSender, SendMailService>(); 
            builder.Services.AddSession(options =>
			{
				options.IdleTimeout = TimeSpan.FromMinutes(5); 
			});
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseAuthentication();
            app.UseSession();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "resetPassword",
                pattern: "User/ResetPassword",
                defaults: new { controller = "User", action = "ResetPassword" });

            app.Run();
        }
    }
}