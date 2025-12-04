using System;
using System.ComponentModel.DataAnnotations;

namespace Web_ban_do_thu_cong_my_nghe.Data
{
    public class DiscountCode
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        [Range(0, 1)]
        public decimal? PercentOff { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? AmountOff { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinOrderValue { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }

        public int UsageLimit { get; set; }
        public int TimesUsed { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
