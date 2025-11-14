using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Web_ban_do_thu_cong_my_nghe.Data;

namespace Web_ban_do_thu_cong_my_nghe.Data;
[Table("ChiTietHoaDon")]
public class ChiTietHoaDon
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaCT_temp { get; set; }

    public int MaHD { get; set; }
    public int MaHH { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal GiamGia { get; set; }

    [ForeignKey("MaHD")]
    public virtual HoaDon HoaDon { get; set; }

    [ForeignKey("MaHH")]
    public virtual Product Product { get; set; }
}
