CREATE DATABASE TourismDB;
USE TourismDB;

CREATE TABLE POI
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200),
    Description NVARCHAR(MAX),
    Latitude FLOAT,
    Longitude FLOAT,
    Radius INT,
    ImagePath NVARCHAR(255),
    AudioPath NVARCHAR(255)
);