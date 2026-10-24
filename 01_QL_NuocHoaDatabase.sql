/* ============================================================
   01_CreateDatabase.sql
   Dự án : Website bán nước hoa - Đồ án cuối kỳ
   DBMS  : Microsoft SQL Server
   Mục đích: Tạo database PerfumeShopDB.
   LƯU Ý : Nếu database đã tồn tại, script sẽ XÓA và tạo lại
           (dùng để reset database demo).
   ============================================================ */

USE master;
GO

IF DB_ID(N'PerfumeShopDB') IS NOT NULL
BEGIN
    ALTER DATABASE PerfumeShopDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE PerfumeShopDB;
END
GO

CREATE DATABASE PerfumeShopDB
    COLLATE Vietnamese_CI_AS;
GO

USE PerfumeShopDB;
GO

PRINT N'Đã tạo database PerfumeShopDB.';
GO
-- XÓA BẢNG CŨ NẾU ĐÃ TỒN TẠI
IF OBJECT_ID('dbo.ProductReviews', 'U') IS NOT NULL DROP TABLE dbo.ProductReviews;
IF OBJECT_ID('dbo.OrderItems', 'U')     IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders', 'U')         IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.CartItems', 'U')      IS NOT NULL DROP TABLE dbo.CartItems;
IF OBJECT_ID('dbo.Carts', 'U')          IS NOT NULL DROP TABLE dbo.Carts;
IF OBJECT_ID('dbo.ProductImages', 'U')  IS NOT NULL DROP TABLE dbo.ProductImages;
IF OBJECT_ID('dbo.ProductVariants', 'U')IS NOT NULL DROP TABLE dbo.ProductVariants;
IF OBJECT_ID('dbo.Products', 'U')       IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Categories', 'U')     IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Brands', 'U')         IS NOT NULL DROP TABLE dbo.Brands;
IF OBJECT_ID('dbo.Coupons', 'U')        IS NOT NULL DROP TABLE dbo.Coupons;
IF OBJECT_ID('dbo.Users', 'U')          IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U')          IS NOT NULL DROP TABLE dbo.Roles;
GO

/* ============================================================
   TẠO 13 BẢNG CẤU TRÚC  

   NGUYÊN TẮC: Database chỉ giữ PK, FK, UNIQUE, NOT NULL, DEFAULT.
   Mọi việc kiểm tra giá trị và tính toán tiền do BACKEND làm.
   - [SỬA]    : cột/ràng buộc cấu trúc thêm so với bản gốc của nhóm
   - [BACKEND]: việc Backend phải kiểm tra (DB không chặn)
   ============================================================ */

-- 1. Roles
CREATE TABLE dbo.Roles (
    RoleID      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleName    NVARCHAR(50)      NOT NULL UNIQUE
);
GO

-- 2. Users
CREATE TABLE dbo.Users (
    UserID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleID        INT               NOT NULL FOREIGN KEY REFERENCES dbo.Roles(RoleID),
    FullName      NVARCHAR(100)     NOT NULL,
    Email         NVARCHAR(150)     NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(255)     NOT NULL,   -- [BACKEND] băm mật khẩu, KHÔNG lưu mật khẩu thô
    Phone         NVARCHAR(20)      NULL,
    Address       NVARCHAR(255)     NULL,
    IsActive      BIT               NOT NULL DEFAULT 1,
    CreatedAt     DATETIME2         NOT NULL DEFAULT SYSDATETIME()
);
GO

-- 3. Brands
CREATE TABLE dbo.Brands (
    BrandID      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    BrandName    NVARCHAR(100)     NOT NULL UNIQUE,
    Country      NVARCHAR(100)     NULL,
    Description  NVARCHAR(MAX)     NULL,
    IsActive     BIT               NOT NULL DEFAULT 1
);
GO

-- 4. Categories
CREATE TABLE dbo.Categories (
    CategoryID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    -- [SỬA] Cột cha để làm menu nhiều cấp (NULL = danh mục gốc)
    ParentCategoryID  INT               NULL
        CONSTRAINT FK_Categories_Parent FOREIGN KEY REFERENCES dbo.Categories(CategoryID),
    CategoryName      NVARCHAR(100)     NOT NULL,
    Slug              NVARCHAR(150)     NOT NULL UNIQUE,
    Description       NVARCHAR(255)     NULL,
    IsActive          BIT               NOT NULL DEFAULT 1
);
GO

-- 5. Products
CREATE TABLE dbo.Products (
    ProductID      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    BrandID        INT               NOT NULL FOREIGN KEY REFERENCES dbo.Brands(BrandID),
    CategoryID     INT               NOT NULL FOREIGN KEY REFERENCES dbo.Categories(CategoryID),
    ProductName    NVARCHAR(200)     NOT NULL,
    Slug           NVARCHAR(250)     NOT NULL UNIQUE,
    Gender         NVARCHAR(20)      NULL,   -- [BACKEND] chỉ nhận: Men / Women / Unisex
    Concentration  NVARCHAR(50)      NULL,   -- [BACKEND] chỉ nhận: Parfum / EDP / EDT / EDC / Extrait / Eau Fraiche
    Description    NVARCHAR(MAX)     NULL,
    IsFeatured     BIT               NOT NULL DEFAULT 0,
    IsActive       BIT               NOT NULL DEFAULT 1,
    CreatedAt      DATETIME2         NOT NULL DEFAULT SYSDATETIME()
);
GO

-- 6. ProductVariants
CREATE TABLE dbo.ProductVariants (
    VariantID      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductID      INT               NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID) ON DELETE CASCADE,
    VolumeML       INT               NOT NULL,           -- [BACKEND] > 0
    SKU            NVARCHAR(50)      NOT NULL UNIQUE,
    Price          DECIMAL(18,2)     NOT NULL,           -- [BACKEND] > 0
    SalePrice      DECIMAL(18,2)     NULL,               -- [BACKEND] NULL hoặc 0 < SalePrice <= Price
    StockQuantity  INT               NOT NULL DEFAULT 0, -- [BACKEND] >= 0, kiểm tra đủ hàng khi đặt
    IsActive       BIT               NOT NULL DEFAULT 1,
    -- [SỬA] Một sản phẩm không có 2 dòng cùng dung tích
    CONSTRAINT UQ_Variants_Product_Volume UNIQUE (ProductID, VolumeML)
);
GO

-- 7. ProductImages
CREATE TABLE dbo.ProductImages (
    ImageID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductID      INT               NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID) ON DELETE CASCADE,
    ImageURL       NVARCHAR(500)     NOT NULL,
    IsPrimary      BIT               NOT NULL DEFAULT 0   -- [BACKEND] mỗi sản phẩm chỉ 1 ảnh chính
);
GO

-- 8. Coupons
CREATE TABLE dbo.Coupons (
    CouponID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Code            NVARCHAR(50)      NOT NULL UNIQUE,
    DiscountValue   DECIMAL(18,2)     NOT NULL,           -- [BACKEND] > 0; nếu IsPercent=1 thì <= 100
    IsPercent       BIT               NOT NULL DEFAULT 0,
    MinOrderAmount  DECIMAL(18,2)     NOT NULL DEFAULT 0,
    ExpiryDate      DATETIME2         NOT NULL,           -- [BACKEND] kiểm tra còn hạn khi áp dụng
    IsActive        BIT               NOT NULL DEFAULT 1,
    -- [SỬA] Giới hạn số lần dùng
    UsageLimit      INT               NULL,               -- NULL = không giới hạn
    UsedCount       INT               NOT NULL DEFAULT 0  -- [BACKEND] +1 mỗi lần đặt hàng thành công
);
GO

-- 9. Carts
CREATE TABLE dbo.Carts (
    CartID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserID          INT               NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserID),
    CreatedAt       DATETIME2         NOT NULL DEFAULT SYSDATETIME(),
    -- [SỬA] Mỗi user chỉ có 1 giỏ hàng
    CONSTRAINT UQ_Carts_User UNIQUE (UserID)
);
GO

-- 10. CartItems
CREATE TABLE dbo.CartItems (
    CartItemID      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CartID          INT               NOT NULL FOREIGN KEY REFERENCES dbo.Carts(CartID) ON DELETE CASCADE,
    VariantID       INT               NOT NULL FOREIGN KEY REFERENCES dbo.ProductVariants(VariantID),
    Quantity        INT               NOT NULL DEFAULT 1,  -- [BACKEND] > 0
    -- [SỬA] Cùng một dung tích không thêm 2 dòng, chỉ tăng Quantity
    CONSTRAINT UQ_CartItems_Cart_Variant UNIQUE (CartID, VariantID)
);
GO

-- 11. Orders
CREATE TABLE dbo.Orders (
    OrderID          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderCode        NVARCHAR(30)      NOT NULL UNIQUE,
    UserID           INT               NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserID),
    CouponID         INT               NULL FOREIGN KEY REFERENCES dbo.Coupons(CouponID),
    ReceiverName     NVARCHAR(100)     NOT NULL,
    ReceiverPhone    NVARCHAR(20)      NOT NULL,
    ShippingAddress  NVARCHAR(255)     NOT NULL,
    -- [SỬA] Tách tiền hàng, giảm giá, phí ship (Backend tính rồi ghi vào)
    SubTotal         DECIMAL(18,2)     NOT NULL,
    DiscountAmount   DECIMAL(18,2)     NOT NULL DEFAULT 0,
    ShippingFee      DECIMAL(18,2)     NOT NULL DEFAULT 0,
    TotalAmount      DECIMAL(18,2)     NOT NULL,           -- [BACKEND] = SubTotal - DiscountAmount + ShippingFee
    OrderStatus      NVARCHAR(30)      NOT NULL DEFAULT N'Pending',
        -- [BACKEND] Pending / Confirmed / Processing / Shipping / Delivered / Cancelled / Returned / Refunded
    PaymentMethod    NVARCHAR(30)      NOT NULL DEFAULT N'COD',
        -- [BACKEND] COD / BankTransfer / VNPay / MoMo
    PaymentStatus    NVARCHAR(30)      NOT NULL DEFAULT N'Unpaid',
        -- [BACKEND] Unpaid / Paid / Failed / Refunded
    OrderDate        DATETIME2         NOT NULL DEFAULT SYSDATETIME()
);
GO

-- 12. OrderItems
CREATE TABLE dbo.OrderItems (
    OrderItemID      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderID          INT               NOT NULL FOREIGN KEY REFERENCES dbo.Orders(OrderID) ON DELETE CASCADE,
    VariantID        INT               NOT NULL FOREIGN KEY REFERENCES dbo.ProductVariants(VariantID),
    -- [SỬA] SNAPSHOT: Backend sao chép thông tin tại lúc mua, đổi tên/giá sau này không làm sai đơn cũ
    ProductNameSnapshot NVARCHAR(300)  NOT NULL,
    SKU              NVARCHAR(50)      NOT NULL,
    VolumeML         INT               NOT NULL,
    Quantity         INT               NOT NULL,           -- [BACKEND] > 0
    UnitPrice        DECIMAL(18,2)     NOT NULL,           -- [BACKEND] giá tại lúc mua (SalePrice nếu có, không thì Price)
    -- [SỬA] Một đơn không có 2 dòng cùng dung tích
    CONSTRAINT UQ_OrderItems_Order_Variant UNIQUE (OrderID, VariantID)
);
GO

-- 13. ProductReviews
CREATE TABLE dbo.ProductReviews (
    ReviewID         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductID        INT               NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID),
    UserID           INT               NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserID),
    -- [SỬA] Có OrderID nghĩa là "đã mua hàng"; NULL là chưa xác minh
    OrderID          INT               NULL FOREIGN KEY REFERENCES dbo.Orders(OrderID),
    Rating           INT               NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment          NVARCHAR(MAX)     NULL,
    -- [SỬA] Admin duyệt review trước khi hiển thị
    IsApproved       BIT               NOT NULL DEFAULT 0,
    CreatedAt        DATETIME2         NOT NULL DEFAULT SYSDATETIME(),
    -- [SỬA] Mỗi user chỉ đánh giá 1 lần cho mỗi sản phẩm
    CONSTRAINT UQ_ProductReviews_User_Product UNIQUE (UserID, ProductID)
);


/* ============================================================
   [TÙY CHỌN] THÊM 2 BẢNG NOTES (thành 15 bảng)
   Để tìm/lọc nước hoa theo note (vanilla, rose, oud...).
   Muốn dùng: bỏ dấu comment toàn bộ khối này, và thêm 2 dòng DROP
   (ProductNotes rồi Notes) vào phần XÓA BẢNG CŨ ở đầu file.
   ============================================================

CREATE TABLE dbo.Notes (
    NoteID    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    NoteName  NVARCHAR(80)      NOT NULL UNIQUE
);
GO

-- Khóa chính 3 cột: cùng một note có thể ở tầng khác nhau ở các sản phẩm khác nhau
CREATE TABLE dbo.ProductNotes (
    ProductID  INT          NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID) ON DELETE CASCADE,
    NoteID     INT          NOT NULL FOREIGN KEY REFERENCES dbo.Notes(NoteID),
    NoteType   NVARCHAR(10) NOT NULL,   -- [BACKEND] chỉ nhận: Top / Heart / Base
    PRIMARY KEY (ProductID, NoteID, NoteType)
);
GO

   ============================================================ */

/* ============================================================
   INSERT DỮ LIỆU MÌNH SẼ ĐƯA XUỐNG ĐÂY MỌI NGƯỜI NHỚ KHI CODE 
   DỮ LIỆU VÀO NHỚ ĐÁNH DẤU TÊN CÁI BẢNG MÌNH ĐƯA DỮU LIỆU VÀO NHA 
   
   ============================================================ */
   GO

-- ===== BẢNG 1/13: Roles =====
-- Vai trò phân quyền: 1 = Admin, 2 = Staff, 3 = Customer.
SET IDENTITY_INSERT dbo.Roles ON;
INSERT INTO dbo.Roles (RoleID, RoleName)
VALUES
    (1, N'Admin'   ),   
    (2, N'Staff'   ),
    (3, N'Customer');
SET IDENTITY_INSERT dbo.Roles OFF;
GO

-- ===== BẢNG 2/13: Users =====
-- Tài khoản đăng nhập: 1 admin, 1 nhân viên, 5 khách hàng (user 7 bị khóa, IsActive = 0).
-- Mật khẩu demo: admin = admin123, staff = staff123, khách hàng = 123456.
-- Cột PasswordHash đang để mật khẩu thường cho dễ demo; Backend xử lý/băm mật khẩu theo cách nhóm chọn.
-- Tên, email, SĐT, địa chỉ là dữ liệu giả.
SET IDENTITY_INSERT dbo.Users ON;
INSERT INTO dbo.Users (UserID, RoleID, FullName, Email, PasswordHash, Phone, Address, IsActive)
VALUES
    (1, 1, N'Nguyễn Quản Trị', N'admin@perfumeshop.vn',  N'admin123', N'0901000001', N'12 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh',              1),
    (2, 2, N'Trần Nhân Viên',  N'staff@perfumeshop.vn',  N'staff123', N'0901000002', N'12 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh',              1),
    (3, 3, N'Lê Minh Anh',     N'minhanh@example.com',   N'123456',   N'0901000003', N'25 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', 1),
    (4, 3, N'Phạm Quốc Bảo',   N'quocbao@example.com',   N'123456',   N'0901000004', N'88 Nguyễn Trãi, Quận 5, TP. Hồ Chí Minh',             1),
    (5, 3, N'Võ Thu Hà',       N'thuha@example.com',     N'123456',   N'0901000005', N'15 Trần Phú, Quận Hải Châu, Đà Nẵng',                 1),
    (6, 3, N'Đặng Hoàng Long', N'hoanglong@example.com', N'123456',   N'0901000006', N'102 Cầu Giấy, Quận Cầu Giấy, Hà Nội',                 1),
    (7, 3, N'Bùi Gia Hân',     N'giahan@example.com',    N'123456',   N'0901000007', N'7 Nguyễn Văn Linh, Quận 7, TP. Hồ Chí Minh',          0);
SET IDENTITY_INSERT dbo.Users OFF;
GO

-- ===== BẢNG 3/13: Brands =====
-- Thương hiệu nước hoa. Country để trống ở Parour vì chưa có nguồn.
SET IDENTITY_INSERT dbo.Brands ON;
INSERT INTO dbo.Brands (BrandID, BrandName, Country, IsActive)
VALUES
    ( 1, N'Parour',          NULL,        1),
    ( 2, N'Calvin Klein',    N'Mỹ',       1),
    ( 3, N'Ralph Lauren',    N'Mỹ',       1),   
    ( 4, N'Issey Miyake',    N'Nhật Bản', 1),
    ( 5, N'Hugo Boss',       N'Đức',      1),
    ( 6, N'Giorgio Armani',  N'Ý',        1),
    ( 7, N'Lolita Lempicka', N'Pháp',     1),
    ( 8, N'Thierry Mugler',  N'Pháp',     1),
    ( 9, N'Valentino',       N'Ý',        1),
    (10, N'Gucci',           N'Ý',        1),
    (11, N'Guerlain',        N'Pháp',     1),
    (12, N'Chanel',          N'Pháp',     1),
    (13, N'Creed',           N'Pháp',     1);
SET IDENTITY_INSERT dbo.Brands OFF;
GO

-- ===== BẢNG 4/13: Categories =====
-- Danh mục menu: Nam, Nữ, Unisex, Niche. ParentCategoryID = NULL nghĩa là danh mục gốc.
SET IDENTITY_INSERT dbo.Categories ON;
INSERT INTO dbo.Categories (CategoryID, ParentCategoryID, CategoryName, Slug, IsActive)
VALUES
    (1, NULL, N'Nước hoa Nam',    N'nuoc-hoa-nam',    1),
    (2, NULL, N'Nước hoa Nữ',     N'nuoc-hoa-nu',     1),
    (3, NULL, N'Nước hoa Unisex', N'nuoc-hoa-unisex', 1),
    (4, NULL, N'Nước hoa Niche',  N'nuoc-hoa-niche',  1);
SET IDENTITY_INSERT dbo.Categories OFF;
GO

-- ===== BẢNG 5/13: Products =====
-- Mỗi dòng là một mùi hương (chưa có giá, giá nằm ở ProductVariants).
-- Sản phẩm 1-14 lấy từ [TGNH], 15-21 lấy từ [HARRY]. Mô tả tự viết ngắn.
-- Gender chỉ nhận Men / Women / Unisex; Concentration chỉ nhận EDP / EDT / Parfum (Backend kiểm tra).
SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products (ProductID, BrandID, CategoryID, ProductName, Slug, Gender, Concentration, Description, IsFeatured, IsActive, CreatedAt)
VALUES
    (1, 1, 1, N'Parour Cigar Silver Essence For Men EDT',
        N'parour-cigar-silver-essence-for-men-edt',
        N'Men', N'EDT', N'Nước hoa nam, nồng độ Eau de Toilette.', 0, 1, N'2026-08-02T09:00:00'),
    (2, 1, 2, N'Parour Vanille Tonka For Women EDP',
        N'parour-vanille-tonka-for-women-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 0, 1, N'2026-08-03T09:00:00'),
    (3, 2, 2, N'Calvin Klein Eternity Aromatic Essence For Women EDP',
        N'calvin-klein-eternity-aromatic-essence-for-women-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 0, 1, N'2026-08-04T09:00:00'),
    (4, 2, 2, N'Calvin Klein My Euphoria EDP',
        N'calvin-klein-my-euphoria-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 0, 1, N'2026-08-05T09:00:00'),
    (5, 3, 2, N'Ralph Lauren Romance Intense EDP',
        N'ralph-lauren-romance-intense-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 0, 1, N'2026-08-06T09:00:00'),
    (6, 4, 1, N'Issey Miyake L''Eau d''Issey Pour Homme Solar Lavender EDTI',
        N'issey-miyake-leau-dissey-pour-homme-solar-lavender-edti',
        N'Men', N'EDT', N'Nước hoa nam, nồng độ Eau de Toilette.', 0, 1, N'2026-08-07T09:00:00'),
    (7, 5, 2, N'Hugo Boss Alive EDP',
        N'hugo-boss-alive-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 0, 1, N'2026-08-08T09:00:00'),
    (8, 5, 2, N'Boss The Scent EDP For Her',
        N'boss-the-scent-edp-for-her',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 1, 1, N'2026-08-09T09:00:00'),
    (9, 5, 2, N'Boss The Scent Magnetic EDP For Women',
        N'boss-the-scent-magnetic-edp-for-women',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 0, 1, N'2026-08-10T09:00:00'),
    (10, 5, 2, N'Boss The Scent Parfum For Her',
        N'boss-the-scent-parfum-for-her',
        N'Women', N'Parfum', N'Nước hoa nữ, nồng độ Parfum.', 0, 1, N'2026-08-11T09:00:00'),
    (11, 6, 2, N'Giorgio Armani My Way Parfum EDP',
        N'giorgio-armani-my-way-parfum-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 0, 1, N'2026-08-12T09:00:00'),
    (12, 7, 2, N'Lolita Lempicka EDP',
        N'lolita-lempicka-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 0, 1, N'2026-08-13T09:00:00'),
    (13, 8, 2, N'Mugler Alien EDP',
        N'mugler-alien-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 1, 1, N'2026-08-14T09:00:00'),
    (14, 8, 2, N'Mugler Alien Goddess EDP',
        N'mugler-alien-goddess-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 0, 1, N'2026-08-15T09:00:00'),
    (15, 9, 2, N'Valentino Donna Born In Roma Purple Melancholia EDP',
        N'valentino-donna-born-in-roma-purple-melancholia-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 1, 1, N'2026-10-01T09:00:00'),
    (16, 9, 1, N'Valentino Uomo Born In Roma Purple Melancholia EDT',
        N'valentino-uomo-born-in-roma-purple-melancholia-edt',
        N'Men', N'EDT', N'Nước hoa nam, nồng độ Eau de Toilette.', 1, 1, N'2026-10-02T09:00:00'),
    (17, 10, 2, N'Gucci Bloom Ambrosia d''Oro EDP',
        N'gucci-bloom-ambrosia-doro-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 0, 1, N'2026-10-03T09:00:00'),
    (18, 11, 2, N'Guerlain L''Heure Bleue EDP',
        N'guerlain-lheure-bleue-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 0, 1, N'2026-10-04T09:00:00'),
    (19, 12, 2, N'Chanel Coco Mademoiselle Crush Absolu EDP',
        N'chanel-coco-mademoiselle-crush-absolu-edp',
        N'Women', N'EDP', N'Nước hoa nữ, nồng độ Eau de Parfum.', 1, 1, N'2026-10-05T09:00:00'),
    (20, 13, 4, N'Creed Wild Vetiver EDP',
        N'creed-wild-vetiver-edp',
        N'Unisex', N'EDP', N'Nước hoa unisex, nồng độ Eau de Parfum.', 1, 1, N'2026-10-06T09:00:00'),
    (21, 13, 4, N'Creed Original Vetiver EDP'   ,
        N'creed-original-vetiver-edp',
        N'Unisex', N'EDP', N'Nước hoa unisex, nồng độ Eau de Parfum.', 0, 1, N'2026-10-07T09:00:00');
SET IDENTITY_INSERT dbo.Products OFF;
GO

-- ===== BẢNG 6/13: ProductVariants =====
-- Mỗi dòng là một dung tích: SKU, giá gốc (Price), giá đang bán (SalePrice, NULL = không giảm), tồn kho.
-- Dòng 4, 6, 8, 12, 18, 22 là dung tích/giá tự đặt [DEMO]; các dòng còn lại lấy từ [TGNH] và [HARRY].
-- Tồn kho là số demo; variant 25 để 0 để test hết hàng. Dung tích 100ml của [HARRY] suy ra từ tên file ảnh.
SET IDENTITY_INSERT dbo.ProductVariants ON;
INSERT INTO dbo.ProductVariants (VariantID, ProductID, VolumeML, SKU, Price, SalePrice, StockQuantity, IsActive)
VALUES
    ( 1,  1,  75, N'PAR-CSE-75',      2450000, 1290000, 20, 1),
    ( 2,  2,  90, N'PAR-VT-90',       2700000, 1390000, 15, 1),
    ( 3,  3,  30, N'CK-EAE-30',       1900000, 1290000, 25, 1),
    ( 4,  3,  50, N'CK-EAE-50',       2790000, 1890000, 12, 1),
    ( 5,  4,  30, N'CK-MYE-30',       1880000, 1290000, 30, 1),
    ( 6,  4,  50, N'CK-MYE-50',       2750000, 1890000, 10, 1),
    ( 7,  5,  30, N'RL-ROM-30',       1895000, 1290000, 18, 1),
    ( 8,  5,  50, N'RL-ROM-50',       2790000, 1890000,  8, 1),
    ( 9,  6,  50, N'IM-LEPH-SL-50',   2180000, 1390000, 14, 1),
    (10,  7,  30, N'HB-ALV-30',       1970000, 1390000, 22, 1),
    (11,  8,  30, N'HB-TS-HER-30',    2030000, 1390000, 40, 1),
    (12,  8,  50, N'HB-TS-HER-50',    2990000, 1990000, 16, 1),
    (13,  9,  30, N'HB-TSM-30',       2160000, 1490000, 16, 1),
    (14, 10,  30, N'HB-TSP-HER-30',   2260000, 1590000,  9, 1),
    (15, 11,  30, N'GA-MYWAY-30',     2515000, 1490000, 11, 1),
    (16, 12, 100, N'LL-EDP-100',      2959000, 1490000,  7, 1),
    (17, 13,  60, N'MUG-ALIEN-60',    3299000, 1590000, 13, 1),
    (18, 13,  30, N'MUG-ALIEN-30',    2100000, 1190000,  6, 1),
    (19, 14,  60, N'MUG-ALIENG-60',   3299000, 1590000,  5, 1),
    (20, 15, 100, N'VAL-DBIR-PM-100', 3750000, 3250000, 10, 1),
    (21, 16, 100, N'VAL-UBIR-PM-100', 3250000, 2750000, 10, 1),
    (22, 16,  50, N'VAL-UBIR-PM-50',  2150000, 1850000,  6, 1),
    (23, 17, 100, N'GUC-BAO-100',     3050000,    NULL,  8, 1),
    (24, 18, 100, N'GUE-LHB-100',     5900000,    NULL,  4, 1),
    (25, 19, 100, N'CHA-CMCA-100',    5500000,    NULL,  0, 1),
    (26, 20, 100, N'CRE-WV-100',      7500000,    NULL,  5, 1),
    (27, 21, 100, N'CRE-OV-100',      7500000,    NULL,  3, 1);
SET IDENTITY_INSERT dbo.ProductVariants OFF;
GO

-- ===== BẢNG 7/13: ProductImages =====
-- Ảnh sản phẩm, IsPrimary = 1 là ảnh đại diện. ImageURL là đường dẫn nội bộ /images/products/<slug>.jpg;
-- nhóm tự tải ảnh về hoặc đổi URL.
SET IDENTITY_INSERT dbo.ProductImages ON;
INSERT INTO dbo.ProductImages (ImageID, ProductID, ImageURL, IsPrimary)
VALUES
    ( 1,  1, N'/images/products/parour-cigar-silver-essence-for-men-edt.jpg',                 1),
    ( 2,  2, N'/images/products/parour-vanille-tonka-for-women-edp.jpg',                      1),
    ( 3,  3, N'/images/products/calvin-klein-eternity-aromatic-essence-for-women-edp.jpg',    1),
    ( 4,  4, N'/images/products/calvin-klein-my-euphoria-edp.jpg',                            1),
    ( 5,  5, N'/images/products/ralph-lauren-romance-intense-edp.jpg',                        1),
    ( 6,  6, N'/images/products/issey-miyake-leau-dissey-pour-homme-solar-lavender-edti.jpg', 1),
    ( 7,  7, N'/images/products/hugo-boss-alive-edp.jpg',                                     1),
    ( 8,  8, N'/images/products/boss-the-scent-edp-for-her.jpg',                              1),
    ( 9,  9, N'/images/products/boss-the-scent-magnetic-edp-for-women.jpg',                   1),
    (10, 10, N'/images/products/boss-the-scent-parfum-for-her.jpg',                           1),
    (11, 11, N'/images/products/giorgio-armani-my-way-parfum-edp.jpg',                        1),
    (12, 12, N'/images/products/lolita-lempicka-edp.jpg',                                     1),
    (13, 13, N'/images/products/mugler-alien-edp.jpg',                                        1),
    (14, 14, N'/images/products/mugler-alien-goddess-edp.jpg',                                1),
    (15, 15, N'/images/products/valentino-donna-born-in-roma-purple-melancholia-edp.jpg',     1),
    (16, 15, N'/images/products/valentino-donna-born-in-roma-purple-melancholia-edp-box.jpg', 0),
    (17, 16, N'/images/products/valentino-uomo-born-in-roma-purple-melancholia-edt.jpg',      1),
    (18, 16, N'/images/products/valentino-uomo-born-in-roma-purple-melancholia-edt-box.jpg',  0),
    (19, 17, N'/images/products/gucci-bloom-ambrosia-doro-edp.jpg',                           1),
    (20, 17, N'/images/products/gucci-bloom-ambrosia-doro-edp-box.jpg',                       0),
    (21, 18, N'/images/products/guerlain-lheure-bleue-edp.jpg',                               1),
    (22, 18, N'/images/products/guerlain-lheure-bleue-edp-box.jpg',                           0),
    (23, 19, N'/images/products/chanel-coco-mademoiselle-crush-absolu-edp.jpg',               1),
    (24, 19, N'/images/products/chanel-coco-mademoiselle-crush-absolu-edp-box.jpg',           0),
    (25, 20, N'/images/products/creed-wild-vetiver-edp.jpg',                                  1),
    (26, 20, N'/images/products/creed-wild-vetiver-edp-box.jpg',                              0),
    (27, 21, N'/images/products/creed-original-vetiver-edp.jpg',                              1),
    (28, 21, N'/images/products/creed-original-vetiver-edp-box.jpg',                          0);
SET IDENTITY_INSERT dbo.ProductImages OFF;
GO
USE PerfumeShopDB;
GO
--Update lại hình ảnh product 
UPDATE dbo.ProductImages
SET ImageURL = CASE ProductID
    WHEN 1 THEN N'/Content/images/Parour Cigar Silver Essence For Men EDT 75ml.webp'
    WHEN 2 THEN N'/Content/images/Parour Vanille Tonka For Women EDP 90ml.jpg'
    WHEN 3 THEN N'/Content/images/Calvin Klein Eternity Aromatic Essence For Women EDP 30ml.webp'
    WHEN 4 THEN N'/Content/images/Calvin Klein My Euphoria EDP 30ml.webp'
    WHEN 5 THEN N'/Content/images/Ralph Lauren Romance Intense EDP 30ml.webp'
    WHEN 6 THEN N'/Content/images/Issey Miyake L''Eau d''Issey Pour Homme Solar Lavender EDTI 50ml.webp'  
    WHEN 7 THEN N'/Content/images/Hugo Boss Alive EDP 30ml.webp'
    WHEN 8 THEN N'/Content/images/Boss The Scent EDP For Her 50ml .webp'
    WHEN 9 THEN N'/Content/images/Boss The Scent Magnetic EDP For Women 50ml.webp'
    WHEN 10 THEN N'/Content/images/Boss The Scent Parfum For Her 30ml.webp'
    WHEN 11 THEN N'/Content/images/Giorgio Armani My Way Parfum EDP 30ml .jpg'
    WHEN 12 THEN N'/Content/images/Lolita Lempicka EDP 50ml.webp'
    WHEN 13 THEN N'/Content/images/Mugler Alien EDP 30ml.webp'
    WHEN 14 THEN N'/Content/images/nuoc-hoa-nuMugler Alien Goddess EDP 30ml.webp'  
    WHEN 15 THEN N'/Content/images/Valentino Donna Born In Roma Purple Melancholia EDP 30ml.webp'
    WHEN 16 THEN N'/Content/images/Valentino Uomo Born In Roma Purple Melancholia EDT 100ml.webp'
    WHEN 17 THEN N'/Content/images/Gucci Bloom Ambrosia d''Oro EDP 60ml.webp'
    WHEN 18 THEN N'/Content/images/Guerlain L''Heure Bleue EDP 30ml.jpg'  
    WHEN 19 THEN N'/Content/images/Chanel Coco Mademoiselle Crush Absolu EDP 60ml.webp'
    WHEN 20 THEN N'/Content/images/Creed Wild Vetiver EDP 100ml.webp'
    WHEN 21 THEN N'/Content/images/Creed Original Vetiver EDP 100ml.webp'
    -- Thêm tiếp các sản phẩm khác vào đây tùy ý
    ELSE ImageURL -- Giữ nguyên các ID khác nếu không đổi
END
WHERE ProductID IN (1, 2, 3, 4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21);
USE PerfumeShopDB;
UPDATE dbo.ProductImages 
SET ImageURL = '/Content/images/Guerlain L''Heure Bleue EDP 30ml.jpg' 
WHERE ProductID = 18;

UPDATE dbo.ProductImages 
SET ImageURL = N'/Content/images/Calvin Klein Eternity Aromatic Essence For Women EDP 30ml.webp' 
WHERE ProductID = 18;

UPDATE dbo.ProductImages 
SET ImageURL = N'/Content/images/gucci-bloom-ambrosia-doro-60ml.webp' 
WHERE ProductID = 17;

UPDATE dbo.ProductImages 
SET ImageURL = N'/Content/images/issey-miyake-solar-lavender-50ml.webp' 
WHERE ProductID = 6;

UPDATE dbo.ProductImages 
SET ImageURL = '/Content/images/calvin-klein-eternit    y-aromatic-essence-30ml.webp' 
WHERE ProductID = 3; -- (Sửa lại đúng ProductID của chai này)

UPDATE dbo.ProductImages 
SET ImageURL = '/Content/images/guerlain-l-heure-bleue-edp-30ml.jpg' 
WHERE ProductID = 18; -- (Sửa lại đúng ProductID của chai này)



-- ===== BẢNG 8/13: Coupons =====
-- Mã giảm giá. IsPercent = 1: DiscountValue là %, = 0: là số tiền.
-- HELLO, HAPPYBIRTHDAY lấy từ [HARRY] (HELLO ngoài site giảm tối đa 100.000đ, Backend tự giới hạn).
-- GIAM50K là mã demo; TET2026 đã hết hạn, dùng để test Backend chặn mã hết hạn.
SET IDENTITY_INSERT dbo.Coupons ON;
INSERT INTO dbo.Coupons (CouponID, Code, DiscountValue, IsPercent, MinOrderAmount, ExpiryDate, IsActive, UsageLimit, UsedCount)
VALUES
    (1, N'HELLO',             5, 1,       0, N'2026-12-31T23:59:59', 1, NULL, 1),
    (2, N'HAPPYBIRTHDAY',     5, 1,       0, N'2026-12-31T23:59:59', 1, NULL, 1),
    (3, N'GIAM50K',       50000, 0, 1000000, N'2026-11-30T23:59:59', 1,  100, 1),
    (4, N'TET2026',          10, 1,  500000, N'2026-02-28T23:59:59', 0,   50, 0);
SET IDENTITY_INSERT dbo.Coupons OFF;
GO

-- ===== BẢNG 9/13: Carts =====
-- Giỏ hàng đang có; mỗi user tối đa 1 giỏ (giỏ 1 của user 3, giỏ 2 của user 5).
SET IDENTITY_INSERT dbo.Carts ON;
INSERT INTO dbo.Carts (CartID, UserID, CreatedAt)
VALUES
    (1, 3, N'2026-10-05T20:10:00'),
    (2, 5, N'2026-10-08T09:30:00');
SET IDENTITY_INSERT dbo.Carts OFF;
GO

-- ===== BẢNG 10/13: CartItems =====
-- Sản phẩm trong giỏ, tính theo VariantID. Giá luôn lấy từ ProductVariants.
SET IDENTITY_INSERT dbo.CartItems ON;
INSERT INTO dbo.CartItems (CartItemID, CartID, VariantID, Quantity)
VALUES
    (1, 1,  4, 1),
    (2, 1, 17, 1),
    (3, 2, 26, 1);
SET IDENTITY_INSERT dbo.CartItems OFF;
GO

-- ===== BẢNG 11/13: Orders =====
-- 7 đơn hàng đủ các trạng thái (Delivered, Shipping, Processing, Pending, Cancelled).
-- TotalAmount = SubTotal - DiscountAmount + ShippingFee (Backend tính). Miễn phí vận chuyển nên ShippingFee = 0.
-- Thông tin người nhận được sao chép lại lúc đặt hàng.
SET IDENTITY_INSERT dbo.Orders ON;
INSERT INTO dbo.Orders (OrderID, OrderCode, UserID, CouponID, OrderDate, OrderStatus, ReceiverName, ReceiverPhone, ShippingAddress, SubTotal, DiscountAmount, ShippingFee, TotalAmount, PaymentMethod, PaymentStatus)
VALUES
    (1, N'DH20260915001', 3, NULL, N'2026-09-15T10:30:00', N'Delivered',
        N'Lê Minh Anh', N'0901000003', N'25 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh',
        2580000, 0, 0, 2580000, N'COD', N'Paid'),
    (2, N'DH20260918002', 4, 1, N'2026-09-18T14:05:00', N'Delivered',
        N'Phạm Quốc Bảo', N'0901000004', N'88 Nguyễn Trãi, Quận 5, TP. Hồ Chí Minh',
        3250000, 100000, 0, 3150000, N'BankTransfer', N'Paid'),
    (3, N'DH20260922003', 5, NULL, N'2026-09-22T19:45:00', N'Shipping',
        N'Võ Thu Hà', N'0901000005', N'15 Trần Phú, Quận Hải Châu, Đà Nẵng',
        7500000, 0, 0, 7500000, N'BankTransfer', N'Paid'),
    (4, N'DH20260927004', 6, 3, N'2026-09-27T08:20:00', N'Processing',
        N'Đặng Hoàng Long', N'0901000006', N'102 Cầu Giấy, Quận Cầu Giấy, Hà Nội',
        4570000, 50000, 0, 4520000, N'COD', N'Unpaid'),
    (5, N'DH20261002005', 3, 2, N'2026-10-02T21:15:00', N'Pending',
        N'Lê Minh Anh', N'0901000003', N'25 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh',
        2750000, 137500, 0, 2612500, N'MoMo', N'Unpaid'),
    (6, N'DH20260905006', 4, NULL, N'2026-09-05T16:40:00', N'Cancelled',
        N'Phạm Quốc Bảo', N'0901000004', N'88 Nguyễn Trãi, Quận 5, TP. Hồ Chí Minh',
        2980000, 0, 0, 2980000, N'COD', N'Unpaid'),
    (7, N'DH20260901007', 5, NULL, N'2026-09-01T11:00:00', N'Delivered',
        N'Võ Thu Hà', N'0901000005', N'15 Trần Phú, Quận Hải Châu, Đà Nẵng',
        2880000, 0, 0, 2880000, N'BankTransfer', N'Paid');
SET IDENTITY_INSERT dbo.Orders OFF;
GO

-- ===== BẢNG 12/13: OrderItems =====
-- Chi tiết từng đơn. Tên, SKU, dung tích, đơn giá được sao chép lúc mua
-- nên đổi giá hoặc tên sản phẩm sau này không làm sai đơn cũ.
SET IDENTITY_INSERT dbo.OrderItems ON;
INSERT INTO dbo.OrderItems (OrderItemID, OrderID, VariantID, ProductNameSnapshot, SKU, VolumeML, Quantity, UnitPrice)
VALUES
    ( 1, 1,  5, N'Calvin Klein My Euphoria EDP',                                N'CK-MYE-30',        30, 1, 1290000),
    ( 2, 1,  3, N'Calvin Klein Eternity Aromatic Essence For Women EDP',        N'CK-EAE-30',        30, 1, 1290000),
    ( 3, 2, 20, N'Valentino Donna Born In Roma Purple Melancholia EDP',         N'VAL-DBIR-PM-100', 100, 1, 3250000),
    ( 4, 3, 26, N'Creed Wild Vetiver EDP',                                      N'CRE-WV-100',      100, 1, 7500000),
    ( 5, 4, 17, N'Mugler Alien EDP',                                            N'MUG-ALIEN-60',     60, 2, 1590000),
    ( 6, 4, 11, N'Boss The Scent EDP For Her',                                  N'HB-TS-HER-30',     30, 1, 1390000),
    ( 7, 5, 21, N'Valentino Uomo Born In Roma Purple Melancholia EDT',          N'VAL-UBIR-PM-100', 100, 1, 2750000),
    ( 8, 6, 14, N'Boss The Scent Parfum For Her',                               N'HB-TSP-HER-30',    30, 1, 1590000),
    ( 9, 6, 10, N'Hugo Boss Alive EDP',                                         N'HB-ALV-30',        30, 1, 1390000),
    (10, 7,  9, N'Issey Miyake L''Eau d''Issey Pour Homme Solar Lavender EDTI', N'IM-LEPH-SL-50',    50, 1, 1390000),
    (11, 7, 15, N'Giorgio Armani My Way Parfum EDP',                            N'GA-MYWAY-30',      30, 1, 1490000);
SET IDENTITY_INSERT dbo.OrderItems OFF;
GO

-- ===== BẢNG 13/13: ProductReviews =====
-- Đánh giá 1-5 sao. OrderID có giá trị = đã mua hàng, NULL = chưa xác minh.
-- IsApproved = 0 là chờ admin duyệt (review số 6). Nội dung đánh giá là dữ liệu demo.
SET IDENTITY_INSERT dbo.ProductReviews ON;
INSERT INTO dbo.ProductReviews (ReviewID, ProductID, UserID, OrderID, Rating, Comment, IsApproved, CreatedAt)
VALUES
    (1,  4, 3,    1, 5, N'Mùi thơm dễ chịu, giao hàng nhanh, chai nguyên seal.', 1, N'2026-09-20T09:00:00'),
    (2,  3, 3,    1, 4, N'Hương nhẹ nhàng, hợp đi làm hằng ngày.',               1, N'2026-09-20T09:10:00'),
    (3, 15, 4,    2, 5, N'Chai đẹp, mùi sang, lưu hương khá lâu.',               1, N'2026-09-25T18:30:00'),
    (4,  6, 5,    7, 4, N'Mùi tươi mát, hợp thời tiết nóng.',                    1, N'2026-09-08T20:00:00'),
    (5, 11, 5,    7, 5, N'Rất ưng, sẽ mua thêm bản dung tích lớn.',              1, N'2026-09-08T20:05:00'),
    (6, 13, 6, NULL, 3, N'Mùi khá đặc trưng, hơi nồng với mình.',                0, N'2026-10-03T10:00:00'),
    (7, 17, 4, NULL, 4, N'Hương hoa nhẹ nhàng, đóng gói cẩn thận.',              1, N'2026-10-04T15:20:00'),
    (8,  8, 3, NULL, 5, N'Giá tốt so với chất lượng.',                           1, N'2026-10-06T21:00:00');
SET IDENTITY_INSERT dbo.ProductReviews OFF;
GO
--Update lại hình ảnh product 



-- ===== KIỂM TRA: số dòng mong đợi Roles 3, Users 7, Brands 13, Categories 4, Products 21, ProductVariants 27,
--                ProductImages 28, Coupons 4, Carts 2, CartItems 3, Orders 7, OrderItems 11, ProductReviews 8 =====
SELECT 'Roles' AS TableName, COUNT(*) AS Total FROM dbo.Roles
UNION ALL SELECT 'Users', COUNT(*) FROM dbo.Users
UNION ALL SELECT 'Brands', COUNT(*) FROM dbo.Brands
UNION ALL SELECT 'Categories', COUNT(*) FROM dbo.Categories
UNION ALL SELECT 'Products', COUNT(*) FROM dbo.Products
UNION ALL SELECT 'ProductVariants', COUNT(*) FROM dbo.ProductVariants
UNION ALL SELECT 'ProductImages', COUNT(*) FROM dbo.ProductImages
UNION ALL SELECT 'Coupons', COUNT(*) FROM dbo.Coupons
UNION ALL SELECT 'Carts', COUNT(*) FROM dbo.Carts
UNION ALL SELECT 'CartItems', COUNT(*) FROM dbo.CartItems
UNION ALL SELECT 'Orders', COUNT(*) FROM dbo.Orders
UNION ALL SELECT 'OrderItems', COUNT(*) FROM dbo.OrderItems
UNION ALL SELECT 'ProductReviews', COUNT(*) FROM dbo.ProductReviews;
GO
SELECT TOP 5 * FROM dbo.ProductImages;
USE PerfumeShopDB;
go
SELECT * FROM dbo.ProductImages;

