namespace QLDaNgoai.Models;

public partial class ChuyenDi
{
    public int MaChuyen { get; set; }

    public string TenChuyen { get; set; } = null!;

    public string DiaDiem { get; set; } = null!;

    public DateOnly NgayDi { get; set; }

    public int SoNguoi { get; set; }

    public decimal NganSach { get; set; }

    public string PhuongTien { get; set; } = null!;

    public bool DaHoanThanh { get; set; }

    public string? GhiChu { get; set; }
}
