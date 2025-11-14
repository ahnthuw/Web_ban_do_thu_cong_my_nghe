using System;
using System.Collections.Generic;

namespace Web_ban_do_thu_cong_my_nghe.Data;

public partial class User
{
    public int Id { get; set; }

    public string Fullname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Role { get; set; }

    public DateTime? CreatedAt { get; set; }
    public string RandomKey { get; set; }
    public bool Status { get; set; }
    public string? Hinh { get; set; }
    public string? Gender { get; set; }
    public string? TenDangNhap { get; set; }
    public DateTime? NgaySinh { get; set; }
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
