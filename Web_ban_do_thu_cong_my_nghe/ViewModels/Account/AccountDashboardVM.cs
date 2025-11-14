using System.Collections.Generic;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels.Account
{
    public class AccountDashboardVM
    {
        public AccountUserSummaryVM User { get; set; } = new AccountUserSummaryVM();
        public List<FavoriteItemVM> Favorites { get; set; } = new List<FavoriteItemVM>();
        public List<NotificationVM> Notifications { get; set; } = new List<NotificationVM>();
        public AccountSettingsVM Settings { get; set; } = new AccountSettingsVM();
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
