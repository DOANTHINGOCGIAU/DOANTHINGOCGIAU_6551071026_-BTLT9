# Quản lý nhà trọ (WinForms + EF Core DB First + API Tỉnh/Phường-Xã)

1. Chạy `Database/QL_NhaTro.sql` trong SSMS.
2. Mở `QLNhaTro.sln` bằng Visual Studio, sửa server trong `QLNhaTro/appsettings.Development.json`, bấm F5.
3. Nút ẩn "Gây lỗi thử": nhấn Ctrl+Shift+F9 trên form để hiện/ẩn.
4. Thử mất mạng: tắt Wi-Fi rồi mở app -> có thông báo thân thiện, Tỉnh/Phường gõ tay được.
5. Log: `Logs\log-yyyyMMdd.txt` cạnh file .exe.

Scaffold lại (nếu muốn tự sinh Models):
    Scaffold-DbContext "Server=.\SQLEXPRESS;Database=QL_NhaTro;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Context QlNhaTroContext -NoOnConfiguring -Force
