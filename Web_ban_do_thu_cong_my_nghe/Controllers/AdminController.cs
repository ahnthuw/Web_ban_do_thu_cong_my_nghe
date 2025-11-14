using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_ban_do_thu_cong_my_nghe.ViewModels;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    public class AdminController : Controller
    {
        private readonly MynghevietDbContext _db;

        public AdminController(MynghevietDbContext context)
        {
            _db = context;
        }

        

        [HttpGet]
        [AllowAnonymous] 
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        [AllowAnonymous] 
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var admin = _db.Users.SingleOrDefault(u =>
              u.TenDangNhap.Trim() == model.TenDangNhap.Trim() && u.Role.Trim() == "Admin");

            if (admin == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại hoặc không phải admin.");
                return View(model);
            }

            
            if (admin.Password != model.Password)
            {
                ModelState.AddModelError("", "Sai mật khẩu.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Fullname),
                new Claim("AdminId", admin.Id.ToString()), 
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);


            
            return RedirectToAction("Index", "Admin");
        }


        [HttpGet]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Admin");
        }

        
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Index()
        {
            
            var users = await _db.Users.ToListAsync();
            return View(users); 
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

            
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LichSuDonHang()
        {
            var orders = await _db.Orders
                .OrderByDescending(o => o.Id)
                .Include(o => o.User)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(int id, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                TempData["StatusMessage"] = "Vui lòng chọn trạng thái hợp lệ.";
                return RedirectToAction(nameof(LichSuDonHang));
            }

            var order = await _db.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            _db.Orders.Update(order);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Cập nhật trạng thái thành công.";
            return RedirectToAction(nameof(LichSuDonHang));
        }
    }
}
