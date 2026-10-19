using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QLNhaTro.Data;
using QLNhaTro.Models;
using QLNhaTro.Services;
using Serilog;

namespace QLNhaTro;

public sealed record PhongItem(int MaPhong, string Text);

/// <summary>Một dòng của DataGridView (kết quả JOIN KhachThue - PhongTro).</summary>
public sealed class KhachRow
{
    public int MaKhach { get; set; }
    public string HoTen { get; set; } = "";
    public string GioiTinh { get; set; } = "";
    public DateTime NgaySinh { get; set; }
    public string SoDienThoai { get; set; } = "";
    public int MaPhong { get; set; }
    public string TenPhong { get; set; } = "";
    public decimal GiaThue { get; set; }
    public int? MaTinh { get; set; }
    public string TenTinh { get; set; } = "";
    public int? MaPhuongXa { get; set; }
    public string TenPhuongXa { get; set; } = "";
    public string QueQuan { get; set; } = "";
    public bool DaDangKyTamTru { get; set; }
}

public class FormKhachThue : Form
{
    private static readonly CultureInfo Vi = new("vi-VN");
    private static readonly Regex SdtRegex = new(@"^\d{9,11}$");
    private const string TatCa = "Tất cả";

    // ----- Nhập liệu -----
    private readonly TextBox txtHoTen = new();
    private readonly RadioButton rbNam = new() { Text = "Nam", Checked = true, AutoSize = true };
    private readonly RadioButton rbNu = new() { Text = "Nữ", AutoSize = true };
    private readonly DateTimePicker dtpNgaySinh = new();
    private readonly TextBox txtSdt = new();
    private readonly ComboBox cboPhong = new();
    private readonly CheckBox chkOGhep = new();
    private readonly ComboBox cboTinh = new();
    private readonly ComboBox cboPhuong = new();
    private readonly LinkLabel linkTaiLai = new();
    private readonly CheckBox chkTamTru = new();

    // ----- Nút -----
    private readonly Button btnThem = new();
    private readonly Button btnSua = new();
    private readonly Button btnXoa = new();
    private readonly Button btnLamMoi = new();
    private readonly Button btnGayLoi = new();   // nút ẩn: Ctrl+Shift+F9 để hiện

    // ----- Tìm kiếm -----
    private readonly TextBox txtTim = new();
    private readonly ComboBox cboLocPhong = new();
    private readonly ComboBox cboLocTinh = new();
    private readonly Button btnBoLoc = new();

    private readonly DataGridView dgv = new();
    private readonly StatusStrip status = new();
    private readonly ToolStripStatusLabel tsStatus = new();

    private int? _selectedId;
    private bool _binding;                 // chặn sự kiện khi đang nạp dữ liệu cho control
    private bool _loadingAddr;             // đang chờ API địa chỉ
    private bool _tinhLoaded;              // danh sách tỉnh đã tải từ API
    private List<PhongTro> _phongs = new();
    private CancellationTokenSource? _wardCts;

    public FormKhachThue()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        Text = "Quản lý nhà trọ - Khách thuê";
        ClientSize = new Size(1100, 660);
        MinimumSize = new Size(1116, 640);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        BuildUi();
        Load += async (_, _) => await InitAsync();
        KeyDown += (_, e) =>
        {
            // Ctrl+Shift+F9: hiện/ẩn nút "Gây lỗi thử"
            if (e.Control && e.Shift && e.KeyCode == Keys.F9) btnGayLoi.Visible = !btnGayLoi.Visible;
        };
    }

    // =====================================================================
    //  GIAO DIỆN
    // =====================================================================
    private static Label Lbl(string text, int x, int y) =>
        new() { Text = text, AutoSize = true, Location = new Point(x, y) };

    private void BuildUi()
    {
        var gb = new GroupBox { Text = "Thông tin khách thuê", Location = new Point(10, 8), Size = new Size(392, 500) };

        txtHoTen.SetBounds(115, 28, 255, 25);
        txtHoTen.MaxLength = 100;

        var gbGt = new GroupBox { Text = "", Location = new Point(115, 54), Size = new Size(255, 38) };
        rbNam.Location = new Point(15, 12);
        rbNu.Location = new Point(110, 12);
        gbGt.Controls.AddRange(new Control[] { rbNam, rbNu });

        dtpNgaySinh.Format = DateTimePickerFormat.Custom;
        dtpNgaySinh.CustomFormat = "dd/MM/yyyy";
        dtpNgaySinh.MaxDate = DateTime.Today;
        dtpNgaySinh.Value = new DateTime(2000, 1, 1);
        dtpNgaySinh.SetBounds(115, 102, 150, 25);

        txtSdt.MaxLength = 15;
        txtSdt.SetBounds(115, 136, 255, 25);

        cboPhong.DropDownStyle = ComboBoxStyle.DropDownList;
        cboPhong.SetBounds(115, 170, 255, 25);

        chkOGhep.Text = "Cho phép ở ghép (cả phòng đang thuê)";
        chkOGhep.AutoSize = true;
        chkOGhep.Location = new Point(115, 198);

        cboTinh.DropDownStyle = ComboBoxStyle.DropDownList;
        cboTinh.SetBounds(115, 228, 255, 25);

        linkTaiLai.Text = "Tải lại danh sách địa chỉ";
        linkTaiLai.AutoSize = true;
        linkTaiLai.Location = new Point(115, 256);
        linkTaiLai.Visible = false;

        cboPhuong.DropDownStyle = ComboBoxStyle.DropDownList;
        cboPhuong.MaxDropDownItems = 12;
        cboPhuong.SetBounds(115, 284, 255, 25);

        chkTamTru.Text = "Đã đăng ký tạm trú";
        chkTamTru.AutoSize = true;
        chkTamTru.Location = new Point(115, 320);

        gb.Controls.AddRange(new Control[]
        {
            Lbl("Họ tên", 15, 32), txtHoTen,
            Lbl("Giới tính", 15, 68), gbGt,
            Lbl("Ngày sinh", 15, 106), dtpNgaySinh,
            Lbl("Số điện thoại", 15, 140), txtSdt,
            Lbl("Phòng", 15, 174), cboPhong, chkOGhep,
            Lbl("Tỉnh/Thành", 15, 232), cboTinh, linkTaiLai,
            Lbl("Phường/Xã", 15, 288), cboPhuong,
            chkTamTru
        });

        btnThem.Text = "Thêm";
        btnSua.Text = "Sửa";
        btnXoa.Text = "Xóa";
        btnLamMoi.Text = "Làm mới";
        btnThem.SetBounds(10, 520, 90, 32);
        btnSua.SetBounds(108, 520, 90, 32);
        btnXoa.SetBounds(206, 520, 90, 32);
        btnLamMoi.SetBounds(304, 520, 98, 32);

        btnGayLoi.Text = "Gây lỗi thử";
        btnGayLoi.Visible = false;
        btnGayLoi.SetBounds(10, 564, 120, 28);

        // ----- Thanh tìm kiếm -----
        txtTim.PlaceholderText = "Tìm theo họ tên hoặc SĐT";
        txtTim.SetBounds(415, 12, 215, 25);
        cboLocPhong.DropDownStyle = ComboBoxStyle.DropDownList;
        cboLocPhong.SetBounds(690, 12, 90, 25);
        cboLocTinh.DropDownStyle = ComboBoxStyle.DropDownList;
        cboLocTinh.SetBounds(830, 12, 150, 25);
        btnBoLoc.Text = "Bỏ lọc";
        btnBoLoc.SetBounds(990, 11, 90, 27);

        foreach (var c in new Control[] { txtTim, cboLocPhong, cboLocTinh, btnBoLoc })
            c.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        // ----- DataGridView -----
        dgv.SetBounds(415, 46, 675, 560);
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

        dgv.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "Họ tên", DataPropertyName = nameof(KhachRow.HoTen), FillWeight = 130 },
            new DataGridViewTextBoxColumn { HeaderText = "SĐT", DataPropertyName = nameof(KhachRow.SoDienThoai), FillWeight = 90 },
            new DataGridViewTextBoxColumn { HeaderText = "Phòng", DataPropertyName = nameof(KhachRow.TenPhong), FillWeight = 50 },
            new DataGridViewTextBoxColumn
            {
                HeaderText = "Giá thuê", DataPropertyName = nameof(KhachRow.GiaThue), FillWeight = 85,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "#,##0' đ'", Alignment = DataGridViewContentAlignment.MiddleRight }
            },
            new DataGridViewTextBoxColumn { HeaderText = "Quê quán", DataPropertyName = nameof(KhachRow.QueQuan), FillWeight = 170 },
            new DataGridViewCheckBoxColumn { HeaderText = "Tạm trú", DataPropertyName = nameof(KhachRow.DaDangKyTamTru), FillWeight = 55 });

        tsStatus.Text = "Sẵn sàng";
        status.Items.Add(tsStatus);

        Controls.AddRange(new Control[]
        {
            gb, btnThem, btnSua, btnXoa, btnLamMoi, btnGayLoi,
            txtTim, cboLocPhong, cboLocTinh, btnBoLoc,
            Lbl("Phòng:", 640, 16), Lbl("Tỉnh:", 790, 16),
            dgv, status
        });

        // ----- Sự kiện -----
        btnThem.Click += BtnThem_Click;
        btnSua.Click += BtnSua_Click;
        btnXoa.Click += BtnXoa_Click;
        btnLamMoi.Click += (_, _) => { ClearInput(); BoLoc(); };
        btnBoLoc.Click += (_, _) => BoLoc();
        btnGayLoi.Click += (_, _) => throw new InvalidOperationException("Lỗi thử nghiệm - nút \"Gây lỗi thử\"");
        dgv.CellClick += Dgv_CellClick;

        txtTim.TextChanged += (_, _) => LoadGrid();
        cboLocPhong.SelectedIndexChanged += (_, _) => LoadGrid();
        cboLocTinh.SelectedIndexChanged += (_, _) => LoadGrid();
        chkOGhep.CheckedChanged += (_, _) =>
        {
            if (_binding) return;
            int? cur = cboPhong.SelectedValue as int?;
            RefreshPhongCombo(cur, cur);
        };
        cboTinh.SelectedIndexChanged += CboTinh_SelectedIndexChanged;
        linkTaiLai.LinkClicked += async (_, _) => await LoadProvincesAsync();
    }

    // =====================================================================
    //  KHỞI TẠO
    // =====================================================================
    private async Task InitAsync()
    {
        RefreshAll();
        await LoadProvincesAsync();
    }

    // =====================================================================
    //  CSDL
    // =====================================================================
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
                "Không thể truy cập cơ sở dữ liệu.\nVui lòng kiểm tra kết nối (appsettings.json) rồi thử lại.",
                "Quản lý nhà trọ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    /// <summary>Nạp lại danh sách phòng, tỉnh (để lọc) rồi nạp lưới.</summary>
    private void RefreshAll()
    {
        List<PhongTro> phongs = new();
        List<string> tinhs = new();

        bool ok = Run(() =>
        {
            using var db = DbFactory.Create();
            phongs = db.PhongTros.AsNoTracking().OrderBy(p => p.TenPhong).ToList();
            tinhs = db.KhachThues.AsNoTracking().Select(k => k.TenTinh).Distinct().OrderBy(x => x).ToList();
        }, "tải danh sách phòng và tỉnh");
        if (!ok) return;

        _phongs = phongs;

        int? locPhong = cboLocPhong.SelectedValue as int?;
        string? locTinh = cboLocTinh.SelectedItem as string;

        _binding = true;
        try
        {
            var items = new List<PhongItem> { new(0, TatCa) };
            items.AddRange(phongs.Select(p => new PhongItem(p.MaPhong, p.TenPhong)));
            cboLocPhong.DataSource = null;
            cboLocPhong.DisplayMember = nameof(PhongItem.Text);
            cboLocPhong.ValueMember = nameof(PhongItem.MaPhong);
            cboLocPhong.DataSource = items;
            cboLocPhong.SelectedValue = locPhong ?? 0;
            if (cboLocPhong.SelectedIndex < 0) cboLocPhong.SelectedIndex = 0;

            cboLocTinh.Items.Clear();
            cboLocTinh.Items.Add(TatCa);
            cboLocTinh.Items.AddRange(tinhs.Cast<object>().ToArray());
            int idx = locTinh == null ? 0 : cboLocTinh.Items.IndexOf(locTinh);
            cboLocTinh.SelectedIndex = idx >= 0 ? idx : 0;
        }
        finally { _binding = false; }

        int? cur = cboPhong.SelectedValue as int?;
        RefreshPhongCombo(cur, cur);
        LoadGrid();
    }

    /// <summary>ComboBox phòng khi nhập: chỉ phòng Trống (hoặc cả phòng đang thuê nếu tick "ở ghép").</summary>
    private void RefreshPhongCombo(int? includeRoom, int? select)
    {
        var items = _phongs
            .Where(p => p.TinhTrang == "Trống"
                        || (chkOGhep.Checked && p.TinhTrang == "Đang thuê")
                        || p.MaPhong == includeRoom)
            .Select(p => new PhongItem(p.MaPhong, $"{p.TenPhong} - {p.GiaThue.ToString("N0", Vi)} đ"))
            .ToList();

        bool old = _binding;
        _binding = true;
        try
        {
            cboPhong.DataSource = null;
            cboPhong.DisplayMember = nameof(PhongItem.Text);
            cboPhong.ValueMember = nameof(PhongItem.MaPhong);
            cboPhong.DataSource = items;
            cboPhong.SelectedIndex = -1;
            if (select != null) cboPhong.SelectedValue = select.Value;
        }
        finally { _binding = old; }
    }

    private void LoadGrid()
    {
        if (_binding) return;

        string kw = txtTim.Text.Trim();
        int maPhong = (cboLocPhong.SelectedValue as int?) ?? 0;
        string? tinh = cboLocTinh.SelectedIndex > 0 ? cboLocTinh.SelectedItem as string : null;

        var rows = new List<KhachRow>();
        bool ok = Run(() =>
        {
            using var db = DbFactory.Create();
            IQueryable<KhachThue> q = db.KhachThues.AsNoTracking().Include(k => k.MaPhongNavigation);

            if (kw.Length > 0) q = q.Where(k => k.HoTen.Contains(kw) || k.SoDienThoai.Contains(kw));
            if (maPhong > 0) q = q.Where(k => k.MaPhong == maPhong);
            if (tinh != null) q = q.Where(k => k.TenTinh == tinh);

            var list = q.OrderBy(k => k.MaPhongNavigation.TenPhong).ThenBy(k => k.HoTen).ToList();
            rows = list.Select(k => new KhachRow
            {
                MaKhach = k.MaKhach,
                HoTen = k.HoTen,
                GioiTinh = k.GioiTinh,
                NgaySinh = k.NgaySinh.ToDateTime(TimeOnly.MinValue),
                SoDienThoai = k.SoDienThoai,
                MaPhong = k.MaPhong,
                TenPhong = k.MaPhongNavigation.TenPhong,
                GiaThue = k.MaPhongNavigation.GiaThue,
                MaTinh = k.MaTinh,
                TenTinh = k.TenTinh,
                MaPhuongXa = k.MaPhuongXa,
                TenPhuongXa = k.TenPhuongXa,
                QueQuan = string.IsNullOrWhiteSpace(k.TenPhuongXa) ? k.TenTinh : $"{k.TenPhuongXa} - {k.TenTinh}",
                DaDangKyTamTru = k.DaDangKyTamTru
            }).ToList();
        }, "tải danh sách khách thuê");

        dgv.DataSource = ok ? rows : new List<KhachRow>();
        dgv.ClearSelection();
    }

    private void BoLoc()
    {
        _binding = true;
        try
        {
            txtTim.Clear();
            if (cboLocPhong.Items.Count > 0) cboLocPhong.SelectedIndex = 0;
            if (cboLocTinh.Items.Count > 0) cboLocTinh.SelectedIndex = 0;
        }
        finally { _binding = false; }
        LoadGrid();
    }

    /// <summary>Cập nhật TinhTrang của các phòng theo số khách hiện có (dùng chung một DbContext/transaction).</summary>
    private static void CapNhatTinhTrang(QlNhaTroContext db, params int[] maPhongs)
    {
        foreach (int id in maPhongs.Distinct())
        {
            var p = db.PhongTros.Find(id);
            if (p == null) continue;
            bool coKhach = db.KhachThues.Any(k => k.MaPhong == id);
            p.TinhTrang = coKhach ? "Đang thuê" : "Trống";
        }
        db.SaveChanges();
    }

    // =====================================================================
    //  API ĐỊA CHỈ
    // =====================================================================
    private static void SetEditable(ComboBox cbo, bool editable) =>
        cbo.DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;

    private async Task LoadProvincesAsync()
    {
        _loadingAddr = true;
        linkTaiLai.Visible = false;

        _binding = true;
        SetEditable(cboTinh, false);
        SetEditable(cboPhuong, false);
        cboTinh.DataSource = null;
        cboTinh.Items.Clear();
        cboTinh.Items.Add("Đang tải…");
        cboTinh.SelectedIndex = 0;
        cboPhuong.DataSource = null;
        cboPhuong.Items.Clear();
        cboTinh.Enabled = false;
        cboPhuong.Enabled = false;
        _binding = false;
        tsStatus.Text = "Đang tải danh sách tỉnh/thành…";

        try
        {
            var list = await DiaChiApi.LayTinhAsync();

            _binding = true;
            cboTinh.Items.Clear();
            cboTinh.DisplayMember = "name";
            cboTinh.ValueMember = "code";
            cboTinh.DataSource = list;
            cboTinh.SelectedIndex = -1;
            cboTinh.Enabled = true;
            _binding = false;

            _tinhLoaded = true;
            tsStatus.Text = $"Đã tải {list.Count} tỉnh/thành từ provinces.open-api.vn";
            Log.Information("Đã tải {Count} tỉnh/thành từ API", list.Count);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _binding = true;
            _tinhLoaded = false;
            cboTinh.DataSource = null;
            cboTinh.Items.Clear();
            SetEditable(cboTinh, true);          // cho gõ tay
            SetEditable(cboPhuong, true);
            cboTinh.Text = "";
            cboPhuong.Text = "";
            cboTinh.Enabled = true;
            cboPhuong.Enabled = true;
            _binding = false;

            linkTaiLai.Visible = true;
            tsStatus.Text = "Không tải được danh sách địa chỉ, vui lòng kiểm tra mạng";
            Log.Warning(ex, "Không tải được danh sách tỉnh/thành từ API - chuyển sang nhập tay");
            MessageBox.Show(
                "Không tải được danh sách địa chỉ, vui lòng kiểm tra mạng.\nBạn vẫn có thể gõ tay Tỉnh/Thành và Phường/Xã để lưu khách.",
                "Quản lý nhà trọ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            _loadingAddr = false;
        }
    }

    private async void CboTinh_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_binding) return;
        if (cboTinh.SelectedItem is DiaChi t)
            await LoadWardsAsync(t.code, null, null);
    }

    /// <summary>Nạp phường/xã của một tỉnh; có thể chọn sẵn theo mã hoặc tên (khi sửa khách).</summary>
    private async Task LoadWardsAsync(int maTinh, int? chonMa, string? chonTen)
    {
        _wardCts?.Cancel();
        var cts = new CancellationTokenSource();
        _wardCts = cts;

        _loadingAddr = true;
        _binding = true;
        SetEditable(cboPhuong, false);
        cboPhuong.DataSource = null;
        cboPhuong.Items.Clear();
        cboPhuong.Items.Add("Đang tải…");
        cboPhuong.SelectedIndex = 0;
        cboPhuong.Enabled = false;
        _binding = false;
        tsStatus.Text = "Đang tải danh sách phường/xã…";

        try
        {
            var list = await DiaChiApi.LayPhuongXaAsync(maTinh, cts.Token);
            if (cts.IsCancellationRequested) return;

            _binding = true;
            cboPhuong.Items.Clear();
            cboPhuong.DisplayMember = "name";
            cboPhuong.ValueMember = "code";
            cboPhuong.DataSource = list;
            cboPhuong.SelectedIndex = -1;
            cboPhuong.Enabled = true;

            if (chonMa != null || !string.IsNullOrWhiteSpace(chonTen))
            {
                var px = (chonMa != null ? list.FirstOrDefault(x => x.code == chonMa) : null)
                         ?? list.FirstOrDefault(x => string.Equals(x.name, chonTen, StringComparison.OrdinalIgnoreCase));
                if (px != null) cboPhuong.SelectedItem = px;
                else if (!string.IsNullOrWhiteSpace(chonTen))
                {
                    SetEditable(cboPhuong, true);   // phường cũ không còn trong danh sách -> cho gõ tay
                    cboPhuong.Text = chonTen;
                }
            }
            _binding = false;

            tsStatus.Text = $"Đã tải {list.Count} phường/xã";
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Người dùng đã chọn tỉnh khác -> bỏ qua kết quả cũ
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            if (cts.IsCancellationRequested) return;

            _binding = true;
            cboPhuong.DataSource = null;
            cboPhuong.Items.Clear();
            SetEditable(cboPhuong, true);
            cboPhuong.Text = chonTen ?? "";
            cboPhuong.Enabled = true;
            _binding = false;

            tsStatus.Text = "Không tải được danh sách địa chỉ, vui lòng kiểm tra mạng";
            Log.Warning(ex, "Không tải được danh sách phường/xã của tỉnh {MaTinh} - chuyển sang nhập tay", maTinh);
            MessageBox.Show(
                "Không tải được danh sách địa chỉ, vui lòng kiểm tra mạng.\nBạn vẫn có thể gõ tay Phường/Xã để lưu khách.",
                "Quản lý nhà trọ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            if (_wardCts == cts) _loadingAddr = false;
        }
    }

    /// <summary>Hiện địa chỉ của khách đang chọn lên hai ComboBox.</summary>
    private async Task HienDiaChiAsync(KhachRow r)
    {
        if (_tinhLoaded && cboTinh.DataSource is List<DiaChi> ds)
        {
            var t = (r.MaTinh != null ? ds.FirstOrDefault(x => x.code == r.MaTinh) : null)
                    ?? ds.FirstOrDefault(x => string.Equals(x.name, r.TenTinh, StringComparison.OrdinalIgnoreCase));
            if (t != null)
            {
                _binding = true;
                SetEditable(cboTinh, false);
                cboTinh.SelectedItem = t;
                _binding = false;
                await LoadWardsAsync(t.code, r.MaPhuongXa, r.TenPhuongXa);
                return;
            }
        }

        // Không có trong danh sách (hoặc mất mạng) -> hiển thị dạng gõ tay
        _binding = true;
        SetEditable(cboTinh, true);
        SetEditable(cboPhuong, true);
        cboPhuong.DataSource = null;
        cboPhuong.Items.Clear();
        cboTinh.Text = r.TenTinh;
        cboPhuong.Text = r.TenPhuongXa;
        cboPhuong.Enabled = true;
        _binding = false;
    }

    // =====================================================================
    //  ĐỌC FORM + KIỂM TRA
    // =====================================================================
    private void Canh(string msg, Control focus)
    {
        MessageBox.Show(msg, "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        focus.Focus();
    }

    private KhachThue? ReadForm()
    {
        string hoTen = txtHoTen.Text.Trim();
        if (hoTen.Length == 0) { Canh("Họ tên không được để trống.", txtHoTen); return null; }

        string sdt = txtSdt.Text.Trim();
        if (!SdtRegex.IsMatch(sdt)) { Canh("Số điện thoại phải gồm 9–11 chữ số.", txtSdt); return null; }

        if (dtpNgaySinh.Value.Date > DateTime.Today) { Canh("Ngày sinh không được lớn hơn hôm nay.", dtpNgaySinh); return null; }

        if (cboPhong.SelectedValue is not int maPhong) { Canh("Vui lòng chọn phòng.", cboPhong); return null; }

        if (_loadingAddr) { Canh("Danh sách địa chỉ đang được tải, vui lòng đợi giây lát.", cboTinh); return null; }

        int? maTinh = null; string tenTinh;
        if (cboTinh.SelectedItem is DiaChi t) { maTinh = t.code; tenTinh = t.name; }
        else tenTinh = cboTinh.Text.Trim();
        if (tenTinh.Length == 0) { Canh("Vui lòng chọn (hoặc nhập) Tỉnh/Thành.", cboTinh); return null; }

        int? maPx = null; string tenPx;
        if (cboPhuong.SelectedItem is DiaChi p) { maPx = p.code; tenPx = p.name; }
        else tenPx = cboPhuong.Text.Trim();
        if (tenPx.Length == 0) { Canh("Vui lòng chọn (hoặc nhập) Phường/Xã.", cboPhuong); return null; }

        return new KhachThue
        {
            HoTen = hoTen,
            GioiTinh = rbNam.Checked ? "Nam" : "Nữ",
            NgaySinh = DateOnly.FromDateTime(dtpNgaySinh.Value),
            SoDienThoai = sdt,
            MaTinh = maTinh,
            TenTinh = tenTinh,
            MaPhuongXa = maPx,
            TenPhuongXa = tenPx,
            DaDangKyTamTru = chkTamTru.Checked,
            MaPhong = maPhong
        };
    }

    private void ClearInput()
    {
        _selectedId = null;
        txtHoTen.Clear();
        rbNam.Checked = true;
        dtpNgaySinh.Value = new DateTime(2000, 1, 1);
        txtSdt.Clear();
        chkTamTru.Checked = false;
        chkOGhep.Checked = false;
        RefreshPhongCombo(null, null);

        _binding = true;
        if (_tinhLoaded)
        {
            SetEditable(cboTinh, false);
            SetEditable(cboPhuong, false);
            cboTinh.SelectedIndex = -1;
            cboPhuong.DataSource = null;
            cboPhuong.Items.Clear();
            cboPhuong.Enabled = false;
        }
        else
        {
            cboTinh.Text = "";
            cboPhuong.Text = "";
        }
        _binding = false;

        dgv.ClearSelection();
        txtHoTen.Focus();
    }

    // =====================================================================
    //  NÚT CRUD
    // =====================================================================
    private void BtnThem_Click(object? sender, EventArgs e)
    {
        var k = ReadForm();
        if (k == null) return;

        bool ok = Run(() =>
        {
            using var db = DbFactory.Create();
            using var tx = db.Database.BeginTransaction();
            db.KhachThues.Add(k);
            db.SaveChanges();
            CapNhatTinhTrang(db, k.MaPhong);          // phòng -> "Đang thuê"
            tx.Commit();
        }, "thêm khách thuê");
        if (!ok) return;

        Log.Information("Thêm khách MaKhach={MaKhach}, MaPhong={MaPhong}", k.MaKhach, k.MaPhong);
        RefreshAll();
        ClearInput();
    }

    private void BtnSua_Click(object? sender, EventArgs e)
    {
        if (_selectedId is null)
        {
            MessageBox.Show("Hãy bấm chọn một khách trong danh sách để sửa.", "Thông báo",
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
            using var tx = db.Database.BeginTransaction();
            var cur = db.KhachThues.Find(id);
            if (cur == null) { notFound = true; return; }

            int phongCu = cur.MaPhong;
            cur.HoTen = input.HoTen;
            cur.GioiTinh = input.GioiTinh;
            cur.NgaySinh = input.NgaySinh;
            cur.SoDienThoai = input.SoDienThoai;
            cur.MaTinh = input.MaTinh;
            cur.TenTinh = input.TenTinh;
            cur.MaPhuongXa = input.MaPhuongXa;
            cur.TenPhuongXa = input.TenPhuongXa;
            cur.DaDangKyTamTru = input.DaDangKyTamTru;
            cur.MaPhong = input.MaPhong;
            db.SaveChanges();

            CapNhatTinhTrang(db, phongCu, input.MaPhong);   // phòng cũ có thể về "Trống"
            tx.Commit();
        }, "sửa khách thuê");
        if (!ok) return;

        if (notFound)
            MessageBox.Show("Khách này không còn tồn tại (có thể đã bị xóa).", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        else
            Log.Information("Sửa khách MaKhach={MaKhach}", id);

        RefreshAll();
        ClearInput();
    }

    private void BtnXoa_Click(object? sender, EventArgs e)
    {
        if (_selectedId is null)
        {
            MessageBox.Show("Hãy bấm chọn một khách trong danh sách để xóa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int id = _selectedId.Value;
        var confirm = MessageBox.Show($"Bạn có chắc muốn xóa khách \"{txtHoTen.Text}\" không?",
            "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

        bool ok = Run(() =>
        {
            using var db = DbFactory.Create();
            using var tx = db.Database.BeginTransaction();
            var cur = db.KhachThues.Find(id);
            if (cur == null) return;

            int phong = cur.MaPhong;
            db.KhachThues.Remove(cur);
            db.SaveChanges();
            CapNhatTinhTrang(db, phong);               // xóa người cuối -> "Trống"
            tx.Commit();
        }, "xóa khách thuê");
        if (!ok) return;

        Log.Information("Xóa khách MaKhach={MaKhach}", id);
        RefreshAll();
        ClearInput();
    }

    private async void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (dgv.Rows[e.RowIndex].DataBoundItem is not KhachRow r) return;

        _selectedId = r.MaKhach;
        txtHoTen.Text = r.HoTen;
        rbNam.Checked = r.GioiTinh == "Nam";
        rbNu.Checked = r.GioiTinh != "Nam";

        var ns = r.NgaySinh.Date;
        if (ns > dtpNgaySinh.MaxDate) ns = dtpNgaySinh.MaxDate;
        if (ns < dtpNgaySinh.MinDate) ns = dtpNgaySinh.MinDate;
        dtpNgaySinh.Value = ns;

        txtSdt.Text = r.SoDienThoai;
        chkTamTru.Checked = r.DaDangKyTamTru;
        RefreshPhongCombo(r.MaPhong, r.MaPhong);   // luôn có phòng hiện tại trong danh sách

        await HienDiaChiAsync(r);
    }
}
