using System.ComponentModel.DataAnnotations;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels
{
    public class RegisterVM
    {
        [Display(Name = "Tên đăng nhập")]
        [Required]
        public string TenDangNhap { get; set; }

        
        public int MaKH { get; set; }
        
        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "*")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; }

        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ tên")] 
        [MaxLength(100, ErrorMessage = "Tối đa 100 ký tự")]
        public string HoTen { get; set; }


        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        public string GioiTinh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập ngày sinh")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
       
        public DateTime? NgaySinh { get; set; }
        [Display(Name = "Địa chỉ")]

        [MaxLength(255, ErrorMessage = "Tối đa 255 ký tự")]
        public string? DiaChi { get; set; }

        [Display(Name = "Điện thoại")]
        [MaxLength(20, ErrorMessage = "Tối đa 20ký tự")]
        [RegularExpression("(^$)|(0[9578])+([0-9]{8})\\b", ErrorMessage = "Chưa đúng định dạng số điện thoại")]
        public string? DienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")] 
        [EmailAddress(ErrorMessage = "Chưa đúng định dạng email")]
      
        public string Email { get; set; }


        public string? Hinh { get; set; }
      

    }
}
