using System;
using System.ComponentModel.DataAnnotations;

namespace Web_ban_do_thu_cong_my_nghe.Data
{
    public class ContactThreadRead
    {
        [Key]
        public int Id { get; set; }

        public int AdminId { get; set; }
        public int CustomerId { get; set; }
        public DateTime LastReadAt { get; set; }
    }
}
