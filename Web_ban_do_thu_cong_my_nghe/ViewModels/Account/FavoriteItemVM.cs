namespace Web_ban_do_thu_cong_my_nghe.ViewModels.Account
{
    public class FavoriteItemVM
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int TimesPurchased { get; set; }
    }
}
