using System.Collections.Generic;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels
{
    public class HangHoaIndexVM
    {
        public List<HangHoaVM> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int? CategoryId { get; set; }
    }
}
