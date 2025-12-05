using System;
using System.ComponentModel.DataAnnotations;

namespace Web_ban_do_thu_cong_my_nghe.Data
{
    public class GuestContact
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Subject { get; set; }

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public bool IsProcessed { get; set; }
    }
}
