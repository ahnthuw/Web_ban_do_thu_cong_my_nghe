using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Web_ban_do_thu_cong_my_nghe.ViewModels;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    public class AdminController : Controller
    {
        private readonly MynghevietDbContext _db;
        private const string SharedLoginViewPath = "~/Views/KhachHang/DangNhap.cshtml";

        public AdminController(MynghevietDbContext context)
        {
            _db = context;
        }

        

        [HttpGet]
        [AllowAnonymous] 
        public IActionResult Login()
        {
            ConfigureAdminLoginView();
            return View(SharedLoginViewPath, new LoginVM());
        }


        [HttpPost]
        [AllowAnonymous] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            ConfigureAdminLoginView();
            if (!ModelState.IsValid)
                return View(SharedLoginViewPath, model);

            var admin = _db.Users.SingleOrDefault(u =>
              u.TenDangNhap.Trim() == model.TenDangNhap.Trim() && u.Role.Trim() == "Admin");

            if (admin == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại hoặc không phải admin.");
                return View(SharedLoginViewPath, model);
            }

            
            if (admin.Password != model.Password)
            {
                ModelState.AddModelError("", "Sai mật khẩu.");
                return View(SharedLoginViewPath, model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Fullname ?? admin.TenDangNhap ?? string.Empty),
                new Claim(MySetting.CLAIM_CUSTOMERID, admin.Id.ToString()),
                new Claim("AdminId", admin.Id.ToString()), 
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            HttpContext.Session.SetInt32(MySetting.SESSION_USER_ID_KEY, admin.Id);


            
            return RedirectToAction(nameof(Dashboard));
        }


        [HttpGet]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Remove(MySetting.SESSION_USER_ID_KEY);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("DangNhap", "KhachHang");
        }

        
        [Authorize(Roles = "Admin")] 
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Users()
        {
            var users = await _db.Users.ToListAsync();
            return View(users);
        }
        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            ViewData["Subtitle"] = "Tổng quan hoạt động";

            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            var totalUsers = await _db.Users.CountAsync();
            var totalOrders = await _db.Orders.CountAsync();
            var totalRevenue = await _db.Orders.SumAsync(o => (decimal?)o.TotalMoney) ?? 0m;
            var revenueToday = await _db.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == today)
                .SumAsync(o => (decimal?)o.TotalMoney) ?? 0m;
            var revenueMonth = await _db.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value >= monthStart && o.OrderDate.Value < monthStart.AddMonths(1))
                .SumAsync(o => (decimal?)o.TotalMoney) ?? 0m;
            var revenueYear = await _db.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value >= yearStart && o.OrderDate.Value < yearStart.AddYears(1))
                .SumAsync(o => (decimal?)o.TotalMoney) ?? 0m;

            var inventory = await _db.Products.SumAsync(p => (int?)p.Stock) ?? 0;
            var unitsSold = await _db.OrderDetails.SumAsync(od => (int?)od.Quantity) ?? 0;
            var productCount = await _db.Products.CountAsync();
            var categoryCount = await _db.Categories.CountAsync();
            var staffCount = await _db.Users.CountAsync(u => u.Role != null && u.Role.Trim().ToLower() == "staff");
            var customerCount = await _db.Users.CountAsync(u => u.Role == "Customer");
            var pendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatusHelper.Pending);
            var shippingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatusHelper.Shipping);

            var recentOrders = await _db.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.Id)
                .Take(5)
                .ToListAsync();

            var topProducts = await _db.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.Product != null)
                .GroupBy(od => new { od.Product!.Id, od.Product.Name, od.Product.Stock })
                .Select(g => new TopProductVM
                {
                    Name = g.Key.Name,
                    Stock = g.Key.Stock,
                    Quantity = g.Sum(x => x.Quantity),
                    UnitsSold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.PriceAtPurchase * x.Quantity)
                })
                .OrderByDescending(tp => tp.Quantity)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardVM
            {
                TotalUsers = totalUsers,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                RevenueToday = revenueToday,
                RevenueThisMonth = revenueMonth,
                RevenueThisYear = revenueYear,
                ProductCount = productCount,
                CategoryCount = categoryCount,
                InventoryInStock = inventory,
                UnitsSold = unitsSold,
                StaffCount = staffCount,
                CustomerCount = customerCount,
                PendingOrders = pendingOrders,
                ShippingOrders = shippingOrders,
                RecentOrders = recentOrders,
                TopProducts = topProducts
            };

            return View(model);
        }

        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditRole(int? id) 
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user); 
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditRole(int id, [FromForm] string role)
        {
            var userToUpdate = await _db.Users.FindAsync(id);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            
            userToUpdate.Role = role;

            _db.Update(userToUpdate);
            await _db.SaveChangesAsync();
            //mmmm
            
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LichSuDonHang()
        {
            await EnsureTrangThaiSeededAsync();
            var orders = await _db.Orders
                .OrderByDescending(o => o.Id)
                .Include(o => o.User)
                .ToListAsync();

            ViewBag.StatusOptions = OrderStatusHelper.AllStatuses;
            return View(orders);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(int id, int status)
        {
            if (!OrderStatusHelper.IsValid(status))
            {
                TempData["StatusMessage"] = "Vui lòng chọn trạng thái hợp lệ.";
                return RedirectToAction(nameof(LichSuDonHang));
            }

            await EnsureTrangThaiSeededAsync();

            var order = await _db.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            _db.Orders.Update(order);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Cập nhật trạng thái thành công.";
            return RedirectToAction(nameof(LichSuDonHang), new { highlight = id });
        }

        private async Task EnsureTrangThaiSeededAsync()
        {
            var knownStatuses = OrderStatusHelper.AllStatuses;
            var existingKeys = await _db.TrangThais.Select(t => t.MaTrangThai).ToListAsync();
            var missingKeys = knownStatuses.Keys.Except(existingKeys).ToList();

            if (!missingKeys.Any())
            {
                return;
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT TrangThai ON");

            foreach (var key in missingKeys)
            {
                _db.TrangThais.Add(new TrangThai
                {
                    MaTrangThai = key,
                    TenTrangThai = knownStatuses[key],
                    MoTa = $"Trạng thái {knownStatuses[key]}"
                });
            }

            await _db.SaveChangesAsync();
            await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT TrangThai OFF");
            await transaction.CommitAsync();
        }

        private void ConfigureAdminLoginView()
        {
            ViewBag.LoginTitle = "Đăng nhập quản trị";
            ViewBag.LoginDescription = "Dành cho tài khoản Admin/Staff";
            ViewBag.FormAction = nameof(Login);
            ViewBag.FormController = nameof(AdminController).Replace("Controller", string.Empty);
            ViewBag.ShowForgot = false;
        }
    }
}
