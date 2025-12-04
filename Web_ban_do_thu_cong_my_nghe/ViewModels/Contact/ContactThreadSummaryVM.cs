using System;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels.Contact
{
    public class ContactThreadSummaryVM
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime? LastMessageAt { get; set; }
        public string LastMessagePreview { get; set; } = string.Empty;
        public bool LastFromCustomer { get; set; }
    }
}
