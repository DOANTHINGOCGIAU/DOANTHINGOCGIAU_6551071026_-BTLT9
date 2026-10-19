/* Script tạo CSDL QL_NhaTro + dữ liệu mẫu */
IF DB_ID(N'QL_NhaTro') IS NULL
    CREATE DATABASE QL_NhaTro;
GO
USE QL_NhaTro;
GO

IF OBJECT_ID(N'dbo.PhongTro', N'U') IS NULL
CREATE TABLE dbo.PhongTro
(
    MaPhong   int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PhongTro PRIMARY KEY,
    TenPhong  nvarchar(20)  NOT NULL,
    GiaThue   decimal(18,0) NOT NULL,
    TinhTrang nvarchar(20)  NOT NULL CONSTRAINT DF_PhongTro_TinhTrang DEFAULT N'Trống'
);
GO

IF OBJECT_ID(N'dbo.KhachThue', N'U') IS NULL
CREATE TABLE dbo.KhachThue
(
    MaKhach        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_KhachThue PRIMARY KEY,
    HoTen          nvarchar(100) NOT NULL,
    GioiTinh       nvarchar(5)   NOT NULL,
    NgaySinh       date          NOT NULL,
    SoDienThoai    varchar(15)   NOT NULL,
    MaTinh         int           NULL,
    TenTinh        nvarchar(100) NOT NULL,
    MaPhuongXa     int           NULL,
    TenPhuongXa    nvarchar(100) NOT NULL,
    DaDangKyTamTru bit           NOT NULL CONSTRAINT DF_KhachThue_TamTru DEFAULT 0,
    MaPhong        int           NOT NULL,
    CONSTRAINT FK_KhachThue_PhongTro FOREIGN KEY (MaPhong) REFERENCES dbo.PhongTro(MaPhong)
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PhongTro)
BEGIN
    INSERT INTO dbo.PhongTro (TenPhong, GiaThue, TinhTrang) VALUES
    (N'P01', 1500000, N'Đang thuê'),
    (N'P02', 1600000, N'Đang thuê'),
    (N'P03', 1800000, N'Đang thuê'),
    (N'P04', 2000000, N'Đang thuê'),
    (N'P05', 1700000, N'Đang thuê'),
    (N'P06', 1500000, N'Trống');

    DECLARE @p01 int = (SELECT MaPhong FROM dbo.PhongTro WHERE TenPhong = N'P01');
    DECLARE @p02 int = (SELECT MaPhong FROM dbo.PhongTro WHERE TenPhong = N'P02');
    DECLARE @p03 int = (SELECT MaPhong FROM dbo.PhongTro WHERE TenPhong = N'P03');
    DECLARE @p04 int = (SELECT MaPhong FROM dbo.PhongTro WHERE TenPhong = N'P04');
    DECLARE @p05 int = (SELECT MaPhong FROM dbo.PhongTro WHERE TenPhong = N'P05');

    -- Mã tỉnh/phường là mã của provinces.open-api.vn (có thể để NULL nếu nhập tay)
    INSERT INTO dbo.KhachThue (HoTen, GioiTinh, NgaySinh, SoDienThoai, MaTinh, TenTinh, MaPhuongXa, TenPhuongXa, DaDangKyTamTru, MaPhong) VALUES
    (N'Nguyễn Văn An',   N'Nam', '2004-05-12', '0912345678', 1,  N'Thành phố Hà Nội', NULL, N'Phường Ba Đình',  1, @p03),
    (N'Nguyễn Văn Minh', N'Nam', '2003-08-02', '0905111222', 1,  N'Thành phố Hà Nội', NULL, N'Phường Ngọc Hà',  0, @p03),
    (N'Trần Thị Bình',   N'Nữ',  '2001-03-20', '0876543210', 37, N'Tỉnh Ninh Bình',   NULL, N'Phường Nam Định', 0, @p01),
    (N'Lê Văn Cường',    N'Nam', '2000-11-09', '0901234567', 38, N'Tỉnh Thanh Hóa',   NULL, N'Phường Hạc Thành', 0, @p02),
    (N'Phạm Minh Đức',   N'Nam', '1999-01-25', '0987654321', 38, N'Tỉnh Thanh Hóa',   NULL, N'Phường Hạc Thành', 1, @p04),
    (N'Hoàng Thị Dung',  N'Nữ',  '2002-07-30', '0765432109', 27, N'Tỉnh Bắc Ninh',    NULL, N'Phường Bắc Ninh',  0, @p05);
END
GO

SELECT * FROM dbo.PhongTro;
SELECT * FROM dbo.KhachThue;
