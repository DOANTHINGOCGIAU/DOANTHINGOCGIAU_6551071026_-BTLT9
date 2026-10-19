/* =====================================================
   Script tạo CSDL QL_ChiTieu + dữ liệu mẫu
   Chạy trong SSMS (hoặc VS: SQL Server Object Explorer)
   ===================================================== */
IF DB_ID(N'QL_ChiTieu') IS NULL
    CREATE DATABASE QL_ChiTieu;
GO
USE QL_ChiTieu;
GO

IF OBJECT_ID(N'dbo.ChiTieu', N'U') IS NULL
CREATE TABLE dbo.ChiTieu
(
    MaCT    int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChiTieu PRIMARY KEY,
    NgayChi date          NOT NULL,
    NoiDung nvarchar(100) NOT NULL,
    LoaiChi nvarchar(30)  NOT NULL,
    SoTien  decimal(18,0) NOT NULL,
    GhiChu  nvarchar(200) NULL
);
GO

-- Dữ liệu mẫu (ngày tính lùi từ hôm nay để luôn có dữ liệu "tháng này")
IF NOT EXISTS (SELECT 1 FROM dbo.ChiTieu)
BEGIN
    DECLARE @h date = CAST(GETDATE() AS date);
    INSERT INTO dbo.ChiTieu (NgayChi, NoiDung, LoaiChi, SoTien, GhiChu) VALUES
    (DATEADD(DAY,-40,@h), N'Cơm tối',            N'Ăn uống',  45000,  NULL),
    (DATEADD(DAY,-35,@h), N'Tiền điện',          N'Khác',     500000, N'Hóa đơn tháng trước'),
    (DATEADD(DAY,-11,@h), N'Quà sinh nhật bạn',  N'Khác',     200000, N'Mừng sinh nhật Lan'),
    (DATEADD(DAY,-10,@h), N'Chơi game',          N'Giải trí', 50000,  NULL),
    (DATEADD(DAY, -9,@h), N'Grab đi học',        N'Đi lại',   55000,  NULL),
    (DATEADD(DAY, -8,@h), N'Ăn sáng',            N'Ăn uống',  25000,  NULL),
    (DATEADD(DAY, -7,@h), N'Gửi xe tháng',       N'Đi lại',   80000,  NULL),
    (DATEADD(DAY, -6,@h), N'Mua sách tham khảo', N'Học tập',  120000, NULL),
    (DATEADD(DAY, -5,@h), N'Trà sữa',            N'Ăn uống',  40000,  NULL),
    (DATEADD(DAY, -4,@h), N'Xem phim',           N'Giải trí', 90000,  N'Đi với nhóm bạn'),
    (DATEADD(DAY, -3,@h), N'Photo giáo trình',   N'Học tập',  25000,  NULL),
    (DATEADD(DAY, -2,@h), N'Xăng xe',            N'Đi lại',   60000,  NULL),
    (DATEADD(DAY, -1,@h), N'Cơm trưa',           N'Ăn uống',  35000,  NULL),
    (@h,                  N'Cà phê với bạn',     N'Ăn uống',  45000,  NULL);
END
GO

SELECT * FROM dbo.ChiTieu ORDER BY NgayChi, MaCT;
