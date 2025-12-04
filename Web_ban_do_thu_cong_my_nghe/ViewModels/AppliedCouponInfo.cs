using System;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels
{
    public class AppliedCouponInfo
    {
        public int CouponId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? PercentOff { get; set; }
        public decimal? AmountOff { get; set; }
        public decimal MinOrderValue { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public int UsageLimit { get; set; }
        public int TimesUsed { get; set; }

        public bool IsStillValid(DateTime utcNow)
        {
            if (utcNow < ValidFrom)
            {
                return false;
            }

            if (ValidTo.HasValue && utcNow > ValidTo.Value)
            {
                return false;
            }

            if (UsageLimit > 0 && TimesUsed >= UsageLimit)
            {
                return false;
            }

            return true;
        }

        public decimal CalculateDiscount(decimal subtotal)
        {
            if (subtotal <= 0 || subtotal < MinOrderValue)
            {
                return 0m;
            }

            decimal discount = 0m;
            if (PercentOff.HasValue && PercentOff.Value > 0)
            {
                discount = Math.Round(subtotal * PercentOff.Value, 0, MidpointRounding.AwayFromZero);
            }
            else if (AmountOff.HasValue && AmountOff.Value > 0)
            {
                discount = AmountOff.Value;
            }

            if (discount <= 0m)
            {
                return 0m;
            }

            return Math.Min(discount, subtotal);
        }

        public string GetSummary(decimal subtotal)
        {
            var discount = CalculateDiscount(subtotal);
            if (discount <= 0)
            {
                return Description;
            }

            return string.IsNullOrWhiteSpace(Description)
                ? $"Mã {Code}: Giảm {discount:N0}đ"
                : $"{Description} (-{discount:N0}đ)";
        }

        public static AppliedCouponInfo FromEntity(Data.DiscountCode entity)
        {
            return new AppliedCouponInfo
            {
                CouponId = entity.Id,
                Code = entity.Code,
                Description = entity.Description ?? string.Empty,
                PercentOff = entity.PercentOff,
                AmountOff = entity.AmountOff,
                MinOrderValue = entity.MinOrderValue,
                ValidFrom = entity.StartDate,
                ValidTo = entity.EndDate,
                UsageLimit = entity.UsageLimit,
                TimesUsed = entity.TimesUsed
            };
        }
    }
}
