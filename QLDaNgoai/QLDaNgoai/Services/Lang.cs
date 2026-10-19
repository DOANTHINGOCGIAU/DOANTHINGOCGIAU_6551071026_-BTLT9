using System.Globalization;

namespace QLDaNgoai.Services;

/// <summary>Đa ngôn ngữ Việt / Anh: bảng chuỗi + CultureInfo tương ứng.</summary>
public static class Lang
{
    private static readonly CultureInfo ViCulture = new("vi-VN");
    private static readonly CultureInfo EnCulture = new("en-US");

    public static string Code { get; private set; } = "vi";
    public static CultureInfo Culture => Code == "en" ? EnCulture : ViCulture;

    public static void Set(string? code)
    {
        Code = code == "en" ? "en" : "vi";
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
    }

    public static string T(string key) =>
        Table.TryGetValue(key, out var p) ? (Code == "en" ? p.en : p.vi) : key;

    public static string T(string key, params object[] args) => string.Format(Culture, T(key), args);

    /// <summary>Tên phương tiện hiển thị (giá trị lưu trong CSDL luôn là tiếng Việt).</summary>
    public static string Vehicle(string vi) =>
        Table.ContainsKey("veh_" + vi) ? T("veh_" + vi) : vi;

    private static readonly Dictionary<string, (string vi, string en)> Table = new()
    {
        ["formTitle"] = ("Kế hoạch dã ngoại", "Picnic planner"),
        ["title"] = ("Kế hoạch dã ngoại cuối tuần", "Weekend picnic planner"),
        ["language"] = ("Ngôn ngữ", "Language"),
        ["grpInfo"] = ("Thông tin chuyến đi", "Trip details"),
        ["lblTen"] = ("Tên chuyến", "Trip name"),
        ["lblDiaDiem"] = ("Địa điểm", "Destination"),
        ["lblNgayDi"] = ("Ngày đi", "Date"),
        ["lblSoNguoi"] = ("Số người", "People"),
        ["lblNganSach"] = ("Ngân sách", "Budget"),
        ["lblPhuongTien"] = ("Phương tiện", "Transport"),
        ["lblGhiChu"] = ("Ghi chú", "Notes"),
        ["chkHoanThanh"] = ("Đã hoàn thành", "Completed"),
        ["btnThem"] = ("Thêm", "Add"),
        ["btnSua"] = ("Sửa", "Edit"),
        ["btnXoa"] = ("Xóa", "Delete"),
        ["btnLamMoi"] = ("Làm mới", "Reset"),
        ["btnThoiTiet"] = ("Xem thời tiết", "Check weather"),
        ["grpThoiTiet"] = ("Dự báo thời tiết", "Weather forecast"),
        ["weatherTitle"] = ("Dự báo thời tiết ngày {0} - {1}", "Forecast for {0} - {1}"),
        ["hintWeather"] = ("Chọn một chuyến rồi bấm \"Xem thời tiết\"", "Select a trip, then click \"Check weather\""),
        ["temp"] = ("Nhiệt độ: {0}°C - {1}°C", "Temperature: {0}°C - {1}°C"),
        ["rain"] = ("Khả năng mưa: {0}%", "Chance of rain: {0}%"),
        ["rainUnknown"] = ("Khả năng mưa: không rõ", "Chance of rain: unknown"),
        ["warn"] = ("Nên mang áo mưa hoặc cân nhắc dời lịch", "Bring a raincoat or consider rescheduling"),
        ["noData"] = ("Chưa có dữ liệu dự báo", "No forecast data yet"),
        ["errPlace"] = ("Không tìm thấy địa danh \"{0}\". Hãy thử tên không dấu, ví dụ \"Da Lat\".",
                        "Place \"{0}\" was not found. Try the name without accents, e.g. \"Da Lat\"."),
        ["errNetwork"] = ("Không lấy được dự báo thời tiết. Vui lòng kiểm tra kết nối mạng rồi thử lại.",
                          "Could not load the forecast. Please check your network connection and try again."),
        ["w_clear"] = ("Trời quang", "Clear sky"),
        ["w_cloud"] = ("Có mây", "Partly cloudy"),
        ["w_fog"] = ("Sương mù", "Fog"),
        ["w_rain"] = ("Mưa", "Rain"),
        ["w_snow"] = ("Tuyết", "Snow"),
        ["w_storm"] = ("Dông", "Thunderstorm"),
        ["w_other"] = ("Không xác định", "Unknown"),
        ["searchHint"] = ("Tìm tên chuyến hoặc địa điểm...", "Search trip name or destination..."),
        ["chkChuaXong"] = ("Chỉ hiện chuyến chưa hoàn thành", "Only show unfinished trips"),
        ["chkLocNgay"] = ("Lọc theo ngày đi", "Filter by date"),
        ["btnTim"] = ("Tìm", "Search"),
        ["btnBoLoc"] = ("Bỏ lọc", "Clear"),
        ["colMa"] = ("Mã", "ID"),
        ["colTen"] = ("Tên chuyến", "Trip name"),
        ["colDiaDiem"] = ("Địa điểm", "Destination"),
        ["colNgayDi"] = ("Ngày đi", "Date"),
        ["colSoNguoi"] = ("Số người", "People"),
        ["colNganSach"] = ("Ngân sách", "Budget"),
        ["colPhuongTien"] = ("Phương tiện", "Transport"),
        ["colXong"] = ("Hoàn thành", "Done"),
        ["colGhiChu"] = ("Ghi chú", "Notes"),
        ["veh_Xe máy"] = ("Xe máy", "Motorbike"),
        ["veh_Ô tô"] = ("Ô tô", "Car"),
        ["veh_Xe khách"] = ("Xe khách", "Coach"),
        ["veh_Tàu hỏa"] = ("Tàu hỏa", "Train"),
        ["tInvalid"] = ("Dữ liệu chưa hợp lệ", "Invalid input"),
        ["tNotice"] = ("Thông báo", "Notice"),
        ["tError"] = ("Lỗi", "Error"),
        ["tConfirm"] = ("Xác nhận xóa", "Confirm delete"),
        ["errTen"] = ("Vui lòng nhập tên chuyến.", "Please enter the trip name."),
        ["errDiaDiem"] = ("Vui lòng nhập địa điểm.", "Please enter the destination."),
        ["errNgayDi"] = ("Ngày đi không được nhỏ hơn hôm nay.", "The trip date cannot be earlier than today."),
        ["errSoNguoi"] = ("Số người phải từ 1 trở lên.", "The number of people must be at least 1."),
        ["errNganSach"] = ("Ngân sách phải lớn hơn hoặc bằng 0.", "The budget must be 0 or more."),
        ["errPhuongTien"] = ("Vui lòng chọn phương tiện.", "Please choose a transport."),
        ["errLen"] = ("Nội dung quá dài (tối đa {0} ký tự).", "Text is too long (max {0} characters)."),
        ["pickRow"] = ("Hãy bấm chọn một chuyến trong danh sách.", "Please select a trip in the list."),
        ["confirmDelete"] = ("Bạn có chắc muốn xóa chuyến \"{0}\" không?", "Are you sure you want to delete \"{0}\"?"),
        ["notFound"] = ("Chuyến đi này không còn tồn tại (có thể đã bị xóa).", "This trip no longer exists (it may have been deleted)."),
        ["dbError"] = ("Không thể truy cập cơ sở dữ liệu. Vui lòng kiểm tra kết nối (appsettings.json) rồi thử lại.",
                       "Cannot access the database. Please check the connection (appsettings.json) and try again."),
        ["errFromTo"] = ("\"Từ ngày\" không được lớn hơn \"Đến ngày\".", "\"From\" date cannot be later than \"To\" date.")
    };
}
