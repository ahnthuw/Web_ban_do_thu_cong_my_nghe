using System.Collections.Generic;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels.Contact
{
    public class AdminContactDashboardVM
    {
        public List<ContactThreadSummaryVM> Threads { get; set; } = new();
        public ContactConversationVM? CurrentConversation { get; set; }
        public int? SelectedCustomerId { get; set; }
        public List<GuestContactVM> GuestContacts { get; set; } = new();
        public bool HasCustomers => Threads.Count > 0;
    }
}
