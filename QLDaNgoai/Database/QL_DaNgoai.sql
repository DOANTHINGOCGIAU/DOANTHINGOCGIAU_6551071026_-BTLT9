/* Script tạo CSDL QL_DaNgoai + 8 chuyến mẫu (ngày đi tính từ hôm nay) */
IF DB_ID(N'QL_DaNgoai') IS NULL
    CREATE DATABASE QL_DaNgoai;
GO
USE QL_DaNgoai;
GO

IF OBJECT_ID(N'dbo.ChuyenDi', N'U') IS NULL
CREATE TABLE dbo.ChuyenDi
(
    MaChuyen     int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChuyenDi PRIMARY KEY,
    TenChuyen    nvarchar(100) NOT NULL,
    DiaDiem      nvarchar(100) NOT NULL,
    NgayDi       date          NOT NULL,
    SoNguoi      int           NOT NULL CONSTRAINT DF_ChuyenDi_SoNguoi DEFAULT 1,
    NganSach     decimal(18,0) NOT NULL CONSTRAINT DF_ChuyenDi_NganSach DEFAULT 0,
    PhuongTien   nvarchar(30)  NOT NULL,
    DaHoanThanh  bit           NOT NULL CONSTRAINT DF_ChuyenDi_Xong DEFAULT 0,
    GhiChu       nvarchar(200) NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ChuyenDi)
BEGIN
    DECLARE @h date = CAST(GETDATE() AS date);
    INSERT INTO dbo.ChuyenDi (TenChuyen, DiaDiem, NgayDi, SoNguoi, NganSach, PhuongTien, DaHoanThanh, GhiChu) VALUES
    (N'Cắm trại đồi chè',      N'Đà Lạt',   DATEADD(DAY, 3,@h), 6, 3500000, N'Ô tô',     0, N'Mang lều và đồ nướng'),
    (N'Săn mây Fansipan',      N'Sa Pa',    DATEADD(DAY, 5,@h), 4, 5200000, N'Xe khách', 0, NULL),
    (N'Tắm biển cuối tuần',    N'Vũng Tàu', DATEADD(DAY, 7,@h), 8, 2400000, N'Xe máy',   0, N'Đi sáng sớm tránh kẹt xe'),
    (N'Phố cổ về đêm',         N'Hội An',   DATEADD(DAY, 9,@h), 3, 6800000, N'Tàu hỏa',  0, N'Thả đèn hoa đăng'),
    (N'Lặn ngắm san hô',       N'Nha Trang',DATEADD(DAY,12,@h), 5, 7500000, N'Tàu hỏa',  0, NULL),
    (N'Thăm Đại Nội',          N'Huế',      DATEADD(DAY,14,@h), 2, 4300000, N'Ô tô',     0, NULL),
    (N'Nghỉ dưỡng đảo ngọc',   N'Phú Quốc', DATEADD(DAY,20,@h), 6, 12000000,N'Ô tô',    0, N'Chưa có dự báo (quá 16 ngày)'),
    (N'Ngắm hoa mận',          N'Mộc Châu', DATEADD(DAY,25,@h), 4, 3000000, N'Xe khách', 0, NULL);
END
GO

SELECT * FROM dbo.ChuyenDi ORDER BY NgayDi;
