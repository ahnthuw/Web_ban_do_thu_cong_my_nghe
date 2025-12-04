using System;
using System.ComponentModel.DataAnnotations;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels
{
    public class DiscountCodeCreateVM
    {
        [Required]
        [MaxLength(64)]
        [Display(Name = "Mã giảm giá")]
        public string Code { get; set; } = string.Empty;

        [MaxLength(255)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Range(0, 100)]
        [Display(Name = "% giảm (0-100)")]
        public decimal? PercentOff { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Giảm cố định (đ)")]
        public decimal? AmountOff { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Đơn tối thiểu (đ)")]
        public decimal MinOrderValue { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Display(Name = "Ngày kết thúc")]
        public DateTime? EndDate { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Giới hạn lượt dùng (0 = không giới hạn)")]
        public int UsageLimit { get; set; }
    }
}
