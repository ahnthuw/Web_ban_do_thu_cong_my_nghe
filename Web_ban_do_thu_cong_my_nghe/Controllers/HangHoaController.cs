using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.ViewComponents;
using Web_ban_do_thu_cong_my_nghe.ViewModels;

namespace Web_ban_do_thu_cong_my_nghe.Controllers
{
    [AllowAnonymous]
    public class HangHoaController : Controller
    {
        private readonly MynghevietDbContext db;

        public HangHoaController(MynghevietDbContext context)
        {
            db = context;
        }
        public IActionResult Index(int? loai)
        {
            var hangHoas = db.Products.Include(p => p.Category).AsQueryable();

            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.CategoryId == loai.Value);
            }
            var discountMap = GetDiscountMap();

            var result = hangHoas
                .AsEnumerable()
                .Select(p =>
                {
                    var discount = discountMap.TryGetValue(p.Id, out var percent) ? percent : 0;
                    return new HangHoaVM
                    {
                        MaHh = p.Id,
                        TenHH = p.Name,
                        Hinh = p.ImageUrl ?? string.Empty,
                        DonGia = p.Price,
                        MoTaNgan = p.Description ?? string.Empty,
                        TenLoai = p.Category?.Name ?? string.Empty,
                        GiamGiaPhanTram = discount
                    };
                })
                .ToList();

            return View(result);
        }

        public IActionResult Search(string query, int? categoryId)
        {
            var hangHoas = db.Products.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrEmpty(query))
            {
                hangHoas = hangHoas.Where(p => p.Name.ToLower().Contains(query.ToLower()));
            }
            if (categoryId.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.CategoryId == categoryId.Value);
            }
            var discountMap = GetDiscountMap();

            var result = hangHoas
                .AsEnumerable()
                .Select(p =>
                {
                    var discount = discountMap.TryGetValue(p.Id, out var percent) ? percent : 0;
                    return new HangHoaVM
                    {
                        MaHh = p.Id,
                        TenHH = p.Name,
                        Hinh = p.ImageUrl ?? string.Empty,
                        DonGia = p.Price,
                        MoTaNgan = p.Description ?? string.Empty,
                        TenLoai = p.Category?.Name ?? string.Empty,
                        GiamGiaPhanTram = discount
                    };
                })
                .ToList();
            ViewBag.Query = query;
            ViewBag.CategoryId = categoryId;
            return View("Index", result);
        }

        public IActionResult BestSeller()
        {
            var discountMap = GetDiscountMap();

            var orderStats = db.OrderDetails
                .Where(od => od.ProductId != null)
                .GroupBy(od => od.ProductId!.Value)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(g => g.TotalQuantity)
                .Take(12)
                .ToList();

            if (!orderStats.Any())
            {
                return View(new List<HangHoaVM>());
            }

            var topProductIds = orderStats.Select(o => o.ProductId).ToList();

            var products = db.Products
                .Include(p => p.Category)
                .Where(p => topProductIds.Contains(p.Id))
                .ToList();

            var bestSellers = orderStats
                .Join(products,
                    stats => stats.ProductId,
                    product => product.Id,
                    (stats, product) =>
                    {
                        var discount = discountMap.TryGetValue(product.Id, out var percent) ? percent : 0;
                        return new HangHoaVM
                        {
                            MaHh = product.Id,
                            TenHH = product.Name,
                            Hinh = product.ImageUrl ?? string.Empty,
                            DonGia = product.Price,
                            MoTaNgan = product.Description ?? string.Empty,
                            TenLoai = product.Category?.Name ?? string.Empty,
                            SoLuongBan = stats.TotalQuantity,
                            GiamGiaPhanTram = discount
                        };
                    })
                .OrderByDescending(item => item.SoLuongBan)
                .ToList();

            return View(bestSellers);
        }
        public IActionResult Detail(int id)
        {
            var hangHoa = db.Products
                .Include(p => p.Category)
                .SingleOrDefault(p => p.Id == id);
            if(hangHoa == null)
            {
                TempData["Message"] = $"Không tìm thấy sản phẩm có mã {id}!";
                return Redirect("/404");
            }
            var dsDanhGia = new List<DanhGiaVM>
    {
        new DanhGiaVM
        {
            TenNguoiDung = "Đàm Thị Xuân Minh",
            NoiDung = "Sản phẩm này rất đẹp, chi tiết tinh xảo. Tôi rất hài lòng!",
            SoSao = 5,
            NgayDang = new DateTime(2025, 10, 27) 
        },
        new DanhGiaVM
        {
            TenNguoiDung = "Nguyễn Thị Binh Nhi",
            NoiDung = "Giao hàng nhanh, đóng gói cẩn thận. Sẽ ủng hộ shop lần sau.",
            SoSao = 4,
            NgayDang = new DateTime(2025, 10,10) 
        },
         new DanhGiaVM
        {
            TenNguoiDung = "Nguyễn Thị Thu Thảo",
            NoiDung = "Màu sắc và kiểu dáng đều hài hòa, có thể dùng trang trí nhà hoặc làm quà tặng đều sang.",
            SoSao = 5,
            NgayDang = new DateTime(2025, 10, 15)
        }
    };
            var discountMap = GetDiscountMap();
            var giamGia = discountMap.TryGetValue(hangHoa.Id, out var percent) ? percent : 0;
            var result = new ChiTietHangHoaVM
            {
                MaHh = hangHoa.Id,
                TenHH = hangHoa.Name,
                Hinh = hangHoa.ImageUrl ?? string.Empty,
                DonGia = hangHoa.Price,
                MoTaNgan = hangHoa.Description ?? string.Empty,
                TenLoai = hangHoa.Category.Name,
                ChiTiet = hangHoa.Description ?? string.Empty,
                DiemDanhGia = 5,
                SoLuongTon = hangHoa.Stock,
                DanhSachDanhGia = dsDanhGia,
                GiamGiaPhanTram = giamGia
            };
            return View(result);
        }
        [HttpGet] 
        public IActionResult DangKy()
        {
            return View(new RegisterVM()); 
        }

        private Dictionary<int, int> GetDiscountMap()
        {
            var percentageTable = new[] { 50, 45, 40, 35, 30, 25, 20, 15, 12, 10 };

            var topProducts = db.Products
                .OrderByDescending(p => p.Price)
                .Select(p => new { p.Id, p.Price })
                .Take(percentageTable.Length)
                .ToList();

            var map = new Dictionary<int, int>();

            for (var i = 0; i < topProducts.Count; i++)
            {
                var percentage = percentageTable[Math.Min(i, percentageTable.Length - 1)];
                map[topProducts[i].Id] = percentage;
            }

            return map;
        }
    }
}


