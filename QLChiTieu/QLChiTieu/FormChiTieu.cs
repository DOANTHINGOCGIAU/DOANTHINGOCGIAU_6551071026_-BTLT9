using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QLChiTieu.Data;
using QLChiTieu.Models;
using Serilog;

namespace QLChiTieu;

/// <summary>Một dòng hiển thị trên DataGridView.</summary>
public sealed class ChiTieuRow
{
    public int MaCt { get; set; }
    public DateTime NgayChi { get; set; }
    public string NoiDung { get; set; } = "";
    public string LoaiChi { get; set; } = "";
    public decimal SoTien { get; set; }
    public string? GhiChu { get; set; }
}

public class FormChiTieu : Form
{
    private static readonly CultureInfo Vi = new("vi-VN");
    private static readonly string[] DanhSachLoai = { "Ăn uống", "Đi lại", "Học tập", "Giải trí", "Khác" };

    // ----- Nhập liệu -----
    private readonly DateTimePicker dtpNgay = new();
    private readonly TextBox txtNoiDung = new();
    private readonly ComboBox cboLoai = new();
    private readonly NumericUpDown nudSoTien = new();
    private readonly TextBox txtGhiChu = new();

    // ----- Tìm kiếm / lọc -----
    private readonly TextBox txtTim = new();
    private readonly ComboBox cboLocLoai = new();
    private readonly CheckBox chkTheoNgay = new();
    private readonly DateTimePicker dtpTu = new();
    private readonly DateTimePicker dtpDen = new();
    private readonly Button btnTim = new();

    // ----- Nút chức năng, lưới, tổng -----
    private readonly Button btnThem = new();
    private readonly Button btnSua = new();
    private readonly Button btnXoa = new();
    private readonly Button btnLamMoi = new();
    private readonly DataGridView dgv = new();
    private readonly Label lblTong = new();

    private int? _selectedId;      // MaCT của dòng đang chọn
    private bool _loading = true;  // chặn sự kiện khi đang khởi tạo / đặt lại bộ lọc

    public FormChiTieu()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        Text = "Sổ chi tiêu cá nhân";
        ClientSize = new Size(900, 640);
        MinimumSize = new Size(916, 600);
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        Load += (_, _) => InitData();
    }

    // =====================================================================
    //  DỰNG GIAO DIỆN
    // =====================================================================
    private static Label Lbl(string text, int x, int y) =>
        new() { Text = text, AutoSize = true, Location = new Point(x, y) };

    private void BuildUi()
    {
        // ---------- GroupBox: Thông tin khoản chi ----------
        var gbInfo = new GroupBox { Text = "Thông tin khoản chi", Location = new Point(12, 8), Size = new Size(520, 192) };

        dtpNgay.Format = DateTimePickerFormat.Custom;
        dtpNgay.CustomFormat = "dd/MM/yyyy";
        dtpNgay.MaxDate = DateTime.Today;
        dtpNgay.SetBounds(95, 24, 150, 25);

        cboLoai.DropDownStyle = ComboBoxStyle.DropDownList;
        cboLoai.SetBounds(335, 24, 165, 25);

        txtNoiDung.MaxLength = 100;
        txtNoiDung.SetBounds(95, 58, 405, 25);

        nudSoTien.Minimum = 0;
        nudSoTien.Maximum = 999999999999M;
        nudSoTien.Increment = 1000;
        nudSoTien.DecimalPlaces = 0;
        nudSoTien.ThousandsSeparator = true;
        nudSoTien.TextAlign = HorizontalAlignment.Right;
        nudSoTien.SetBounds(95, 92, 150, 25);

        txtGhiChu.Multiline = true;
        txtGhiChu.MaxLength = 200;
        txtGhiChu.ScrollBars = ScrollBars.Vertical;
        txtGhiChu.SetBounds(95, 126, 405, 54);

        gbInfo.Controls.AddRange(new Control[]
        {
            Lbl("Ngày chi", 15, 28), dtpNgay,
            Lbl("Loại chi", 265, 28), cboLoai,
            Lbl("Nội dung", 15, 62), txtNoiDung,
            Lbl("Số tiền (đ)", 15, 96), nudSoTien,
            Lbl("Ghi chú", 15, 130), txtGhiChu
        });

        // ---------- GroupBox: Tìm kiếm ----------
        var gbSearch = new GroupBox
        {
            Text = "Tìm kiếm",
            Location = new Point(545, 8),
            Size = new Size(343, 192),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        txtTim.PlaceholderText = "Nhập nội dung...";
        txtTim.SetBounds(90, 24, 238, 25);

        cboLocLoai.DropDownStyle = ComboBoxStyle.DropDownList;
        cboLocLoai.SetBounds(90, 58, 238, 25);

        chkTheoNgay.Text = "Lọc theo khoảng ngày";
        chkTheoNgay.AutoSize = true;
        chkTheoNgay.Checked = true;
        chkTheoNgay.Location = new Point(90, 90);

        dtpTu.Format = DateTimePickerFormat.Custom;
        dtpTu.CustomFormat = "dd/MM/yyyy";
        dtpTu.SetBounds(90, 118, 130, 25);

        dtpDen.Format = DateTimePickerFormat.Custom;
        dtpDen.CustomFormat = "dd/MM/yyyy";
        dtpDen.SetBounds(90, 152, 130, 25);

        btnTim.Text = "Tìm";
        btnTim.SetBounds(238, 150, 90, 28);

        gbSearch.Controls.AddRange(new Control[]
        {
            Lbl("Nội dung", 15, 28),
            txtTim,
            Lbl("Loại chi", 15, 62),
            cboLocLoai,
            chkTheoNgay,
            Lbl("Từ ngày", 15, 122), dtpTu,
            Lbl("Đến ngày", 15, 156), dtpDen,
            btnTim
        });

        // ---------- Nút chức năng ----------
        btnThem.Text = "Thêm";
        btnSua.Text = "Sửa";
        btnXoa.Text = "Xóa";
        btnLamMoi.Text = "Làm mới";
        btnThem.SetBounds(95, 208, 100, 32);
        btnSua.SetBounds(205, 208, 100, 32);
        btnXoa.SetBounds(315, 208, 100, 32);
        btnLamMoi.SetBounds(425, 208, 100, 32);

        // ---------- DataGridView ----------
        dgv.SetBounds(12, 250, 876, 340);
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgv.AutoGenerateColumns = false;
        dgv.ReadOnly = true;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.AllowUserToResizeRows = false;
        dgv.MultiSelect = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.BackgroundColor = SystemColors.Window;
        dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 248, 252) };

        var right = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N0" };

        dgv.Columns.AddRange(
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Mã", DataPropertyName = nameof(ChiTieuRow.MaCt), FillWeight = 45,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "D3" }
            },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Ngày chi", DataPropertyName = nameof(ChiTieuRow.NgayChi), FillWeight = 75,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "dd/MM/yyyy" }
            },
            new DataGridViewTextBoxColumn { HeaderText = "Nội dung", DataPropertyName = nameof(ChiTieuRow.NoiDung), FillWeight = 160 },
            new DataGridViewTextBoxColumn { HeaderText = "Loại chi", DataPropertyName = nameof(ChiTieuRow.LoaiChi), FillWeight = 80 },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Số tiền", DataPropertyName = nameof(ChiTieuRow.SoTien), FillWeight = 85,
                DefaultCellStyle = right
            },
            new DataGridViewTextBoxColumn { HeaderText = "Ghi chú", DataPropertyName = nameof(ChiTieuRow.GhiChu), FillWeight = 160 });

        // ---------- Label tổng chi ----------
        lblTong.SetBounds(12, 598, 876, 28);
        lblTong.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblTong.TextAlign = ContentAlignment.MiddleRight;
        lblTong.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblTong.ForeColor = Color.Firebrick;
        lblTong.Text = "Tổng chi: 0 đ";

        Controls.AddRange(new Control[] { gbInfo, gbSearch, btnThem, btnSua, btnXoa, btnLamMoi, dgv, lblTong });

        // ---------- Sự kiện ----------
        btnThem.Click += BtnThem_Click;
        btnSua.Click += BtnSua_Click;
        btnXoa.Click += BtnXoa_Click;
        btnLamMoi.Click += BtnLamMoi_Click;
        btnTim.Click += BtnTim_Click;
        dgv.CellClick += Dgv_CellClick;

        // Lọc tự cập nhật khi thay đổi điều kiện
        txtTim.TextChanged += (_, _) => LoadData();
        cboLocLoai.SelectedIndexChanged += (_, _) => LoadData();
        dtpTu.ValueChanged += (_, _) => LoadData();
        dtpDen.ValueChanged += (_, _) => LoadData();
        chkTheoNgay.CheckedChanged += (_, _) =>
        {
            dtpTu.Enabled = dtpDen.Enabled = chkTheoNgay.Checked;
            LoadData();
        };
    }

    // =====================================================================
    //  KHỞI TẠO DỮ LIỆU
    // =====================================================================
    private void InitData()
    {
        _loading = true;

        cboLoai.Items.AddRange(DanhSachLoai);
        cboLoai.SelectedIndex = 0;

        cboLocLoai.Items.Add("Tất cả");
        cboLocLoai.Items.AddRange(DanhSachLoai);

        ResetFilters();

        _loading = false;
        LoadData();
    }

    /// <summary>Đặt bộ lọc về mặc định: tất cả loại, từ đầu tháng đến hôm nay.</summary>
    private void ResetFilters()
    {
        txtTim.Clear();
        cboLocLoai.SelectedIndex = 0;
        var today = DateTime.Today;
        dtpTu.Value = new DateTime(today.Year, today.Month, 1);
        dtpDen.Value = today;
        chkTheoNgay.Checked = true;
        dtpTu.Enabled = dtpDen.Enabled = true;
    }

    // =====================================================================
    //  TRUY VẤN / HIỂN THỊ
    // =====================================================================
    private void LoadData()
    {
        if (_loading) return;

        string kw = txtTim.Text.Trim();
        string? loai = cboLocLoai.SelectedIndex > 0 ? cboLocLoai.SelectedItem?.ToString() : null;
        bool theoNgay = chkTheoNgay.Checked;
        var tu = DateOnly.FromDateTime(dtpTu.Value);
        var den = DateOnly.FromDateTime(dtpDen.Value);

        var rows = new List<ChiTieuRow>();

        if (!(theoNgay && tu > den))
        {
            bool ok = Run(() =>
            {
                using var db = DbFactory.Create();
                IQueryable<ChiTieu> q = db.ChiTieus.AsNoTracking();

                if (kw.Length > 0) q = q.Where(x => x.NoiDung.Contains(kw));
                if (loai != null) q = q.Where(x => x.LoaiChi == loai);
                if (theoNgay) q = q.Where(x => x.NgayChi >= tu && x.NgayChi <= den);

                var list = q.OrderByDescending(x => x.NgayChi).ThenByDescending(x => x.MaCt).ToList();

                rows = list.Select(x => new ChiTieuRow
                {
                    MaCt = x.MaCt,
                    NgayChi = x.NgayChi.ToDateTime(TimeOnly.MinValue),
                    NoiDung = x.NoiDung,
                    LoaiChi = x.LoaiChi,
                    SoTien = x.SoTien,
                    GhiChu = x.GhiChu
                }).ToList();
            }, "tải danh sách chi tiêu");

            if (!ok) rows = new List<ChiTieuRow>();
        }

        dgv.DataSource = rows;
        dgv.ClearSelection();
        _selectedId = null;

        decimal tong = rows.Sum(r => r.SoTien);
        lblTong.Text = $"Tổng chi: {tong.ToString("N0", Vi)} đ";
    }

    /// <summary>Chạy một thao tác CSDL; nếu lỗi thì ghi log Error và báo thân thiện.</summary>
    private bool Run(Action action, string moTa)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Lỗi CSDL khi {HanhDong}", moTa);
            MessageBox.Show(
                "Không thể truy cập cơ sở dữ liệu.\nVui lòng kiểm tra kết nối (appsettings.json) rồi thử lại.\n\n" +
                "Chi tiết lỗi đã được ghi vào thư mục Logs.",
                "Sổ chi tiêu cá nhân", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    // =====================================================================
    //  KIỂM TRA HỢP LỆ
    // =====================================================================
    private void Canh(string message, Control focus)
    {
        MessageBox.Show(message, "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        focus.Focus();
    }

    /// <summary>Đọc dữ liệu từ form; trả về null nếu không hợp lệ (đã báo lỗi).</summary>
    private ChiTieu? ReadForm()
    {
        string noiDung = txtNoiDung.Text.Trim();
        if (noiDung.Length == 0) { Canh("Nội dung không được để trống.", txtNoiDung); return null; }
        if (noiDung.Length > 100) { Canh("Nội dung tối đa 100 ký tự.", txtNoiDung); return null; }
        if (cboLoai.SelectedIndex < 0) { Canh("Vui lòng chọn loại chi.", cboLoai); return null; }
        if (nudSoTien.Value <= 0) { Canh("Số tiền phải lớn hơn 0.", nudSoTien); return null; }
        if (dtpNgay.Value.Date > DateTime.Today) { Canh("Ngày chi không được lớn hơn hôm nay.", dtpNgay); return null; }

        string ghiChu = txtGhiChu.Text.Trim();
        if (ghiChu.Length > 200) { Canh("Ghi chú tối đa 200 ký tự.", txtGhiChu); return null; }

        return new ChiTieu
        {
            NgayChi = DateOnly.FromDateTime(dtpNgay.Value),
            NoiDung = noiDung,
            LoaiChi = cboLoai.SelectedItem!.ToString()!,
            SoTien = nudSoTien.Value,
            GhiChu = ghiChu.Length == 0 ? null : ghiChu
        };
    }

    private void ClearInput()
    {
        dtpNgay.Value = DateTime.Today;
        txtNoiDung.Clear();
        cboLoai.SelectedIndex = 0;
        nudSoTien.Value = 0;
        txtGhiChu.Clear();
        _selectedId = null;
        dgv.ClearSelection();
        txtNoiDung.Focus();
    }

    // =====================================================================
    //  SỰ KIỆN NÚT
    // =====================================================================
    private void BtnThem_Click(object? sender, EventArgs e)
    {
        var khoan = ReadForm();
        if (khoan == null) return;

        bool ok = Run(() =>
        {
            using var db = DbFactory.Create();
            db.ChiTieus.Add(khoan);
            db.SaveChanges();           // sau SaveChanges, khoan.MaCt đã có giá trị identity
        }, "thêm khoản chi");
        if (!ok) return;

        Log.Information("Thêm khoản chi MaCT={MaCT}, NoiDung={NoiDung}, SoTien={SoTien}",
            khoan.MaCt, khoan.NoiDung, khoan.SoTien);

        ClearInput();
        LoadData();
    }

    private void BtnSua_Click(object? sender, EventArgs e)
    {
        if (_selectedId is null)
        {
            MessageBox.Show("Hãy bấm chọn một dòng trong danh sách để sửa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var input = ReadForm();
        if (input == null) return;

        int id = _selectedId.Value;
        bool notFound = false;

        bool ok = Run(() =>
        {
            using var db = DbFactory.Create();
            var cur = db.ChiTieus.Find(id);
            if (cur == null) { notFound = true; return; }

            cur.NgayChi = input.NgayChi;
            cur.NoiDung = input.NoiDung;
            cur.LoaiChi = input.LoaiChi;
            cur.SoTien = input.SoTien;
            cur.GhiChu = input.GhiChu;
            db.SaveChanges();
        }, "sửa khoản chi");
        if (!ok) return;

        if (notFound)
        {
            MessageBox.Show("Khoản chi này không còn tồn tại (có thể đã bị xóa).", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            Log.Information("Sửa khoản chi MaCT={MaCT}, NoiDung={NoiDung}, SoTien={SoTien}",
                id, input.NoiDung, input.SoTien);
        }

        ClearInput();
        LoadData();
    }

    private void BtnXoa_Click(object? sender, EventArgs e)
    {
        if (_selectedId is null)
        {
            MessageBox.Show("Hãy bấm chọn một dòng trong danh sách để xóa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int id = _selectedId.Value;
        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn xóa khoản chi \"{txtNoiDung.Text}\" (mã {id:000}) không?",
            "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

        bool ok = Run(() =>
        {
            using var db = DbFactory.Create();
            var cur = db.ChiTieus.Find(id);
            if (cur != null)
            {
                db.ChiTieus.Remove(cur);
                db.SaveChanges();
            }
        }, "xóa khoản chi");
        if (!ok) return;

        Log.Information("Xóa khoản chi MaCT={MaCT}", id);

        ClearInput();
        LoadData();
    }

    private void BtnLamMoi_Click(object? sender, EventArgs e)
    {
        ClearInput();
        _loading = true;
        ResetFilters();
        _loading = false;
        LoadData();
    }

    private void BtnTim_Click(object? sender, EventArgs e)
    {
        if (chkTheoNgay.Checked && dtpTu.Value.Date > dtpDen.Value.Date)
        {
            Canh("\"Từ ngày\" không được lớn hơn \"Đến ngày\".", dtpTu);
            return;
        }
        LoadData();
    }

    // Bấm vào một dòng -> đổ dữ liệu lên các control để sửa
    private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (dgv.Rows[e.RowIndex].DataBoundItem is not ChiTieuRow r) return;

        _selectedId = r.MaCt;

        var ngay = r.NgayChi.Date;
        if (ngay > dtpNgay.MaxDate) ngay = dtpNgay.MaxDate;
        if (ngay < dtpNgay.MinDate) ngay = dtpNgay.MinDate;
        dtpNgay.Value = ngay;

        txtNoiDung.Text = r.NoiDung;

        int idx = Array.IndexOf(DanhSachLoai, r.LoaiChi);
        cboLoai.SelectedIndex = idx >= 0 ? idx : DanhSachLoai.Length - 1;

        nudSoTien.Value = Math.Min(Math.Max(r.SoTien, nudSoTien.Minimum), nudSoTien.Maximum);
        txtGhiChu.Text = r.GhiChu ?? "";
    }
}
