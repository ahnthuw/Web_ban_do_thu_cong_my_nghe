using System.Collections.Generic;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels.Contact
{
    public class ContactConversationVM
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerRoleLabel { get; set; } = string.Empty;
        public List<ContactMessageVM> Messages { get; set; } = new();
        public ContactSendVM Composer { get; set; } = new();
    }
}
