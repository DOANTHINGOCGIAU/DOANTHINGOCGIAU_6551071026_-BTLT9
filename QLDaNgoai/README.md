# Kế hoạch dã ngoại (WinForms + EF Core DB First + Open-Meteo + Việt/Anh)

1. Chạy `Database/QL_DaNgoai.sql` trong SSMS (8 chuyến, ngày đi từ +3 đến +25 ngày kể từ hôm nay).
2. Mở `QLDaNgoai.sln` bằng Visual Studio, sửa server trong `QLDaNgoai/appsettings.Development.json`, bấm F5.
3. Chọn chuyến -> bấm "Xem thời tiết" (cần Internet).
4. Ngôn ngữ lưu ở `%LocalAppData%\KeHoachDaNgoai\usersettings.json`.

Địa chỉ API dùng:
- https://geocoding-api.open-meteo.com/v1/search?name=...&count=1&language=vi
- https://api.open-meteo.com/v1/forecast?latitude=..&longitude=..&daily=...&timezone=auto&forecast_days=16
