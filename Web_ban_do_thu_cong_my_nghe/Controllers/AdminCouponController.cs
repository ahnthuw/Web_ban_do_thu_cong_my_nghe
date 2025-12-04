using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.ViewModels;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCouponController : Controller
    {
        private readonly MynghevietDbContext _db;

        public AdminCouponController(MynghevietDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var coupons = await _db.DiscountCodes
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            ViewData["Title"] = "Mã giảm giá";
            ViewData["Subtitle"] = "Quản lý và tạo mới mã khuyến mãi";
            return View(coupons);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new DiscountCodeCreateVM
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1),
                MinOrderValue = 0,
                UsageLimit = 0
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DiscountCodeCreateVM model)
        {
            if ((model.PercentOff ?? 0m) <= 0 && (model.AmountOff ?? 0m) <= 0)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập phần trăm hoặc số tiền giảm hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedCode = model.Code.Trim().ToUpperInvariant();
            var exists = await _db.DiscountCodes.AnyAsync(c => c.Code == normalizedCode);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Code), "Mã giảm giá đã tồn tại.");
                return View(model);
            }

            var percentValue = model.PercentOff.HasValue ? model.PercentOff.Value / 100m : (decimal?)null;

            var entity = new DiscountCode
            {
                Code = normalizedCode,
                Description = model.Description,
                PercentOff = percentValue,
                AmountOff = model.AmountOff,
                MinOrderValue = model.MinOrderValue,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                UsageLimit = model.UsageLimit,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.DiscountCodes.Add(entity);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"Đã tạo mã giảm giá {entity.Code}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var coupon = await _db.DiscountCodes.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }

            coupon.IsActive = !coupon.IsActive;
            _db.DiscountCodes.Update(coupon);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = $"Đã {(coupon.IsActive ? "kích hoạt" : "tạm dừng")} mã {coupon.Code}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
