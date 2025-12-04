using System;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels.Contact
{
    public class ContactMessageVM
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsOwn { get; set; }
        public bool IsAdminMessage { get; set; }
    }
}
