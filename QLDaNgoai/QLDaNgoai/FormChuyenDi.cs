using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QLDaNgoai.Data;
using QLDaNgoai.Models;
using QLDaNgoai.Services;
using Serilog;

namespace QLDaNgoai;

public sealed record Option(string Value, string Text);

/// <summary>Một dòng của DataGridView.</summary>
public sealed class TripRow
{
    public int MaChuyen { get; set; }
    public string TenChuyen { get; set; } = "";
    public string DiaDiem { get; set; } = "";
    public DateTime NgayDi { get; set; }
    public int SoNguoi { get; set; }
    public decimal NganSach { get; set; }
    public string PhuongTienGoc { get; set; } = "";   // giá trị lưu trong CSDL (tiếng Việt)
    public string PhuongTien { get; set; } = "";      // tên hiển thị theo ngôn ngữ
    public bool DaHoanThanh { get; set; }
    public string? GhiChu { get; set; }
}

public class FormChuyenDi : Form
{
    private static readonly string[] PhuongTienGoc = { "Xe máy", "Ô tô", "Xe khách", "Tàu hỏa" };
    private enum WState { None, Ok, NoData, NotFound, Error }

    // ----- Tiêu đề + ngôn ngữ -----
    private readonly Label lblTieuDe = new();
    private readonly Label lblNgonNgu = new();
    private readonly ComboBox cboNgonNgu = new();

    // ----- Nhập liệu -----
    private readonly GroupBox gbInfo = new();
    private readonly Label lblTen = new(), lblDiaDiem = new(), lblNgayDi = new(), lblSoNguoi = new(),
                           lblNganSach = new(), lblPT = new(), lblGhiChu = new();
    private readonly TextBox txtTen = new(), txtDiaDiem = new(), txtGhiChu = new();
    private readonly DateTimePicker dtpNgayDi = new();
    private readonly NumericUpDown nudSoNguoi = new(), nudNganSach = new();
    private readonly ComboBox cboPT = new();
    private readonly CheckBox chkHoanThanh = new();
    private readonly Button btnThem = new(), btnSua = new(), btnXoa = new(), btnLamMoi = new();

    // ----- Thời tiết -----
    private readonly Button btnThoiTiet = new();
    private readonly ProgressBar progress = new();
    private readonly GroupBox gbThoiTiet = new();
    private readonly Label lblIcon = new(), lblMoTa = new(), lblNhiet = new(), lblMua = new(), lblCanhBao = new();
    private readonly Panel panelCanhBao = new();

    // ----- Lọc -----
    private readonly TextBox txtTim = new();
    private readonly CheckBox chkChuaXong = new(), chkLocNgay = new();
    private readonly DateTimePicker dtpTu = new(), dtpDen = new();
    private readonly Button btnTim = new(), btnBoLoc = new();

    // ----- Lưới -----
    private readonly DataGridView dgv = new();
    private readonly DataGridViewTextBoxColumn colMa = new(), colTen = new(), colDiaDiem = new(), colNgay = new(),
        colSo = new(), colNgan = new(), colPT = new(), colGhiChu = new();
    private readonly DataGridViewCheckBoxColumn colXong = new();

    // ----- Trạng thái -----
    private int? _selectedId;
    private bool _binding = true;
    private WState _wState = WState.None;
    private WeatherInfo? _wInfo;
    private string _wPlace = "";
    private DateOnly _wDate;

    public FormChuyenDi()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        ClientSize = new Size(1000, 700);
        MinimumSize = new Size(1016, 640);
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        Load += (_, _) => InitForm();
    }

    // =====================================================================
    //  GIAO DIỆN
    // =====================================================================
    private static void Place(Control c, Control parent, int x, int y, int w = 0, int h = 0)
    {
        c.Location = new Point(x, y);
        if (w > 0) c.Size = new Size(w, h > 0 ? h : c.Height);
        parent.Controls.Add(c);
    }

    private static void PlaceLabel(Label l, Control parent, int x, int y)
    {
        l.AutoSize = true;
        l.Location = new Point(x, y);
        parent.Controls.Add(l);
    }

    private void BuildUi()
    {
        // ---------- Tiêu đề ----------
        lblTieuDe.AutoSize = true;
        lblTieuDe.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblTieuDe.Location = new Point(12, 8);

        lblNgonNgu.AutoSize = true;
        lblNgonNgu.Location = new Point(790, 15);
        lblNgonNgu.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        cboNgonNgu.DropDownStyle = ComboBoxStyle.DropDownList;
        cboNgonNgu.SetBounds(860, 11, 128, 25);
        cboNgonNgu.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        Controls.AddRange(new Control[] { lblTieuDe, lblNgonNgu, cboNgonNgu });

        // ---------- Thông tin chuyến đi ----------
        gbInfo.Location = new Point(12, 50);
        gbInfo.Size = new Size(470, 312);

        PlaceLabel(lblTen, gbInfo, 15, 30);       Place(txtTen, gbInfo, 135, 26, 320);
        PlaceLabel(lblDiaDiem, gbInfo, 15, 60);   Place(txtDiaDiem, gbInfo, 135, 56, 320);
        PlaceLabel(lblNgayDi, gbInfo, 15, 90);    Place(dtpNgayDi, gbInfo, 135, 86, 150);
        PlaceLabel(lblSoNguoi, gbInfo, 15, 120);  Place(nudSoNguoi, gbInfo, 135, 116, 80);
        PlaceLabel(lblNganSach, gbInfo, 15, 150); Place(nudNganSach, gbInfo, 135, 146, 150);
        PlaceLabel(lblPT, gbInfo, 15, 180);       Place(cboPT, gbInfo, 135, 176, 150);
        Place(chkHoanThanh, gbInfo, 135, 208);
        PlaceLabel(lblGhiChu, gbInfo, 15, 240);   Place(txtGhiChu, gbInfo, 135, 236, 320);

        txtTen.MaxLength = 100;
        txtDiaDiem.MaxLength = 100;
        txtGhiChu.MaxLength = 200;
        dtpNgayDi.Format = DateTimePickerFormat.Custom;
        nudSoNguoi.Minimum = 1;
        nudSoNguoi.Maximum = 1000;
        nudSoNguoi.Value = 1;
        nudNganSach.Minimum = 0;
        nudNganSach.Maximum = 999999999999M;
        nudNganSach.Increment = 100000;
        nudNganSach.ThousandsSeparator = true;
        nudNganSach.TextAlign = HorizontalAlignment.Right;
        cboPT.DropDownStyle = ComboBoxStyle.DropDownList;
        chkHoanThanh.AutoSize = true;

        foreach (var b in new[] { btnThem, btnSua, btnXoa, btnLamMoi }) b.Size = new Size(100, 30);
        Place(btnThem, gbInfo, 15, 270);
        Place(btnSua, gbInfo, 125, 270);
        Place(btnXoa, gbInfo, 235, 270);
        Place(btnLamMoi, gbInfo, 345, 270);
        Controls.Add(gbInfo);

        // ---------- Thời tiết ----------
        btnThoiTiet.SetBounds(500, 50, 160, 30);
        progress.Style = ProgressBarStyle.Marquee;
        progress.MarqueeAnimationSpeed = 30;
        progress.Visible = false;
        progress.SetBounds(670, 56, 318, 18);
        progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        gbThoiTiet.SetBounds(500, 90, 488, 272);
        gbThoiTiet.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        lblIcon.Font = new Font("Segoe UI Emoji", 44F);
        lblIcon.TextAlign = ContentAlignment.MiddleCenter;
        lblIcon.SetBounds(15, 35, 130, 100);

        lblMoTa.Font = new Font("Segoe UI", 14F);
        lblMoTa.SetBounds(155, 40, 320, 62);
        lblNhiet.SetBounds(155, 108, 320, 20);
        lblMua.SetBounds(155, 132, 320, 20);

        panelCanhBao.SetBounds(15, 190, 458, 60);
        panelCanhBao.BackColor = Color.FromArgb(255, 224, 178);
        panelCanhBao.Visible = false;
        lblCanhBao.Dock = DockStyle.Fill;
        lblCanhBao.Padding = new Padding(10, 0, 0, 0);
        lblCanhBao.TextAlign = ContentAlignment.MiddleLeft;
        lblCanhBao.Font = new Font("Segoe UI", 10F);
        panelCanhBao.Controls.Add(lblCanhBao);

        gbThoiTiet.Controls.AddRange(new Control[] { lblIcon, lblMoTa, lblNhiet, lblMua, panelCanhBao });
        Controls.AddRange(new Control[] { btnThoiTiet, progress, gbThoiTiet });

        // ---------- Lọc ----------
        txtTim.SetBounds(12, 372, 220, 25);
        chkChuaXong.AutoSize = true;
        chkChuaXong.Location = new Point(242, 375);
        chkLocNgay.AutoSize = true;
        chkLocNgay.Location = new Point(470, 375);
        foreach (var d in new[] { dtpTu, dtpDen }) d.Format = DateTimePickerFormat.Custom;
        dtpTu.SetBounds(590, 372, 105, 25);
        dtpDen.SetBounds(705, 372, 105, 25);
        dtpTu.Value = DateTime.Today;
        dtpDen.Value = DateTime.Today.AddDays(30);
        dtpTu.Enabled = dtpDen.Enabled = false;
        btnTim.SetBounds(820, 370, 80, 28);
        btnBoLoc.SetBounds(908, 370, 80, 28);
        Controls.AddRange(new Control[] { txtTim, chkChuaXong, chkLocNgay, dtpTu, dtpDen, btnTim, btnBoLoc });

        // ---------- DataGridView ----------
        dgv.SetBounds(12, 408, 976, 280);
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

        colMa.DataPropertyName = nameof(TripRow.MaChuyen); colMa.FillWeight = 35;
        colTen.DataPropertyName = nameof(TripRow.TenChuyen); colTen.FillWeight = 150;
        colDiaDiem.DataPropertyName = nameof(TripRow.DiaDiem); colDiaDiem.FillWeight = 90;
        colNgay.DataPropertyName = nameof(TripRow.NgayDi); colNgay.FillWeight = 80;
        colNgay.DefaultCellStyle.Format = "d";                          // theo CultureInfo hiện hành
        colSo.DataPropertyName = nameof(TripRow.SoNguoi); colSo.FillWeight = 55;
        colNgan.DataPropertyName = nameof(TripRow.NganSach); colNgan.FillWeight = 90;
        colNgan.DefaultCellStyle.Format = "#,##0' ₫'";                  // dấu phân cách theo CultureInfo
        colNgan.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        colPT.DataPropertyName = nameof(TripRow.PhuongTien); colPT.FillWeight = 80;
        colXong.DataPropertyName = nameof(TripRow.DaHoanThanh); colXong.FillWeight = 65;
        colGhiChu.DataPropertyName = nameof(TripRow.GhiChu); colGhiChu.FillWeight = 120;

        dgv.Columns.AddRange(colMa, colTen, colDiaDiem, colNgay, colSo, colNgan, colPT, colXong, colGhiChu);
        Controls.Add(dgv);

        // ---------- Sự kiện ----------
        btnThem.Click += BtnThem_Click;
        btnSua.Click += BtnSua_Click;
        btnXoa.Click += BtnXoa_Click;
        btnLamMoi.Click += (_, _) => { ClearInput(); BoLoc(); };
        btnThoiTiet.Click += BtnThoiTiet_Click;
        btnTim.Click += BtnTim_Click;
        btnBoLoc.Click += (_, _) => BoLoc();
        dgv.CellClick += Dgv_CellClick;
        cboNgonNgu.SelectedIndexChanged += CboNgonNgu_SelectedIndexChanged;

        txtTim.TextChanged += (_, _) => LoadData();
        chkChuaXong.CheckedChanged += (_, _) => LoadData();
        dtpTu.ValueChanged += (_, _) => LoadData();
        dtpDen.ValueChanged += (_, _) => LoadData();
        chkLocNgay.CheckedChanged += (_, _) =>
        {
            dtpTu.Enabled = dtpDen.Enabled = chkLocNgay.Checked;
            LoadData();
        };
    }

    // =====================================================================
    //  KHỞI TẠO + ĐA NGÔN NGỮ
    // =====================================================================
    private void InitForm()
    {
        _binding = true;
        cboNgonNgu.DisplayMember = nameof(Option.Text);
        cboNgonNgu.ValueMember = nameof(Option.Value);
        cboNgonNgu.DataSource = new List<Option> { new("vi", "Tiếng Việt"), new("en", "English") };
        cboNgonNgu.SelectedValue = Lang.Code;
        _binding = false;

        dtpNgayDi.Value = DateTime.Today;
        ApplyLanguage();      // đặt toàn bộ chữ + nạp lưới lần đầu
    }

    private void CboNgonNgu_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_binding) return;
        if (cboNgonNgu.SelectedValue is not string code) return;

        Lang.Set(code);
        Program.User.Language = Lang.Code;
        Program.User.Save();               // lưu vào %LocalAppData%\KeHoachDaNgoai\usersettings.json
        Log.Information("Đổi ngôn ngữ sang {Lang}", Lang.Code);
        ApplyLanguage();
    }

    /// <summary>Đổi toàn bộ nhãn, nút, tiêu đề cột, định dạng ngày/tiền ngay lập tức.</summary>
    private void ApplyLanguage()
    {
        _binding = true;
        try
        {
            Text = Lang.T("formTitle");
            lblTieuDe.Text = Lang.T("title");
            lblNgonNgu.Text = Lang.T("language");

            gbInfo.Text = Lang.T("grpInfo");
            lblTen.Text = Lang.T("lblTen");
            lblDiaDiem.Text = Lang.T("lblDiaDiem");
            lblNgayDi.Text = Lang.T("lblNgayDi");
            lblSoNguoi.Text = Lang.T("lblSoNguoi");
            lblNganSach.Text = Lang.T("lblNganSach");
            lblPT.Text = Lang.T("lblPhuongTien");
            lblGhiChu.Text = Lang.T("lblGhiChu");
            chkHoanThanh.Text = Lang.T("chkHoanThanh");
            btnThem.Text = Lang.T("btnThem");
            btnSua.Text = Lang.T("btnSua");
            btnXoa.Text = Lang.T("btnXoa");
            btnLamMoi.Text = Lang.T("btnLamMoi");
            btnThoiTiet.Text = Lang.T("btnThoiTiet");

            txtTim.PlaceholderText = Lang.T("searchHint");
            chkChuaXong.Text = Lang.T("chkChuaXong");
            chkLocNgay.Text = Lang.T("chkLocNgay");
            btnTim.Text = Lang.T("btnTim");
            btnBoLoc.Text = Lang.T("btnBoLoc");

            // Định dạng ngày theo culture; số tiền tự theo culture nhờ CultureInfo.CurrentCulture
            string fmt = Lang.Culture.DateTimeFormat.ShortDatePattern;
            dtpNgayDi.CustomFormat = fmt;
            dtpTu.CustomFormat = fmt;
            dtpDen.CustomFormat = fmt;
            nudNganSach.ThousandsSeparator = false;     // bật/tắt để ô số vẽ lại theo culture mới
            nudNganSach.ThousandsSeparator = true;
            nudSoNguoi.ThousandsSeparator = false;

            // Danh sách phương tiện (giữ nguyên giá trị đang chọn)
            string? sel = cboPT.SelectedValue as string;
            cboPT.DataSource = null;
            cboPT.DisplayMember = nameof(Option.Text);
            cboPT.ValueMember = nameof(Option.Value);
            cboPT.DataSource = PhuongTienGoc.Select(v => new Option(v, Lang.Vehicle(v))).ToList();
            cboPT.SelectedValue = sel ?? PhuongTienGoc[0];

            // Tiêu đề cột
            colMa.HeaderText = Lang.T("colMa");
            colTen.HeaderText = Lang.T("colTen");
            colDiaDiem.HeaderText = Lang.T("colDiaDiem");
            colNgay.HeaderText = Lang.T("colNgayDi");
            colSo.HeaderText = Lang.T("colSoNguoi");
            colNgan.HeaderText = Lang.T("colNganSach");
            colPT.HeaderText = Lang.T("colPhuongTien");
            colXong.HeaderText = Lang.T("colXong");
            colGhiChu.HeaderText = Lang.T("colGhiChu");
        }
        finally { _binding = false; }

        LoadData();
        RenderWeather();
    }

    // =====================================================================
    //  CSDL + LỌC
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
            MessageBox.Show(Lang.T("dbError"), Lang.T("tError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void LoadData()
    {
        if (_binding) return;

        string kw = txtTim.Text.Trim();
        bool chuaXong = chkChuaXong.Checked;
        bool locNgay = chkLocNgay.Checked;
        var tu = DateOnly.FromDateTime(dtpTu.Value);
        var den = DateOnly.FromDateTime(dtpDen.Value);

        var rows = new List<TripRow>();
        if (!(locNgay && tu > den))
        {
            bool ok = Run(() =>
            {
                using var db = DbFactory.Create();
                IQueryable<ChuyenDi> q = db.ChuyenDis.AsNoTracking();

                if (kw.Length > 0) q = q.Where(x => x.TenChuyen.Contains(kw) || x.DiaDiem.Contains(kw));
                if (chuaXong) q = q.Where(x => !x.DaHoanThanh);
                if (locNgay) q = q.Where(x => x.NgayDi >= tu && x.NgayDi <= den);

                var list = q.OrderBy(x => x.NgayDi).ThenBy(x => x.MaChuyen).ToList();
                rows = list.Select(x => new TripRow
                {
                    MaChuyen = x.MaChuyen,
                    TenChuyen = x.TenChuyen,
                    DiaDiem = x.DiaDiem,
                    NgayDi = x.NgayDi.ToDateTime(TimeOnly.MinValue),
                    SoNguoi = x.SoNguoi,
                    NganSach = x.NganSach,
                    PhuongTienGoc = x.PhuongTien,
                    PhuongTien = Lang.Vehicle(x.PhuongTien),
                    DaHoanThanh = x.DaHoanThanh,
                    GhiChu = x.GhiChu
                }).ToList();
            }, "tải danh sách chuyến đi");
            if (!ok) rows = new List<TripRow>();
        }

        dgv.DataSource = rows;
        dgv.ClearSelection();
        _selectedId = null;
    }

    private void BoLoc()
    {
        _binding = true;
        try
        {
            txtTim.Clear();
            chkChuaXong.Checked = false;
            chkLocNgay.Checked = false;
            dtpTu.Enabled = dtpDen.Enabled = false;
            dtpTu.Value = DateTime.Today;
            dtpDen.Value = DateTime.Today.AddDays(30);
        }
        finally { _binding = false; }
        LoadData();
    }

    private void BtnTim_Click(object? sender, EventArgs e)
    {
        if (chkLocNgay.Checked && dtpTu.Value.Date > dtpDen.Value.Date)
        {
            Canh(Lang.T("errFromTo"), dtpTu);
            return;
        }
        LoadData();
    }

    // =====================================================================
    //  KIỂM TRA + CRUD
    // =====================================================================
    private void Canh(string msg, Control focus)
    {
        MessageBox.Show(msg, Lang.T("tInvalid"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        focus.Focus();
    }

    private ChuyenDi? ReadForm(bool laThemMoi)
    {
        string ten = txtTen.Text.Trim();
        if (ten.Length == 0) { Canh(Lang.T("errTen"), txtTen); return null; }

        string dia = txtDiaDiem.Text.Trim();
        if (dia.Length == 0) { Canh(Lang.T("errDiaDiem"), txtDiaDiem); return null; }

        if (laThemMoi && dtpNgayDi.Value.Date < DateTime.Today) { Canh(Lang.T("errNgayDi"), dtpNgayDi); return null; }
        if (nudSoNguoi.Value < 1) { Canh(Lang.T("errSoNguoi"), nudSoNguoi); return null; }
        if (nudNganSach.Value < 0) { Canh(Lang.T("errNganSach"), nudNganSach); return null; }
        if (cboPT.SelectedValue is not string pt) { Canh(Lang.T("errPhuongTien"), cboPT); return null; }

        string ghiChu = txtGhiChu.Text.Trim();

        return new ChuyenDi
        {
            TenChuyen = ten,
            DiaDiem = dia,
            NgayDi = DateOnly.FromDateTime(dtpNgayDi.Value),
            SoNguoi = (int)nudSoNguoi.Value,
            NganSach = nudNganSach.Value,
            PhuongTien = pt,
            DaHoanThanh = chkHoanThanh.Checked,
            GhiChu = ghiChu.Length == 0 ? null : ghiChu
        };
    }

    private void ClearInput()
    {
        _selectedId = null;
        txtTen.Clear();
        txtDiaDiem.Clear();
        dtpNgayDi.Value = DateTime.Today;
        nudSoNguoi.Value = 1;
        nudNganSach.Value = 0;
        cboPT.SelectedIndex = 0;
        chkHoanThanh.Checked = false;
        txtGhiChu.Clear();
        dgv.ClearSelection();
        txtTen.Focus();
    }

    private void BtnThem_Click(object? sender, EventArgs e)
    {
        var c = ReadForm(laThemMoi: true);
        if (c == null) return;

        bool ok = Run(() =>
        {
            using var db = DbFactory.Create();
            db.ChuyenDis.Add(c);
            db.SaveChanges();
        }, "thêm chuyến đi");
        if (!ok) return;

        Log.Information("Thêm chuyến MaChuyen={MaChuyen}, DiaDiem={DiaDiem}, NgayDi={NgayDi}", c.MaChuyen, c.DiaDiem, c.NgayDi);
        ClearInput();
        LoadData();
    }

    private void BtnSua_Click(object? sender, EventArgs e)
    {
        if (_selectedId is null)
        {
            MessageBox.Show(Lang.T("pickRow"), Lang.T("tNotice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var input = ReadForm(laThemMoi: false);
        if (input == null) return;

        int id = _selectedId.Value;
        bool notFound = false;

        bool ok = Run(() =>
        {
            using var db = DbFactory.Create();
            var cur = db.ChuyenDis.Find(id);
            if (cur == null) { notFound = true; return; }

            cur.TenChuyen = input.TenChuyen;
            cur.DiaDiem = input.DiaDiem;
            cur.NgayDi = input.NgayDi;
            cur.SoNguoi = input.SoNguoi;
            cur.NganSach = input.NganSach;
            cur.PhuongTien = input.PhuongTien;
            cur.DaHoanThanh = input.DaHoanThanh;
            cur.GhiChu = input.GhiChu;
            db.SaveChanges();
        }, "sửa chuyến đi");
        if (!ok) return;

        if (notFound) MessageBox.Show(Lang.T("notFound"), Lang.T("tNotice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        else Log.Information("Sửa chuyến MaChuyen={MaChuyen}", id);

        ClearInput();
        LoadData();
    }

    private void BtnXoa_Click(object? sender, EventArgs e)
    {
        if (_selectedId is null)
        {
            MessageBox.Show(Lang.T("pickRow"), Lang.T("tNotice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int id = _selectedId.Value;
        var confirm = MessageBox.Show(Lang.T("confirmDelete", txtTen.Text), Lang.T("tConfirm"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

        bool ok = Run(() =>
        {
            using var db = DbFactory.Create();
            var cur = db.ChuyenDis.Find(id);
            if (cur != null)
            {
                db.ChuyenDis.Remove(cur);
                db.SaveChanges();
            }
        }, "xóa chuyến đi");
        if (!ok) return;

        Log.Information("Xóa chuyến MaChuyen={MaChuyen}", id);
        ClearInput();
        LoadData();
    }

    private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (dgv.Rows[e.RowIndex].DataBoundItem is not TripRow r) return;

        _selectedId = r.MaChuyen;
        txtTen.Text = r.TenChuyen;
        txtDiaDiem.Text = r.DiaDiem;

        var d = r.NgayDi.Date;
        if (d < dtpNgayDi.MinDate) d = dtpNgayDi.MinDate;
        if (d > dtpNgayDi.MaxDate) d = dtpNgayDi.MaxDate;
        dtpNgayDi.Value = d;

        nudSoNguoi.Value = Math.Min(Math.Max(r.SoNguoi, nudSoNguoi.Minimum), nudSoNguoi.Maximum);
        nudNganSach.Value = Math.Min(Math.Max(r.NganSach, nudNganSach.Minimum), nudNganSach.Maximum);
        cboPT.SelectedValue = r.PhuongTienGoc;
        chkHoanThanh.Checked = r.DaHoanThanh;
        txtGhiChu.Text = r.GhiChu ?? "";
    }

    // =====================================================================
    //  THỜI TIẾT
    // =====================================================================
    private async void BtnThoiTiet_Click(object? sender, EventArgs e)
    {
        string diaDiem = txtDiaDiem.Text.Trim();
        if (diaDiem.Length == 0) { Canh(Lang.T("errDiaDiem"), txtDiaDiem); return; }

        var ngay = DateOnly.FromDateTime(dtpNgayDi.Value);
        _wPlace = diaDiem;
        _wDate = ngay;

        btnThoiTiet.Enabled = false;          // khóa nút trong lúc tải
        progress.Visible = true;              // ProgressBar kiểu Marquee
        try
        {
            var kq = await WeatherService.LayThoiTietAsync(diaDiem, ngay, Lang.Code);
            switch (kq.Status)
            {
                case LookupStatus.Ok:
                    _wState = WState.Ok;
                    _wInfo = kq.Info;
                    Log.Information("Đã lấy thời tiết {DiaDiem} ngày {Ngay}", diaDiem, ngay);
                    break;
                case LookupStatus.PlaceNotFound:
                    _wState = WState.NotFound;
                    Log.Warning("Không tìm thấy địa danh {DiaDiem}", diaDiem);
                    break;
                default:
                    _wState = WState.NoData;
                    break;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _wState = WState.Error;
            Log.Warning(ex, "Lỗi mạng khi lấy thời tiết cho {DiaDiem}", diaDiem);
        }
        finally
        {
            progress.Visible = false;
            btnThoiTiet.Enabled = true;
        }

        RenderWeather();
    }

    private static (string icon, string key) Describe(int code) => code switch
    {
        0 => ("☀", "w_clear"),
        >= 1 and <= 3 => ("⛅", "w_cloud"),
        45 or 48 => ("🌫", "w_fog"),
        (>= 51 and <= 67) or (>= 80 and <= 82) => ("🌧", "w_rain"),
        (>= 71 and <= 77) or 85 or 86 => ("❄", "w_snow"),
        >= 95 and <= 99 => ("⛈", "w_storm"),
        _ => ("🌡", "w_other")
    };

    /// <summary>Vẽ lại khung thời tiết theo trạng thái hiện tại + ngôn ngữ hiện tại.</summary>
    private void RenderWeather()
    {
        panelCanhBao.Visible = false;
        lblNhiet.Text = "";
        lblMua.Text = "";
        gbThoiTiet.Text = Lang.T("grpThoiTiet");

        string ngay = _wDate.ToString("d", Lang.Culture);

        switch (_wState)
        {
            case WState.None:
                lblIcon.Text = "🌍";
                lblMoTa.Font = new Font("Segoe UI", 11F);
                lblMoTa.Text = Lang.T("hintWeather");
                break;

            case WState.NoData:
                gbThoiTiet.Text = Lang.T("weatherTitle", ngay, _wPlace);
                lblIcon.Text = "🕓";
                lblMoTa.Font = new Font("Segoe UI", 12F);
                lblMoTa.Text = Lang.T("noData");
                break;

            case WState.NotFound:
                lblIcon.Text = "🔍";
                lblMoTa.Font = new Font("Segoe UI", 11F);
                lblMoTa.Text = Lang.T("errPlace", _wPlace);
                break;

            case WState.Error:
                lblIcon.Text = "📡";
                lblMoTa.Font = new Font("Segoe UI", 11F);
                lblMoTa.Text = Lang.T("errNetwork");
                break;

            case WState.Ok when _wInfo != null:
                var (icon, key) = Describe(_wInfo.Code);
                gbThoiTiet.Text = Lang.T("weatherTitle", ngay, _wPlace);
                lblIcon.Text = icon;
                lblMoTa.Font = new Font("Segoe UI", 14F);
                lblMoTa.Text = Lang.T(key);
                lblNhiet.Text = Lang.T("temp", Math.Round(_wInfo.TMin), Math.Round(_wInfo.TMax));
                lblMua.Text = _wInfo.RainProb is int p ? Lang.T("rain", p) : Lang.T("rainUnknown");

                if (_wInfo.RainProb is >= 60)
                {
                    lblCanhBao.Text = "⚠  " + Lang.T("warn");
                    panelCanhBao.Visible = true;             // nền cam khi xác suất mưa >= 60%
                }
                break;
        }
    }
}
