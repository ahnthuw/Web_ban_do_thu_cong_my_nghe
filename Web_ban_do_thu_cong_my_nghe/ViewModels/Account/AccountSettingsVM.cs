namespace Web_ban_do_thu_cong_my_nghe.ViewModels.Account
{
    public class AccountSettingsVM
    {
        public string Fullname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool ReceiveEmail { get; set; }
        public bool ReceiveSms { get; set; }
    }
}
