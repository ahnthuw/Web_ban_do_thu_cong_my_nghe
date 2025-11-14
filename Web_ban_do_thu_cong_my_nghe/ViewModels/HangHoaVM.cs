using System;
using System.Collections.Generic;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels
{
    public class HangHoaVM
    {
        public int MaHh { get; set; }
        public string TenHH { get; set; }
        public string Hinh { get; set; }
        public decimal DonGia { get; set; } 
        public string MoTaNgan { get; set; }
        public string TenLoai { get; set; }
        public int SoLuongBan { get; set; }
        public int GiamGiaPhanTram { get; set; }
        public decimal GiaSauGiam => GiamGiaPhanTram > 0 ? Math.Round(DonGia * (100 - GiamGiaPhanTram) / 100, 0) : DonGia;
    }

    public class ChiTietHangHoaVM
    {
        public int MaHh { get; set; }
        public string TenHH { get; set; }
        public string Hinh { get; set; }
        public decimal DonGia { get; set; }
        public string MoTaNgan { get; set; }
        public string TenLoai { get; set; }
        public string ChiTiet { get; set; }
        public int DiemDanhGia { get; set; }
        public int SoLuongTon { get; set; }
        public List<DanhGiaVM> DanhSachDanhGia { get; set; } = new List<DanhGiaVM>();
        public int GiamGiaPhanTram { get; set; }
        public decimal GiaSauGiam => GiamGiaPhanTram > 0 ? Math.Round(DonGia * (100 - GiamGiaPhanTram) / 100, 0) : DonGia;
    }

   
   
    public class DanhGiaVM
    {
        public string TenNguoiDung { get; set; }
        public DateTime NgayDang { get; set; }
        public string NoiDung { get; set; }
        public int SoSao { get; set; }
    }

}

