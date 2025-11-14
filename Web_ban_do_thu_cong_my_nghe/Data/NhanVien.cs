using System.ComponentModel.DataAnnotations;

namespace Web_ban_do_thu_cong_my_nghe.Data
{
    public class NhanVien
    {
        [Key]
        public int MaNV { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string MatKhau { get; set; }
    }
}
