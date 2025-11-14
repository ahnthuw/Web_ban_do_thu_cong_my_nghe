using System;
using System.Collections.Generic;

namespace Web_ban_do_thu_cong_my_nghe.Data;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
