using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Security.Claims;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.Helpers;
using Web_ban_do_thu_cong_my_nghe.ViewModels;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    public class CartController : Controller
    {
        private readonly MynghevietDbContext db;

        private const string DefaultShippingKey = "fixed";
        private const string DefaultPaymentKey = "COD";
        private const decimal TaxRate = InvoiceVM.DefaultTaxRate;
        private const decimal FixedShippingFee = 15000m;

        private static readonly Dictionary<string, (string Label, decimal Fee)> ShippingOptions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "fixed", ("Phí vận chuyển", FixedShippingFee) }
        };

        private static readonly Dictionary<string, string> PaymentOptions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "COD", "Thanh toán khi nhận hàng" },
            { "BANK", "Chuyển khoản ngân hàng" }
        };

        public CartController(MynghevietDbContext context)
        {
            db = context;
        }

        // Lấy giỏ hàng từ session
        public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();

        #region Giỏ hàng

        public async Task<IActionResult> Index()
        {
            var cartItems = Cart;
            var appliedCoupon = GetAppliedCoupon();
            var totals = CalculateCartTotals(cartItems, appliedCoupon);
            AssignCartTotalsToViewBag(totals, appliedCoupon);
            HydrateCouponMessage();
            ViewBag.AvailableCoupons = await GetAvailableCouponsAsync();
            return View(cartItems);
        }

        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);
            if (item == null)
            {
                var product = db.Products.SingleOrDefault(p => p.Id == id);
                if (product == null)
                    return Redirect("/404");

                item = new CartItem
                {
                    MaHh = product.Id,
                    TenHH = product.Name,
                    DonGia = product.Price,
                    Hinh = product.ImageUrl ?? string.Empty,
                    SoLuong = quantity
                };
                gioHang.Add(item);
            }
            else
            {
                item.SoLuong += quantity;
            }

            HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
            return RedirectToAction("Index");
        }

        public IActionResult RemoveCart(int id)
        {
            var gioHang = Cart;
            var item = gioHang.SingleOrDefault(p => p.MaHh == id);
            if (item != null)
            {
                gioHang.Remove(item);
                HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
                if (!gioHang.Any()) ClearAppliedCoupon();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            quantity = Math.Max(quantity, 1);

            var cartItems = Cart;
            var item = cartItems.SingleOrDefault(p => p.MaHh == id);
            if (item == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

            item.SoLuong = quantity;
            HttpContext.Session.Set(MySetting.CART_KEY, cartItems);

            var appliedCoupon = GetAppliedCoupon();
            var totals = CalculateCartTotals(cartItems, appliedCoupon);
            return Json(new
            {
                success = true,
                quantity = item.SoLuong,
                lineTotal = item.ThanhTien,
                subtotal = totals.Subtotal,
                shippingFee = totals.ShippingFee,
                discount = totals.Discount,
                taxAmount = totals.TaxAmount,
                totalBeforeTax = totals.TotalBeforeTax,
                totalAfterTax = totals.TotalAfterTax
            });
        }

        #endregion

        #region Coupon

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyCoupon(string couponCode)
        {
            var cartItems = Cart;
            if (!cartItems.Any())
            {
                TempData["CouponStatus"] = "Giỏ hàng trống.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(couponCode))
            {
                TempData["CouponStatus"] = "Vui lòng nhập mã giảm giá.";
                return RedirectToAction(nameof(Index));
            }

            var couponEntity = await db.DiscountCodes.SingleOrDefaultAsync(c => c.Code == couponCode.Trim().ToUpperInvariant());
            if (couponEntity == null || !couponEntity.IsActive || (couponEntity.EndDate.HasValue && couponEntity.EndDate < DateTime.UtcNow))
            {
                TempData["CouponStatus"] = "Mã giảm giá không hợp lệ hoặc hết hạn.";
                return RedirectToAction(nameof(Index));
            }

            var applied = AppliedCouponInfo.FromEntity(couponEntity);
            HttpContext.Session.Set(MySetting.CART_COUPON_KEY, applied);
            TempData["CouponStatus"] = $"Áp dụng mã {applied.Code} thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveCoupon()
        {
            ClearAppliedCoupon();
            TempData["CouponStatus"] = "Đã hủy mã giảm giá.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Checkout

        [Authorize]
        [HttpGet]
        public IActionResult Checkout()
        {
            var cartItems = Cart;
            if (!cartItems.Any())
            {
                ViewBag.EmptyMessage = "Giỏ hàng trống.";
            }

            var shippingFee = PrepareCheckoutViewData();
            var appliedCoupon = GetAppliedCoupon();
            var totals = CalculateCartTotals(cartItems, appliedCoupon, shippingFee);
            AssignCartTotalsToViewBag(totals, appliedCoupon);
            HydrateCouponMessage();

            return View(cartItems);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutVM model)
        {
            var cartItems = Cart;
            if (!cartItems.Any())
            {
                TempData["StatusMessage"] = "Giỏ hàng trống.";
                return RedirectToAction(nameof(Index));
            }

            // Chuẩn hóa phương thức vận chuyển và thanh toán
            var shippingKey = NormalizeShippingKey(model.PhuongThucVanChuyen);
            var paymentKey = NormalizePaymentKey(model.PhuongThucThanhToan);
            var shippingFee = PrepareCheckoutViewData(shippingKey, paymentKey);

            // Lấy coupon
            var appliedCoupon = await RefreshCouponFromDatabaseAsync(GetAppliedCoupon());

            // Tính tổng đơn hàng
            var totals = CalculateCartTotals(cartItems, appliedCoupon.coupon, shippingFee);

            // Xác định khách hàng hiện tại
            var customer = await ResolveCurrentCustomerAsync();
            if (customer == null)
            {
                await HttpContext.SignOutAsync();
                TempData["StatusMessage"] = "Vui lòng đăng nhập trước khi đặt hàng.";
                return RedirectToAction("DangNhap", "KhachHang", new { ReturnUrl = "/Cart/Checkout" });
            }

            // Lấy thông tin nhận hàng, ưu tiên dữ liệu nhập, fallback về thông tin user
            var recipientName = string.IsNullOrWhiteSpace(model.HoTen)
                ? (customer.Fullname ?? customer.TenDangNhap ?? "Khách hàng")
                : model.HoTen.Trim();

            var shippingAddress = string.IsNullOrWhiteSpace(model.DiaChi)
                ? customer.Address?.Trim() ?? string.Empty
                : model.DiaChi.Trim();

            var shippingPhone = string.IsNullOrWhiteSpace(model.SoDienThoai)
                ? customer.PhoneNumber?.Trim() ?? string.Empty
                : model.SoDienThoai.Trim();

            model.HoTen = recipientName;
            model.DiaChi = shippingAddress;
            model.SoDienThoai = shippingPhone;

            // Kiểm tra bắt buộc
            if (string.IsNullOrWhiteSpace(shippingAddress) || string.IsNullOrWhiteSpace(shippingPhone))
            {
                ModelState.AddModelError("", "Vui lòng cung cấp địa chỉ và số điện thoại nhận hàng.");
                AssignCartTotalsToViewBag(totals, appliedCoupon.coupon);
                HydrateCouponMessage();
                return View(cartItems);
            }

            try
            {
                // Tạo đơn hàng
                var order = new Order
                {
                    UserId = customer.Id, // ⚠ phải là Id hợp lệ trong bảng users
                    OrderDate = DateTime.Now,
                    Status = OrderStatusHelper.Pending,
                    TotalMoney = totals.TotalAfterTax,
                    ShippingAddress = shippingAddress,
                    ShippingPhone = shippingPhone,
                    Notes = BuildOrderNotes(model.Ghichu, PaymentOptions[paymentKey], ShippingOptions[shippingKey].Label, appliedCoupon.coupon, totals.Discount, totals.TaxAmount)
                };

                await db.Orders.AddAsync(order);
                await db.SaveChangesAsync();

                // Thêm chi tiết đơn hàng
                var details = cartItems.Select(item => new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.MaHh,
                    Quantity = item.SoLuong,
                    PriceAtPurchase = item.DonGia
                }).ToList();

                await db.OrderDetails.AddRangeAsync(details);

                // Cập nhật coupon nếu có
                if (appliedCoupon.coupon != null)
                {
                    var couponEntity = await db.DiscountCodes.FindAsync(appliedCoupon.coupon.CouponId);
                    if (couponEntity != null)
                    {
                        couponEntity.TimesUsed++;
                        db.DiscountCodes.Update(couponEntity);
                    }
                }

                await db.SaveChangesAsync();

                // Xóa session giỏ hàng và coupon
                HttpContext.Session.Set(MySetting.CART_KEY, new List<CartItem>());
                ClearAppliedCoupon();
                HttpContext.Session.SetString(MySetting.ORDER_SUCCESS_KEY, order.Id.ToString());

                return RedirectToAction(nameof(Success));
            }
            // CartController.cs
            catch (Exception ex)
            {
                // Lấy thông tin lỗi chi tiết từ InnerException
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "Không có chi tiết thêm";

                // Hiển thị cả lỗi chính và lỗi chi tiết ra màn hình
                ModelState.AddModelError("", $"Lỗi lưu đơn hàng: {ex.Message} -> Chi tiết: {innerMessage}");

                // Khôi phục lại dữ liệu cho View để không bị lỗi null reference khi load lại trang
                AssignCartTotalsToViewBag(totals, appliedCoupon.coupon);
                HydrateCouponMessage();
                return View(cartItems);
            }
        }



        [AllowAnonymous]
        public IActionResult Success()
        {
            var successToken = HttpContext.Session.GetString(MySetting.ORDER_SUCCESS_KEY);
            if (string.IsNullOrWhiteSpace(successToken))
            {
                return RedirectToAction("Index", "Home");
            }

            HttpContext.Session.Remove(MySetting.ORDER_SUCCESS_KEY);
            ViewBag.OrderReference = successToken;
            return View();
        }

        #endregion

        #region Helpers

        private decimal PrepareCheckoutViewData(string? shippingKey = null, string? paymentKey = null)
        {
            var shipping = NormalizeShippingKey(shippingKey);
            var payment = NormalizePaymentKey(paymentKey);

            ViewBag.ShippingOptions = ShippingOptions.Select(kvp => new { kvp.Key, kvp.Value.Label, kvp.Value.Fee }).ToList();
            ViewBag.PaymentOptions = PaymentOptions.Select(kvp => new { kvp.Key, kvp.Value }).ToList();
            ViewBag.SelectedShipping = shipping;
            ViewBag.SelectedPayment = payment;
            ViewBag.ShippingFee = ShippingOptions[shipping].Fee;
            ViewBag.TaxRate = TaxRate;
            return ShippingOptions[shipping].Fee;
        }

        private static string NormalizeShippingKey(string? key) => !string.IsNullOrWhiteSpace(key) && ShippingOptions.ContainsKey(key) ? key : DefaultShippingKey;
        private static string NormalizePaymentKey(string? key) => !string.IsNullOrWhiteSpace(key) && PaymentOptions.ContainsKey(key) ? key : DefaultPaymentKey;

        private void AssignCartTotalsToViewBag(CartTotals totals, AppliedCouponInfo? coupon)
        {
            ViewBag.CartSubtotal = totals.Subtotal;
            ViewBag.ShippingFee = totals.ShippingFee;
            ViewBag.DiscountAmount = totals.Discount;
            ViewBag.CartTax = totals.TaxAmount;
            ViewBag.CartTotalBeforeTax = totals.TotalBeforeTax;
            ViewBag.CartTotal = totals.TotalAfterTax;
            ViewBag.CouponCode = coupon?.Code ?? string.Empty;
            ViewBag.CouponDescription = coupon?.GetSummary(totals.Subtotal) ?? string.Empty;
        }

        private void HydrateCouponMessage()
        {
            if (ViewBag.CouponMessage == null && TempData.ContainsKey("CouponStatus"))
                ViewBag.CouponMessage = TempData["CouponStatus"];
        }

        private AppliedCouponInfo? GetAppliedCoupon()
        {
            var coupon = HttpContext.Session.Get<AppliedCouponInfo>(MySetting.CART_COUPON_KEY);
            if (coupon != null && !coupon.IsStillValid(DateTime.UtcNow))
            {
                ClearAppliedCoupon();
                return null;
            }
            return coupon;
        }

        private void ClearAppliedCoupon() => HttpContext.Session.Remove(MySetting.CART_COUPON_KEY);

        private async Task<(AppliedCouponInfo? coupon, string? errorMessage)> RefreshCouponFromDatabaseAsync(AppliedCouponInfo? cached)
        {
            if (cached == null) return (null, null);
            var entity = await db.DiscountCodes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cached.CouponId);
            if (entity == null || !entity.IsActive)
            {
                ClearAppliedCoupon();
                return (null, "Mã giảm giá không còn hợp lệ.");
            }
            return (AppliedCouponInfo.FromEntity(entity), null);
        }

        private async Task<User?> ResolveCurrentCustomerAsync()
        {
            var claim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == MySetting.CLAIM_CUSTOMERID);
            if (claim != null && int.TryParse(claim.Value, out var id))
                return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

            var name = HttpContext.User.Identity?.Name;
            if (!string.IsNullOrEmpty(name))
                return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.TenDangNhap == name.Trim());

            return null;
        }

        private async Task<List<DiscountCode>> GetAvailableCouponsAsync()
        {
            var now = DateTime.UtcNow;
            return await db.DiscountCodes.AsNoTracking()
                .Where(c => c.IsActive && c.StartDate <= now && (c.EndDate == null || c.EndDate >= now))
                .OrderByDescending(c => c.CreatedAt)
                .Take(6)
                .ToListAsync();
        }

        private static string BuildOrderNotes(string? note, string paymentLabel, string shippingLabel, AppliedCouponInfo? coupon, decimal discount, decimal tax)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(note)) sb.AppendLine(note.Trim());
            sb.AppendLine($"Thanh toán: {paymentLabel}");
            sb.AppendLine($"Vận chuyển: {shippingLabel}");
            if (coupon != null && discount > 0) sb.AppendLine($"Mã giảm giá: {coupon.Code} (-{discount:N0}đ)");
            if (tax > 0) sb.AppendLine($"Thuế: {tax:N0}đ");
            return sb.ToString().Trim();
        }

        private CartTotals CalculateCartTotals(List<CartItem> items, AppliedCouponInfo? coupon = null, decimal? shippingFeeOverride = null)
        {
            var subtotal = items.Sum(i => i.ThanhTien);
            var shippingFee = shippingFeeOverride ?? FixedShippingFee;
            var discount = coupon?.CalculateDiscount(subtotal) ?? 0m;
            discount = Math.Min(discount, subtotal);
            var totalBeforeTax = Math.Max(0, subtotal - discount) + shippingFee;
            var taxAmount = Math.Round(totalBeforeTax * TaxRate, 0, MidpointRounding.AwayFromZero);
            var totalAfterTax = totalBeforeTax + taxAmount;
            return new CartTotals(subtotal, shippingFee, discount, totalBeforeTax, taxAmount, totalAfterTax);
        }

        private sealed record CartTotals(decimal Subtotal, decimal ShippingFee, decimal Discount, decimal TotalBeforeTax, decimal TaxAmount, decimal TotalAfterTax);

        #endregion
    }
}
