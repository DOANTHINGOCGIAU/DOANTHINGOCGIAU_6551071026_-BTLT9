# Sổ chi tiêu cá nhân (WinForms + EF Core Database First)

## Chạy bằng F5 trong Visual Studio
1. Chạy `Database/QL_ChiTieu.sql` trong SSMS để tạo CSDL + 14 dòng dữ liệu mẫu.
2. Mở `QLChiTieu.sln` bằng Visual Studio.
3. Sửa tên server trong `QLChiTieu/appsettings.Development.json` cho đúng máy bạn
   (thử: `.\SQLEXPRESS`, `.`, hoặc `(localdb)\MSSQLLocalDB`).
4. Bấm **F5** (hoặc Ctrl+F5).

## Cấu hình
- `appsettings.json`: cấu hình gốc (dùng khi publish / môi trường Production).
- `appsettings.Development.json`: ghi đè khi chạy F5 (đã có trong .gitignore).
- `appsettings.example.json`: file mẫu để commit lên GitHub.
- Đổi connection string -> chạy lại là kết nối CSDL khác, không cần build lại.

## Log
`Logs\log-yyyyMMdd.txt` cạnh file .exe (Information: Thêm/Sửa/Xóa kèm MaCT; Error: lỗi CSDL).

## Publish Framework-Dependent, win-x64
Visual Studio: Build > Publish > chọn profile `FolderProfile`.
Hoặc dòng lệnh:
    dotnet publish QLChiTieu/QLChiTieu.csproj -c Release -r win-x64 --self-contained false -o publish
Chép thư mục `publish` sang máy khác (đã cài .NET 8 Desktop Runtime), sửa
`appsettings.json` cho đúng máy đó rồi chạy `QLChiTieu.exe`.

## Scaffold lại từ CSDL (nếu muốn tự sinh)
Package Manager Console (Default project = QLChiTieu):
    Scaffold-DbContext "Server=.\SQLEXPRESS;Database=QL_ChiTieu;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Context QlChiTieuContext -NoOnConfiguring -Force
(Các file Models/ChiTieu.cs và Models/QlChiTieuContext.cs có sẵn giống kết quả scaffold.)
