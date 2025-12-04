
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Web_ban_do_thu_cong_my_nghe.Helpers;

namespace Web_ban_do_thu_cong_my_nghe.Data;

public partial class MynghevietDbContext : DbContext
{
    public MynghevietDbContext()
    {
    }

    public MynghevietDbContext(DbContextOptions<MynghevietDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<DiscountCode> DiscountCodes { get; set; }

    public DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<TrangThai> TrangThais { get; set; }

    public virtual DbSet<ContactMessage> ContactMessages { get; set; }



    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseSqlServer("Data Source=(localdb)\\mssqllocaldb;Initial Catalog=myngheviet_db;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --------------------------------------------------------------------------------------
        // **KHỐI CODE ĐÃ THÊM/SỬA ĐỂ KHẮC PHỤC LỖI 'TrangThaiMaTrangThai'**
        // Lỗi này xảy ra khi EF Core cố gắng tạo mối quan hệ giữa Order và TrangThai
        // nhưng cột Khóa ngoại không phải là TrangThaiMaTrangThai.
        // Chúng ta sử dụng .Ignore() để ngăn EF Core cố gắng suy luận mối quan hệ này.
        modelBuilder.Entity<Order>(entity =>
        {
            // Bỏ qua thuộc tính điều hướng TrangThai (nếu đã từng tồn tại)
            entity.Ignore("TrangThai");
            entity.ToTable("orders");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.OrderDate).HasColumnName("order_date");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TotalMoney).HasColumnName("total_money");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ShippingAddress).HasColumnName("shipping_address");
            entity.Property(e => e.ShippingPhone).HasColumnName("shipping_phone");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("order_details");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.PriceAtPurchase).HasColumnName("price_at_purchase");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.Stock).HasColumnName("stock");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Fullname).HasColumnName("fullname");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.RandomKey).HasColumnName("RandomKey");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Hinh).HasColumnName("Hinh");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.TenDangNhap).HasColumnName("TenDangNhap");
            entity.Property(e => e.NgaySinh).HasColumnName("NgaySinh");
        });

        // Cấu hình lại cho User và NhanVien (nếu cần)
        modelBuilder.Entity<NhanVien>().ToTable("NhanVien");
        // --------------------------------------------------------------------------------------

        modelBuilder.Entity<TrangThai>(entity =>
        {
            entity.HasKey(e => e.MaTrangThai).HasName("PK__TrangTha__14109724137F1578");

            entity.ToTable("TrangThai");

            entity.Property(e => e.MaTrangThai)
                .ValueGeneratedNever(); // Đảm bảo khóa không tự động tăng

            entity.Property(e => e.TenTrangThai)
                .HasMaxLength(100);
            entity.Property(e => e.MoTa)
                .HasMaxLength(255);

            entity.HasData(
                new TrangThai
                {
                    MaTrangThai = OrderStatusHelper.Pending,
                    TenTrangThai = "Chờ xử lý",
                    MoTa = "Đơn hàng mới khởi tạo"
                },
                new TrangThai
                {
                    MaTrangThai = OrderStatusHelper.Confirmed,
                    TenTrangThai = "Đã xác nhận",
                    MoTa = "Đơn hàng đã được xác nhận bởi nhân viên"
                },
                new TrangThai
                {
                    MaTrangThai = OrderStatusHelper.Shipping,
                    TenTrangThai = "Đang giao hàng",
                    MoTa = "Đơn hàng đã bàn giao cho đơn vị vận chuyển"
                },
                new TrangThai
                {
                    MaTrangThai = OrderStatusHelper.Completed,
                    TenTrangThai = "Đã hoàn thành",
                    MoTa = "Đơn hàng đã giao thành công"
                },
                new TrangThai
                {
                    MaTrangThai = OrderStatusHelper.Cancelled,
                    TenTrangThai = "Đã hủy",
                    MoTa = "Đơn hàng đã bị hủy"
                }
            );
        });

        modelBuilder.Entity<DiscountCode>(entity =>
        {
            entity.ToTable("discount_codes");

            entity.Property(e => e.Code)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(255);

            entity.Property(e => e.PercentOff)
                .HasColumnType("decimal(5,2)");

            entity.Property(e => e.AmountOff)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.MinOrderValue)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.StartDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.EndDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime2");
        });

        modelBuilder.Entity<ContactMessage>(entity =>
        {
            entity.ToTable("contact_messages");
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();

            entity.HasOne(d => d.Sender)
                .WithMany()
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Receiver)
                .WithMany()
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
