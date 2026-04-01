CREATE DATABASE TourismCMS;
GO

USE TourismCMS;
GO

CREATE TABLE Users (
    Id INT IDENTITY PRIMARY KEY,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100),
    Phone NVARCHAR(20),

    Role NVARCHAR(20) NOT NULL, 
    -- admin / owner / user

    IsVerified BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,

    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE RefreshTokens (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT,
    Token NVARCHAR(500),
    ExpiryDate DATETIME,

    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE OwnerRegistrations (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT,

    PoiName NVARCHAR(255),
    CCCD NVARCHAR(255), -- mã hóa

    Status NVARCHAR(20) DEFAULT 'pending',
    -- pending / approved / rejected

    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE POIs (
    Id INT IDENTITY PRIMARY KEY,
    OwnerId INT,

    Name NVARCHAR(255),
    Description NVARCHAR(MAX),
    Address NVARCHAR(255),

    Latitude FLOAT,
    Longitude FLOAT,

    Status NVARCHAR(20) DEFAULT 'pending',
    -- pending / approved / rejected

    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (OwnerId) REFERENCES Users(Id)
);

CREATE TABLE MenuItems (
    Id INT IDENTITY PRIMARY KEY,
    PoiId INT,

    Name NVARCHAR(255),
    Description NVARCHAR(255),
    Price DECIMAL(18,2),

    ImageUrl NVARCHAR(500),
    IsAvailable BIT DEFAULT 1,

    FOREIGN KEY (PoiId) REFERENCES POIs(Id)
);

CREATE TABLE Submissions (
    Id INT IDENTITY PRIMARY KEY,
    OwnerId INT,
    PoiId INT,

    Type NVARCHAR(50),
    -- create / update / delete

    Data NVARCHAR(MAX), -- JSON

    Status NVARCHAR(20) DEFAULT 'pending',

    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (OwnerId) REFERENCES Users(Id),
    FOREIGN KEY (PoiId) REFERENCES POIs(Id)
);

CREATE TABLE Reviews (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT,
    PoiId INT,

    Rating INT CHECK (Rating BETWEEN 1 AND 5),
    Comment NVARCHAR(MAX),

    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (PoiId) REFERENCES POIs(Id)
);

CREATE TABLE Favorites (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT,
    PoiId INT,

    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (PoiId) REFERENCES POIs(Id)
);

CREATE TABLE AI_Usage_Limits (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT,

    DailyLimit INT DEFAULT 10,
    UsedToday INT DEFAULT 0,

    LastReset DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE AuditLogs (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT,

    Action NVARCHAR(255),
    Entity NVARCHAR(100),
    EntityId INT,

    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

INSERT INTO Users (Email, PasswordHash, Role, IsVerified)
VALUES ('admin@gmail.com', '123', 'admin', 1);