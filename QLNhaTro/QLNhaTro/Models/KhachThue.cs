namespace QLNhaTro.Models;

public partial class KhachThue
{
    public int MaKhach { get; set; }

    public string HoTen { get; set; } = null!;

    public string GioiTinh { get; set; } = null!;

    public DateOnly NgaySinh { get; set; }

    public string SoDienThoai { get; set; } = null!;

    public int? MaTinh { get; set; }

    public string TenTinh { get; set; } = null!;

    public int? MaPhuongXa { get; set; }

    public string TenPhuongXa { get; set; } = null!;

    public bool DaDangKyTamTru { get; set; }

    public int MaPhong { get; set; }

    public virtual PhongTro MaPhongNavigation { get; set; } = null!;
}
