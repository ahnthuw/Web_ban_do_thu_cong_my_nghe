using System.ComponentModel.DataAnnotations;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels.Contact
{
    public class ContactSendVM
    {
        [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn.")]
        [StringLength(1000, ErrorMessage = "Tin nhắn tối đa 1000 ký tự.")]
        [Display(Name = "Tin nhắn")]
        public string Message { get; set; } = string.Empty;
    }
}
