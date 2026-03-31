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
-- POI_CATEGORIES (LIÊN K?T)
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
-- VISIT LOG (QR)
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
VALUES ('admin','123',N'Qu?n tr?','admin@gmail.com',1);

-- =========================
-- DANH M?C
-- =========================
INSERT INTO Categories VALUES
(N'Quán ?c'),
(N'Quán n??ng'),
(N'Quán l?u');

-- =========================
-- QUÁN ?N (CÓ HÌNH)
-- =========================
INSERT INTO POIs (Name, Latitude, Longitude, Address, Description, Thumbnail, Status)
VALUES
(N'?c Oanh',10.757,106.704,N'534 V?nh Khánh, Q4',N'?c n?i ti?ng ?ông khách',
'https://images.unsplash.com/photo-1559847844-5315695dadae',N'Open'),

(N'?c ?ào',10.756,106.703,N'212 V?nh Khánh, Q4',N'S?t tr?ng mu?i ngon',
'https://images.unsplash.com/photo-1562967916-eb82221dfb92',N'Open'),

(N'?c V?',10.757,106.705,N'376 V?nh Khánh, Q4',N'M? khuya',
'https://images.unsplash.com/photo-1604908176997-125f25cc6f3d',N'Open'),

(N'?c Th?o',10.756,106.704,N'220 V?nh Khánh, Q4',N'H?i s?n ?a d?ng',
'https://images.unsplash.com/photo-1617196034738-26c5c1d3c2c2',N'Open'),

(N'Chilli Quán',10.755,106.703,N'158 V?nh Khánh, Q4',N'L?u n??ng giá r?',
'https://images.unsplash.com/photo-1555992336-03a23c7b20ee',N'Open'),

(N'Th? Gi?i Bò',10.756,106.704,N'245 V?nh Khánh, Q4',N'Bò n??ng ngon',
'https://images.unsplash.com/photo-1558030006-450675393462',N'Open'),

(N'?t Xiêm Quán',10.755,106.702,N'120 V?nh Khánh, Q4',N'Món cay h?p d?n',
'https://images.unsplash.com/photo-1600891964599-f61ba0e24092',N'Open'),

(N'L?u Cá Kèo',10.755,106.703,N'V?nh Khánh, Q4',N'L?u cá kèo ??c s?n',
'https://images.unsplash.com/photo-1604908554007-7f3d1cf7b3c4',N'Open');

-- =========================
-- GÁN DANH M?C
-- =========================
-- ?c
INSERT INTO POI_Categories VALUES (1,1),(2,1),(3,1),(4,1);

-- N??ng
INSERT INTO POI_Categories VALUES (5,2),(6,2),(7,2);

-- L?u
INSERT INTO POI_Categories VALUES (8,3);

-- =========================
-- MENU
-- =========================
INSERT INTO Menu (POIID, FoodName, Price, Image)
VALUES
(1,N'?c h??ng xào b?',80000,'https://images.unsplash.com/photo-1559847844-5315695dadae'),
(1,N'Sò ?i?p n??ng phô mai',70000,'https://images.unsplash.com/photo-1562967916-eb82221dfb92'),
(2,N'?c len xào d?a',60000,'https://images.unsplash.com/photo-1604908176997-125f25cc6f3d'),
(5,N'Ba ch? n??ng',90000,'https://images.unsplash.com/photo-1555992336-03a23c7b20ee'),
(5,N'L?u thái',120000,'https://images.unsplash.com/photo-1600891964599-f61ba0e24092'),
(6,N'Bò n??ng t?ng',150000,'https://images.unsplash.com/photo-1558030006-450675393462');

-- =========================
-- QR CHECK-IN
-- =========================
INSERT INTO VisitLog (POIID, DeviceID)
VALUES 
(1,'device001'),
(2,'device002'),
(5,'device003');