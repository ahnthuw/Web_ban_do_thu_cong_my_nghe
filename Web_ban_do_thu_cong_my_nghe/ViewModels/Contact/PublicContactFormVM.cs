using System.ComponentModel.DataAnnotations;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels.Contact
{
    public class PublicContactFormVM
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [MaxLength(200, ErrorMessage = "Chủ đề tối đa 200 ký tự")]
        [Display(Name = "Chủ đề")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        [MaxLength(2000, ErrorMessage = "Nội dung tối đa 2000 ký tự")]
        [Display(Name = "Nội dung liên hệ")]
        public string Message { get; set; } = string.Empty;
    }
}
