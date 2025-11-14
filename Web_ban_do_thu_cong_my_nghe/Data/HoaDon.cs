using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Web_ban_do_thu_cong_my_nghe.Data;

namespace Web_ban_do_thu_cong_my_nghe.Data;
[Table("HoaDon")]
public class HoaDon
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaHD { get; set; }

    public int UserId { get; set; }
    public DateTime NgayDat { get; set; }
    public DateTime? NgayCan { get; set; }
    public DateTime? NgayGiao { get; set; }

    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Phone_Number { get; set; }

    public string? CachThanhToan { get; set; }
    public string? CachVanChuyen { get; set; }
    public decimal? PhiVanChuyen { get; set; }

    public int MaTrangThai { get; set; }
    public int? MaNV { get; set; }
    public string? GhiChu { get; set; }

    [ForeignKey("MaNV")]
    public NhanVien NhanVien { get; set; }

    [ForeignKey("MaTrangThai")]
    public TrangThai TrangThai { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
