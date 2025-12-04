// Trong file: Web_ban_do_thu_cong_my_nghe.Helpers/OrderStatusHelper.cs

using System.Collections.Generic;
using System.Linq;

namespace Web_ban_do_thu_cong_my_nghe.Helpers;

public static class OrderStatusHelper
{
    public const int Pending = 0;
    public const int Confirmed = 10; // THÊM TRẠNG THÁI MỚI: Đã xác nhận
    public const int Shipping = 20;  // Thay đổi giá trị để chèn Confirmed
    public const int Completed = 30;
    public const int Cancelled = 40; // THÊM TRẠNG THÁI MỚI: Đã hủy

    private static readonly IReadOnlyDictionary<int, string> StatusLabels = new Dictionary<int, string>
    {
        { Pending, "Chờ xử lý" },        // Đổi "Chờ xác nhận" thành "Chờ xử lý"
        { Confirmed, "Đã xác nhận" },    // THÊM: Label mới
        { Shipping, "Đang giao hàng" },
        { Completed, "Đã hoàn thành" },
        { Cancelled, "Đã hủy" }         // THÊM: Label mới
    };

    public static IReadOnlyDictionary<int, string> AllStatuses => StatusLabels;

    public static bool IsValid(int status) => StatusLabels.ContainsKey(status);

    public static string GetLabel(int status) => StatusLabels.TryGetValue(status, out var label)
        ? label
        : "Không xác định";

    public static string GetLabel(int? status)
    {
        if (!status.HasValue)
        {
            return StatusLabels[Pending];
        }

        return GetLabel(status.Value);
    }

    public static int Normalize(int? status) => status.HasValue && StatusLabels.ContainsKey(status.Value)
        ? status.Value
        : Pending;

    // Giữ nguyên các hàm còn lại
    public static string GetStatusText(int status) => GetLabel(status);

    public static string GetStatusText(int? status) => GetLabel(status);

    public static string GetStatusClass(int status) => status switch
    {
        Pending => "badge-warning",
        Confirmed => "badge-primary", // Thêm class mới cho Confirmed
        Shipping => "badge-info",
        Completed => "badge-success",
        Cancelled => "badge-danger",  // Thêm class mới cho Cancelled
        _ => "badge-secondary"
    };

    public static string GetStatusClass(int? status) => GetStatusClass(Normalize(status));

    public static IEnumerable<(int Value, string Label)> GetStatusOptions() => StatusLabels.Select(kvp => (kvp.Key, kvp.Value));
}