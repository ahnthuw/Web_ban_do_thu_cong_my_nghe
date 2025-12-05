using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.Helpers;
using Web_ban_do_thu_cong_my_nghe.ViewModels;
using Web_ban_do_thu_cong_my_nghe.ViewModels.Account;
using Web_ban_do_thu_cong_my_nghe.ViewModels.Contact;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    [Authorize(Roles = "Customer")]
    public class KhachHangController : Controller
    {
        private readonly MynghevietDbContext db;
        private readonly IMapper _mapper;

        public KhachHangController(MynghevietDbContext context, IMapper mapper)
        {
            db = context;
            _mapper = mapper;
        }

        #region Register

        
        [AllowAnonymous]
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [AllowAnonymous] 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKy(RegisterVM model)
        {
            if (ModelState.IsValid)
            {

                var existingUser = db.Users.SingleOrDefault(u => u.TenDangNhap == model.TenDangNhap || u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc email đã tồn tại.");
                    return View(model);
                }

                var randomKeyMoi = MyUtil.GenerateRamdomKey();

                var user = new User
                {
                    Fullname = model.HoTen,
                    Email = model.Email,
                    TenDangNhap = model.TenDangNhap,
                    Password = model.MatKhau.ToMd5Hash(randomKeyMoi),
                    RandomKey = randomKeyMoi,
                    Status = true,
                    Role = "Customer", // << TỐT!
                    Gender = model.GioiTinh,
                    NgaySinh = model.NgaySinh,
                    Address = model.DiaChi,
                    PhoneNumber = model.DienThoai,
                    Hinh = model.Hinh,
                    CreatedAt = DateTime.Now
                };

                db.Users.Add(user);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Đăng ký thành công, mời đăng nhập!";
                return RedirectToAction("DangNhap", "KhachHang");
            }

            return View(model);
        }

        #endregion


        #region Contact

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> LienHe()
        {
            var customerIdValue = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerIdValue))
            {
                return RedirectToAction(nameof(DangNhap));
            }

            var admin = await GetPrimaryAdminAsync();
            if (admin == null)
            {
                TempData["StatusMessage"] = "Hiện chưa có quản trị viên trực tuyến để hỗ trợ.";
                return View(new ContactConversationVM { PartnerName = "Quản trị viên" });
            }

            var conversation = await BuildConversationAsync(int.Parse(customerIdValue), admin.Id, viewerIsAdmin: false);
            return View(conversation);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LienHe([Bind(Prefix = "Composer")] ContactSendVM input)
        {
            var customerIdValue = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerIdValue))
            {
                return RedirectToAction(nameof(DangNhap));
            }

            var admin = await GetPrimaryAdminAsync();
            if (admin == null)
            {
                TempData["StatusMessage"] = "Hiện chưa có quản trị viên trực tuyến để hỗ trợ.";
                return RedirectToAction(nameof(LienHe));
            }

            if (!ModelState.IsValid)
            {
                var conversation = await BuildConversationAsync(int.Parse(customerIdValue), admin.Id, viewerIsAdmin: false);
                conversation.Composer = input;
                return View(conversation);
            }

            var message = new ContactMessage
            {
                SenderId = int.Parse(customerIdValue),
                ReceiverId = admin.Id,
                Message = input.Message.Trim(),
                SentAt = DateTime.UtcNow,
                IsAdminMessage = false
            };

            db.ContactMessages.Add(message);
            await db.SaveChangesAsync();

            TempData["StatusMessage"] = "Đã gửi tin nhắn đến quản trị viên.";
            return RedirectToAction(nameof(LienHe));
        }

        #endregion


        #region Login

        [AllowAnonymous]
        [HttpGet]
        public IActionResult DangNhap(string? ReturnUrl)
        {
            // Cấu hình view login
            ViewBag.LoginTitle = "Đăng nhập tài khoản";
            ViewBag.FormAction = nameof(DangNhap);
            ViewBag.FormController = "KhachHang";
            ViewBag.ShowForgot = true;
            ViewBag.ReturnUrl = ReturnUrl;

            return View(new LoginVM());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(LoginVM model, string? ReturnUrl)
        {
            // Cấu hình view login
            ViewBag.LoginTitle = "Đăng nhập tài khoản";
            ViewBag.FormAction = nameof(DangNhap);
            ViewBag.FormController = "KhachHang";
            ViewBag.ShowForgot = true;
            ViewBag.ReturnUrl = ReturnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // Tìm user theo tên đăng nhập
            var user = await db.Users.SingleOrDefaultAsync(u => u.TenDangNhap == model.TenDangNhap);
            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (!user.Status)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin.");
                return View(model);
            }

            var normalizedRole = NormalizeRole(user.Role);
            if (!IsPasswordValid(user, model.Password, normalizedRole))
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            // Tạo claim với đầy đủ role cho cả khách hàng và admin
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.TenDangNhap ?? user.Fullname ?? string.Empty),
                new Claim(MySetting.CLAIM_CUSTOMERID, user.Id.ToString()),
                new Claim(ClaimTypes.Role, normalizedRole)
            };

            if (string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim("AdminId", user.Id.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Đăng nhập
            await HttpContext.SignInAsync(principal);

            // Lưu session userId
            HttpContext.Session.SetInt32(MySetting.SESSION_USER_ID_KEY, user.Id);

            // Redirect sau khi login
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return Redirect(ReturnUrl);

            return RedirectAfterLogin(normalizedRole, ReturnUrl);
        }

        #endregion


        #region Forgot Password

        [AllowAnonymous]
        [HttpGet]
        public IActionResult QuenMatKhau()
        {
            ViewData["Title"] = "Quên mật khẩu";
            return View(new ForgotPasswordVM());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuenMatKhau(ForgotPasswordVM model)
        {
            ViewData["Title"] = "Quên mật khẩu";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await db.Users.SingleOrDefaultAsync(u => u.TenDangNhap == model.TenDangNhap);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản với tên đăng nhập đã nhập.");
                return View(model);
            }

            var providedPhone = (model.SoDienThoai ?? string.Empty).Trim();
            var providedEmail = (model.Email ?? string.Empty).Trim();
            var phoneMatches = string.Equals((user.PhoneNumber ?? string.Empty).Trim(), providedPhone, StringComparison.OrdinalIgnoreCase);
            var emailMatches = string.Equals((user.Email ?? string.Empty).Trim(), providedEmail, StringComparison.OrdinalIgnoreCase);

            if (!phoneMatches || !emailMatches)
            {
                ModelState.AddModelError(string.Empty, "Thông tin xác minh không khớp. Vui lòng kiểm tra lại số điện thoại và email.");
                return View(model);
            }

            var resetToken = MyUtil.GenerateRamdomKey(32);
            HttpContext.Session.SetInt32(MySetting.PASSWORD_RESET_USER_KEY, user.Id);
            HttpContext.Session.SetString(MySetting.PASSWORD_RESET_TOKEN_KEY, resetToken);

            TempData["StatusMessage"] = "Xác thực thành công. Vui lòng đặt lại mật khẩu mới.";
            return RedirectToAction(nameof(ResetMatKhau));
        }

        #endregion

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ResetMatKhau()
        {
            ViewData["Title"] = "Đặt lại mật khẩu";

            var userId = HttpContext.Session.GetInt32(MySetting.PASSWORD_RESET_USER_KEY);
            var token = HttpContext.Session.GetString(MySetting.PASSWORD_RESET_TOKEN_KEY);
            if (userId == null || string.IsNullOrEmpty(token))
            {
                TempData["StatusMessage"] = "Phiên xác thực đã hết hạn. Vui lòng xác thực lại thông tin.";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            var user = await db.Users.FindAsync(userId.Value);
            if (user == null)
            {
                ClearPasswordResetSession();
                TempData["StatusMessage"] = "Không tìm thấy tài khoản yêu cầu đặt lại mật khẩu.";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            ViewBag.Username = user.TenDangNhap ?? user.Email ?? user.Fullname;
            return View(new ResetPasswordVM());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetMatKhau(ResetPasswordVM model)
        {
            ViewData["Title"] = "Đặt lại mật khẩu";

            var userId = HttpContext.Session.GetInt32(MySetting.PASSWORD_RESET_USER_KEY);
            var token = HttpContext.Session.GetString(MySetting.PASSWORD_RESET_TOKEN_KEY);
            if (userId == null || string.IsNullOrEmpty(token))
            {
                TempData["StatusMessage"] = "Phiên đặt lại mật khẩu đã hết hạn. Vui lòng xác thực lại thông tin.";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await db.Users.FindAsync(userId.Value);
            if (user == null)
            {
                ClearPasswordResetSession();
                TempData["StatusMessage"] = "Không tìm thấy tài khoản. Vui lòng thử lại.";
                return RedirectToAction(nameof(QuenMatKhau));
            }

            var newRandomKey = MyUtil.GenerateRamdomKey();
            user.RandomKey = newRandomKey;
            user.Password = model.MatKhauMoi.ToMd5Hash(newRandomKey);

            db.Users.Update(user);
            await db.SaveChangesAsync();

            ClearPasswordResetSession();
            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Mời bạn đăng nhập.";
            return RedirectToAction(nameof(DangNhap));
        }


        private void ConfigureLoginView(string title, string actionName, string controllerName, bool showForgot, string? returnUrl = null, string? description = null)
        {
            ViewBag.LoginTitle = title;
            ViewBag.LoginDescription = description;
            ViewBag.FormAction = actionName;
            ViewBag.FormController = controllerName;
            ViewBag.ShowForgot = showForgot;
            ViewBag.ReturnUrl = returnUrl;
        }

        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return "Customer";
            }

            return role.Trim();
        }

        private static bool IsPasswordValid(User user, string providedPassword, string normalizedRole)
        {
            var hashedInput = providedPassword.ToMd5Hash(user.RandomKey);
            if (string.Equals(user.Password, hashedInput, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(user.Password, providedPassword, StringComparison.Ordinal);
            }

            return false;
        }

        private IActionResult RedirectAfterLogin(string normalizedRole, string? returnUrl)
        {
            if (string.Equals(normalizedRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("/");
        }

        private void ClearPasswordResetSession()
        {
            HttpContext.Session.Remove(MySetting.PASSWORD_RESET_USER_KEY);
            HttpContext.Session.Remove(MySetting.PASSWORD_RESET_TOKEN_KEY);
        }

        private async Task<User?> GetPrimaryAdminAsync()
        {
            return await db.Users
                .Where(u => u.Role != null && u.Role.Trim().ToLower() == "admin")
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<ContactConversationVM> BuildConversationAsync(int viewerId, int partnerId, bool viewerIsAdmin)
        {
            var partner = await db.Users.FindAsync(partnerId);
            var messages = await db.ContactMessages
                .AsNoTracking()
                .Where(m => (m.SenderId == viewerId && m.ReceiverId == partnerId) || (m.SenderId == partnerId && m.ReceiverId == viewerId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var conversation = new ContactConversationVM
            {
                PartnerId = partnerId,
                PartnerName = partner?.Fullname ?? partner?.TenDangNhap ?? partner?.Email ?? "Quản trị viên",
                PartnerRoleLabel = viewerIsAdmin ? "Khách hàng" : "Quản trị viên",
                Messages = messages.Select(m => new ContactMessageVM
                {
                    Id = m.Id,
                    Content = m.Message,
                    SentAt = m.SentAt,
                    IsOwn = viewerIsAdmin ? m.IsAdminMessage : !m.IsAdminMessage,
                    IsAdminMessage = m.IsAdminMessage
                }).ToList(),
                Composer = new ContactSendVM()
            };

            return conversation;
        }

       
        public async Task<IActionResult> Profile()
        {
            
            var customerId = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);

         
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            
            var customer = await db.Users.FindAsync(int.Parse(customerId));

            if (customer == null)
            {
                return NotFound();
            }

            
            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var customerId = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            var customer = await db.Users.FindAsync(int.Parse(customerId));
            if (customer == null)
            {
                return NotFound();
            }

            var model = new EditProfileVM
            {
                HoTen = customer.Fullname,
                Email = customer.Email,
                SoDienThoai = customer.PhoneNumber,
                DiaChi = customer.Address,
                NgaySinh = customer.NgaySinh,
                GioiTinh = customer.Gender
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileVM model)
        {
            var customerId = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = await db.Users.FindAsync(int.Parse(customerId));
            if (customer == null)
            {
                return NotFound();
            }

            var emailExists = await db.Users
                .AnyAsync(u => u.Email == model.Email && u.Id != customer.Id);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng bởi tài khoản khác.");
                return View(model);
            }

            customer.Fullname = model.HoTen;
            customer.Email = model.Email;
            customer.PhoneNumber = model.SoDienThoai;
            customer.Address = model.DiaChi;
            customer.NgaySinh = model.NgaySinh;
            customer.Gender = model.GioiTinh;

            db.Users.Update(customer);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            var customerId = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            return View(new ChangePasswordVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            var customerId = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = await db.Users.FindAsync(int.Parse(customerId));
            if (customer == null)
            {
                return NotFound();
            }

            var currentHash = model.MatKhauHienTai.ToMd5Hash(customer.RandomKey);
            if (!string.Equals(currentHash, customer.Password, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            var newRandomKey = MyUtil.GenerateRamdomKey();
            customer.RandomKey = newRandomKey;
            customer.Password = model.MatKhauMoi.ToMd5Hash(newRandomKey);

            db.Users.Update(customer);
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> Account(string? tab)
        {
            var customerIdValue = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerIdValue) || !int.TryParse(customerIdValue, out var customerId))
            {
                return RedirectToAction("DangNhap");
            }

            var customer = await db.Users.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound();
            }

            var ordersQuery = db.Orders.Where(o => o.UserId == customerId);
            var totalOrders = await ordersQuery.CountAsync();
            var totalSpent = await ordersQuery.SumAsync(o => (decimal?)o.TotalMoney) ?? 0m;

            var favoriteProducts = await db.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order != null && od.Order.UserId == customerId && od.Product != null)
                .GroupBy(od => od.Product!)
                .Select(g => new FavoriteItemVM
                {
                    ProductId = g.Key.Id,
                    Name = g.Key.Name,
                    Price = g.Key.Price,
                    ImageUrl = g.Key.ImageUrl ?? string.Empty,
                    TimesPurchased = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(f => f.TimesPurchased)
                .Take(8)
                .ToListAsync();

            var recentNotifications = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .Take(8)
                .Select(o => new NotificationVM
                {
                    Title = $"Đơn hàng #{o.Id}",
                    Message = $"Trạng thái hiện tại: {OrderStatusHelper.GetLabel(o.Status)}",
                    CreatedAt = o.OrderDate ?? DateTime.Now,
                    Status = OrderStatusHelper.GetLabel(o.Status),
                    StatusCode = OrderStatusHelper.Normalize(o.Status)
                })
                .ToListAsync();

            var viewModel = new AccountDashboardVM
            {
                User = new AccountUserSummaryVM
                {
                    Id = customer.Id,
                    Fullname = customer.Fullname,
                    Email = customer.Email ?? string.Empty,
                    PhoneNumber = customer.PhoneNumber ?? string.Empty,
                    Address = customer.Address ?? string.Empty,
                    AvatarUrl = customer.Hinh ?? string.Empty,
                    Role = customer.Role ?? string.Empty
                },
                Favorites = favoriteProducts,
                Notifications = recentNotifications,
                Settings = new AccountSettingsVM
                {
                    Fullname = customer.Fullname,
                    Email = customer.Email ?? string.Empty,
                    PhoneNumber = customer.PhoneNumber ?? string.Empty,
                    Address = customer.Address ?? string.Empty,
                    ReceiveEmail = true,
                    ReceiveSms = !string.IsNullOrEmpty(customer.PhoneNumber)
                },
                TotalOrders = totalOrders,
                TotalSpent = totalSpent
            };

            ViewBag.ActiveTab = string.IsNullOrWhiteSpace(tab) ? "overview" : tab.ToLowerInvariant();
            return View(viewModel);
        }

        [AllowAnonymous]
        public async Task<IActionResult> DangXuat()
        {
            HttpContext.Session.Remove(MySetting.SESSION_USER_ID_KEY);
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }


        public async Task<IActionResult> LichSuDonHang()
        {
            var customerId = User.FindFirstValue("CustomerId");
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("DangNhap");
            }

            var orders = await db.Orders
                .Where(o => o.UserId == int.Parse(customerId))
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> HoaDon(int id)
        {
            var customerIdValue = User.FindFirstValue(MySetting.CLAIM_CUSTOMERID);
            if (string.IsNullOrEmpty(customerIdValue) || !int.TryParse(customerIdValue, out var customerId))
            {
                return RedirectToAction("DangNhap");
            }

            var order = await db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == customerId);

            if (order == null)
            {
                return NotFound();
            }

            var invoice = InvoiceVM.FromOrder(order, false);

            invoice.BackUrl = Url.Action(nameof(LichSuDonHang));
            invoice.BackLabel = "Quay về lịch sử đơn hàng";

            return View("~/Views/Shared/Invoice.cshtml", invoice);
        }
    }
}





