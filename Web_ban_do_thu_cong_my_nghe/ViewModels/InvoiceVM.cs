using System;
using System.Collections.Generic;
using System.Linq;
using Web_ban_do_thu_cong_my_nghe.Data;
using Web_ban_do_thu_cong_my_nghe.Helpers;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels
{
    public class InvoiceVM
    {
        public const decimal DefaultTaxRate = 0.1m;
        private static readonly decimal[] KnownShippingFees = { 0m, 8000m, 15000m };
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public DateTime? OrderDate { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalBeforeTax { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAfterTax { get; set; }
        public decimal Total => TotalAfterTax;
        public string Notes { get; set; } = string.Empty;
        public bool IsAdminView { get; set; }
        public string? BackUrl { get; set; }
        public string BackLabel { get; set; } = "Quay lại";
        public List<InvoiceItemVM> Items { get; set; } = new();

        public static InvoiceVM FromOrder(Order order, bool isAdminView)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            var items = order.OrderDetails?
                .Select(detail => new InvoiceItemVM
                {
                    ProductName = detail.Product?.Name ?? "Sản phẩm",
                    Quantity = detail.Quantity,
                    UnitPrice = detail.PriceAtPurchase
                })
                .ToList() ?? new List<InvoiceItemVM>();

            var subtotal = items.Sum(i => i.LineTotal);
            var totalRecorded = order.TotalMoney;
            var taxRate = DefaultTaxRate;
            var discountAmount = ExtractDiscountAmount(order.Notes);

            decimal shippingFee;
            decimal totalBeforeTax;
            decimal taxAmount;
            decimal totalAfterTax;

            if (IsLegacyPreTaxOrder(subtotal, totalRecorded))
            {
                shippingFee = Math.Max(0, totalRecorded - subtotal);
                totalBeforeTax = subtotal + shippingFee;
                taxAmount = Math.Round(totalBeforeTax * taxRate, 0, MidpointRounding.AwayFromZero);
                totalAfterTax = totalBeforeTax + taxAmount;
            }
            else
            {
                totalAfterTax = totalRecorded;
                totalBeforeTax = Math.Round(totalAfterTax / (1 + taxRate), 0, MidpointRounding.AwayFromZero);
                var inferredShipping = totalBeforeTax - subtotal + discountAmount;
                shippingFee = NormalizeShippingFee(inferredShipping);
                taxAmount = totalAfterTax - totalBeforeTax;
            }

            if (discountAmount <= 0)
            {
                discountAmount = Math.Max(0, subtotal + shippingFee - totalBeforeTax);
            }

            return new InvoiceVM
            {
                OrderId = order.Id,
                CustomerName = order.User?.Fullname ?? "Khách hàng",
                CustomerEmail = order.User?.Email ?? string.Empty,
                CustomerPhone = order.ShippingPhone,
                ShippingAddress = order.ShippingAddress,
                OrderDate = order.OrderDate,
                StatusLabel = OrderStatusHelper.GetLabel(order.Status),
                StatusClass = OrderStatusHelper.GetStatusClass(order.Status),
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                DiscountAmount = discountAmount,
                TotalBeforeTax = totalBeforeTax,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                TotalAfterTax = totalAfterTax,
                Notes = order.Notes ?? string.Empty,
                Items = items,
                IsAdminView = isAdminView
            };
        }

        private static bool IsLegacyPreTaxOrder(decimal subtotal, decimal totalRecorded)
        {
            var difference = totalRecorded - subtotal;
            return KnownShippingFees.Contains(difference);
        }

        private static decimal ExtractDiscountAmount(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return 0m;
            }

            var lines = notes
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim());

            foreach (var line in lines)
            {
                if (!line.StartsWith("Mã giảm giá", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var start = line.IndexOf("(-", StringComparison.Ordinal);
                var end = line.IndexOf('đ');
                if (start < 0 || end <= start + 2)
                {
                    continue;
                }

                var amountPart = line.Substring(start + 2, end - (start + 2));
                var digitsOnly = new string(amountPart.Where(char.IsDigit).ToArray());
                if (string.IsNullOrEmpty(digitsOnly))
                {
                    continue;
                }

                if (decimal.TryParse(digitsOnly, out var parsed))
                {
                    return parsed;
                }
            }

            return 0m;
        }

        private static decimal NormalizeShippingFee(decimal shippingFee)
        {
            if (shippingFee <= 0)
            {
                return 0m;
            }

            foreach (var fee in KnownShippingFees)
            {
                if (Math.Abs(fee - shippingFee) <= 1)
                {
                    return fee;
                }
            }

            return shippingFee;
        }
    }

    public class InvoiceItemVM
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
