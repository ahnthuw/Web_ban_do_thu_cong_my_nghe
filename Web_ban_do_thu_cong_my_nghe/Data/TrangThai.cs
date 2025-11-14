using System.ComponentModel.DataAnnotations;

namespace Web_ban_do_thu_cong_my_nghe.Data
{
    public class TrangThai
    {
        [Key]
        public int MaTrangThai { get; set; }
        public string TenTrangThai { get; set; }
        public string MoTa { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
