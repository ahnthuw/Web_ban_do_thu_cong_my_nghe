using System;

namespace Web_ban_do_thu_cong_my_nghe.Data
{
    public class ContactMessage
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsAdminMessage { get; set; }
        public virtual User? Sender { get; set; }
        public virtual User? Receiver { get; set; }
    }
}
