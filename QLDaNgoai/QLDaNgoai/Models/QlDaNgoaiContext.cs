using Microsoft.EntityFrameworkCore;

namespace QLDaNgoai.Models;

public partial class QlDaNgoaiContext : DbContext
{
    public QlDaNgoaiContext()
    {
    }

    public QlDaNgoaiContext(DbContextOptions<QlDaNgoaiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChuyenDi> ChuyenDis { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChuyenDi>(entity =>
        {
            entity.HasKey(e => e.MaChuyen).HasName("PK_ChuyenDi");

            entity.ToTable("ChuyenDi");

            entity.Property(e => e.DiaDiem).HasMaxLength(100);
            entity.Property(e => e.GhiChu).HasMaxLength(200);
            entity.Property(e => e.NganSach).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.NgayDi).HasColumnType("date");
            entity.Property(e => e.PhuongTien).HasMaxLength(30);
            entity.Property(e => e.TenChuyen).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
