-- =========================
-- CREATE DATABASE
-- =========================
CREATE DATABASE FoodPOI;
GO
USE FoodPOI;
GO

-- =========================
-- ROLES + ADMIN
-- =========================
CREATE TABLE Roles (
    RoleID INT IDENTITY PRIMARY KEY,
    RoleName NVARCHAR(50)
);

CREATE TABLE AdminUsers (
    UserID INT IDENTITY PRIMARY KEY,
    Username NVARCHAR(100),
    Password VARCHAR(255),
    FullName NVARCHAR(100),
    Email VARCHAR(100),
    RoleID INT,
    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
);

-- =========================
-- CATEGORIES
-- =========================
CREATE TABLE Categories (
    CategoryID INT IDENTITY PRIMARY KEY,
    CategoryName NVARCHAR(100)
);

-- =========================
-- POIs (QUÁN)
-- =========================
CREATE TABLE POIs (
    POIID INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(150),
    Latitude FLOAT,
    Longitude FLOAT,
    Address NVARCHAR(255),
    Description NVARCHAR(MAX),
    Thumbnail VARCHAR(500),
    Status NVARCHAR(50),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- =========================
-- POI_CATEGORIES
-- =========================
CREATE TABLE POI_Categories (
    POIID INT,
    CategoryID INT,
    PRIMARY KEY (POIID, CategoryID),
    FOREIGN KEY (POIID) REFERENCES POIs(POIID),
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);

-- =========================
-- MENU
-- =========================
CREATE TABLE Menu (
    MenuID INT IDENTITY PRIMARY KEY,
    POIID INT,
    FoodName NVARCHAR(150),
    Price FLOAT,
    Image VARCHAR(500),
    FOREIGN KEY (POIID) REFERENCES POIs(POIID)
);

-- =========================
-- VISIT LOG
-- =========================
CREATE TABLE VisitLog (
    VisitID INT IDENTITY PRIMARY KEY,
    POIID INT,
    DeviceID VARCHAR(100),
    VisitTime DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (POIID) REFERENCES POIs(POIID)
);

-- =========================
-- AUDIT LOG
-- =========================
CREATE TABLE AuditLogs (
    LogID INT IDENTITY PRIMARY KEY,
    UserID INT,
    Action NVARCHAR(255),
    Timestamp DATETIME DEFAULT GETDATE()
);

-- =========================
-- INSERT DATA
-- =========================

-- Roles + Admin
INSERT INTO Roles VALUES (N'Admin'), (N'Staff');

INSERT INTO AdminUsers (Username, Password, FullName, Email, RoleID)
VALUES ('admin','123',N'Quản trị','admin@gmail.com',1);

-- =========================
-- DANH MỤC
-- =========================
INSERT INTO Categories VALUES
(N'Quán ốc'),
(N'Quán nướng'),
(N'Quán lẩu');

-- =========================
-- QUÁN ĂN (ẢNH GOOGLE MAP)
-- =========================
INSERT INTO POIs (Name, Latitude, Longitude, Address, Description, Thumbnail, Status)
VALUES
(N'Ốc Oanh',10.757,106.704,N'534 Vĩnh Khánh, Q4',N'Ốc nổi tiếng đông khách',
'https://lh3.googleusercontent.com/p/AF1QipOcOanh',N'Open'),

(N'Ốc Đào',10.756,106.703,N'212 Vĩnh Khánh, Q4',N'Sốt trứng muối ngon',
'https://lh3.googleusercontent.com/p/AF1QipOcDao',N'Open'),

(N'Ốc Vũ',10.757,106.705,N'376 Vĩnh Khánh, Q4',N'Mở khuya',
'https://lh3.googleusercontent.com/p/AF1QipOcVu',N'Open'),

(N'Ốc Thảo',10.756,106.704,N'220 Vĩnh Khánh, Q4',N'Hải sản đa dạng',
'https://lh3.googleusercontent.com/p/AF1QipOcThao',N'Open'),

(N'Chilli Quán',10.755,106.703,N'158 Vĩnh Khánh, Q4',N'Lẩu nướng giá rẻ',
'https://lh3.googleusercontent.com/p/AF1QipChilli',N'Open'),

(N'Thế Giới Bò',10.756,106.704,N'245 Vĩnh Khánh, Q4',N'Bò nướng ngon',
'https://lh3.googleusercontent.com/p/AF1QipTheGioiBo',N'Open'),

(N'Ớt Xiêm Quán',10.755,106.702,N'120 Vĩnh Khánh, Q4',N'Món cay hấp dẫn',
'https://lh3.googleusercontent.com/p/AF1QipOtXiem',N'Open'),

(N'Lẩu Cá Kèo',10.755,106.703,N'Vĩnh Khánh, Q4',N'Lẩu cá kèo đặc sản',
'https://lh3.googleusercontent.com/p/AF1QipLauCaKeo',N'Open');

-- =========================
-- GÁN DANH MỤC
-- =========================
INSERT INTO POI_Categories VALUES (1,1),(2,1),(3,1),(4,1);
INSERT INTO POI_Categories VALUES (5,2),(6,2),(7,2);
INSERT INTO POI_Categories VALUES (8,3);

-- =========================
-- MENU
-- =========================
INSERT INTO Menu (POIID, FoodName, Price, Image)
VALUES
(1,N'Ốc hương xào bơ',80000,'https://lh3.googleusercontent.com/p/AF1QipMon1'),
(1,N'Sò điệp nướng phô mai',70000,'https://lh3.googleusercontent.com/p/AF1QipMon2'),
(2,N'Ốc len xào dừa',60000,'https://lh3.googleusercontent.com/p/AF1QipMon3'),
(5,N'Ba chỉ nướng',90000,'https://lh3.googleusercontent.com/p/AF1QipMon4'),
(5,N'Lẩu thái',120000,'https://lh3.googleusercontent.com/p/AF1QipMon5'),
(6,N'Bò nướng tảng',150000,'https://lh3.googleusercontent.com/p/AF1QipMon6');

-- =========================
-- QR CHECK-IN
-- =========================
INSERT INTO VisitLog (POIID, DeviceID)
VALUES 
(1,'device001'),
(2,'device002'),
(5,'device003');