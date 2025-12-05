using System;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels.Contact
{
    public class GuestContactVM
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsProcessed { get; set; }
    }
}
