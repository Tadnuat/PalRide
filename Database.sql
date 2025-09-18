-- ============================================
-- PalRide (SQL Server 2019 version)
-- ============================================

DROP DATABASE IF EXISTS PalRide;
GO
CREATE DATABASE PalRide;
GO
USE PalRide;
GO

-- -----------------------
-- Table: Admin
-- -----------------------
DROP TABLE IF EXISTS Admin;
CREATE TABLE Admin (
  AdminId INT IDENTITY(1,1) PRIMARY KEY,
  FullName NVARCHAR(100) NOT NULL,
  Email NVARCHAR(100) NOT NULL UNIQUE,
  PasswordHash NVARCHAR(255) NOT NULL,
  PhoneNumber NVARCHAR(15),
  Role NVARCHAR(20) NOT NULL CONSTRAINT CK_Admin_Role CHECK (Role IN ('SuperAdmin','Moderator')),
  IsActive BIT NOT NULL DEFAULT 1,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  LastLogin DATETIME2 NULL
);
GO

INSERT INTO Admin (FullName, Email, PasswordHash, Role)
VALUES ('Super Admin', 'admin@palride.local', 'PLACEHOLDER_HASH', 'SuperAdmin');
GO


-- -----------------------
-- Table: [User]
-- -----------------------
DROP TABLE IF EXISTS [User];
CREATE TABLE [User] (
  UserId INT IDENTITY(1,1) PRIMARY KEY,
  FullName NVARCHAR(100) NOT NULL,
  Email NVARCHAR(100) NOT NULL UNIQUE,
  PhoneNumber NVARCHAR(15) NOT NULL UNIQUE,
  PasswordHash NVARCHAR(255) NOT NULL,
  Role NVARCHAR(20) NOT NULL DEFAULT 'Passenger'
    CONSTRAINT CK_User_Role CHECK (Role IN ('Passenger','Driver','Both')),
  Gender NVARCHAR(10) NULL CONSTRAINT CK_User_Gender CHECK (Gender IN ('Male','Female','Other')),
  DateOfBirth DATE NULL,
  StudentId NVARCHAR(50) NULL,
  University NVARCHAR(150) NULL,
  GmailVerified BIT NOT NULL DEFAULT 0,
  NationalId NVARCHAR(20) NULL,
  NationalIdVerified BIT NOT NULL DEFAULT 0,
  PhoneVerified BIT NOT NULL DEFAULT 0,
  RatingAverage DECIMAL(3,2) NOT NULL DEFAULT 0.00,
  IsActive BIT NOT NULL DEFAULT 1,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  UpdatedAt DATETIME2 NULL
);
GO


-- -----------------------
-- Table: Vehicle
-- -----------------------
DROP TABLE IF EXISTS Vehicle;
CREATE TABLE Vehicle (
  VehicleId INT IDENTITY(1,1) PRIMARY KEY,
  UserId INT NOT NULL,
  Type NVARCHAR(20) NOT NULL CONSTRAINT CK_Vehicle_Type CHECK (Type IN ('Car','Motorbike','ElectricBike')),
  Brand NVARCHAR(50) NULL,
  Model NVARCHAR(50) NULL,
  Color NVARCHAR(30) NULL,
  Year SMALLINT NULL,
  SeatCount TINYINT NOT NULL DEFAULT 1,
  LicensePlate NVARCHAR(20) UNIQUE,
  Verified BIT NOT NULL DEFAULT 0,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  CONSTRAINT FK_Vehicle_User FOREIGN KEY (UserId) REFERENCES [User](UserId)
);
GO


-- -----------------------
-- Table: Route
-- -----------------------
DROP TABLE IF EXISTS Route;
CREATE TABLE Route (
  RouteId INT IDENTITY(1,1) PRIMARY KEY,
  UserId INT NOT NULL,
  PickupLocation NVARCHAR(255) NOT NULL,
  DropoffLocation NVARCHAR(255) NOT NULL,
  IsRoundTrip BIT NOT NULL DEFAULT 0,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  CONSTRAINT FK_Route_User FOREIGN KEY (UserId) REFERENCES [User](UserId)
);
GO


-- -----------------------
-- Table: Trip
-- -----------------------
DROP TABLE IF EXISTS Trip;
CREATE TABLE Trip (
  TripId INT IDENTITY(1,1) PRIMARY KEY,
  DriverId INT NOT NULL,
  VehicleId INT NULL,
  PickupLocation NVARCHAR(255) NOT NULL,
  DropoffLocation NVARCHAR(255) NOT NULL,
  StartTime DATETIME2 NOT NULL,
  EndTime DATETIME2 NULL,
  SeatTotal TINYINT NOT NULL DEFAULT 1,
  SeatAvailable TINYINT NOT NULL DEFAULT 1,
  PricePerSeat DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  PriceFullRide DECIMAL(12,2) NULL,
  Note NVARCHAR(500) NULL,
  Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'
    CONSTRAINT CK_Trip_Status CHECK (Status IN ('Pending','Active','Completed','Cancelled')),
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  UpdatedAt DATETIME2 NULL,
  CONSTRAINT FK_Trip_User FOREIGN KEY (DriverId) REFERENCES [User](UserId),
  CONSTRAINT FK_Trip_Vehicle FOREIGN KEY (VehicleId) REFERENCES Vehicle(VehicleId)
);
GO


-- -----------------------
-- Table: Booking
-- -----------------------
DROP TABLE IF EXISTS Booking;
CREATE TABLE Booking (
  BookingId INT IDENTITY(1,1) PRIMARY KEY,
  TripId INT NOT NULL,
  PassengerId INT NOT NULL,
  SeatCount TINYINT NOT NULL DEFAULT 1,
  TotalPrice DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'
    CONSTRAINT CK_Booking_Status CHECK (Status IN ('Pending','Accepted','Cancelled','Completed')),
  BookingTime DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  UpdatedAt DATETIME2 NULL,
  CONSTRAINT FK_Booking_Trip FOREIGN KEY (TripId) REFERENCES Trip(TripId),
  CONSTRAINT FK_Booking_User FOREIGN KEY (PassengerId) REFERENCES [User](UserId)
);
GO


-- -----------------------
-- Table: Wallet
-- -----------------------
DROP TABLE IF EXISTS Wallet;
CREATE TABLE Wallet (
  WalletId INT IDENTITY(1,1) PRIMARY KEY,
  UserId INT NOT NULL UNIQUE,
  Balance DECIMAL(14,2) NOT NULL DEFAULT 0.00,
  UpdatedAt DATETIME2 NULL,
  CONSTRAINT FK_Wallet_User FOREIGN KEY (UserId) REFERENCES [User](UserId)
);
GO


-- -----------------------
-- Table: Transaction
-- -----------------------
DROP TABLE IF EXISTS [Transaction];
CREATE TABLE [Transaction] (
  TransactionId INT IDENTITY(1,1) PRIMARY KEY,
  WalletId INT NOT NULL,
  Amount DECIMAL(14,2) NOT NULL,
  Type NVARCHAR(20) NOT NULL
    CONSTRAINT CK_Transaction_Type CHECK (Type IN ('Deposit','Withdraw','Payment','Refund')),
  ReferenceType NVARCHAR(20) DEFAULT 'Other'
    CONSTRAINT CK_Transaction_RefType CHECK (ReferenceType IN ('Booking','Trip','Manual','Other')),
  ReferenceId INT NULL,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  ApprovedBy INT NULL,
  CONSTRAINT FK_Transaction_Wallet FOREIGN KEY (WalletId) REFERENCES Wallet(WalletId),
  CONSTRAINT FK_Transaction_Admin FOREIGN KEY (ApprovedBy) REFERENCES Admin(AdminId)
);
GO


-- -----------------------
-- Table: Voucher
-- -----------------------
DROP TABLE IF EXISTS Voucher;
CREATE TABLE Voucher (
  VoucherId INT IDENTITY(1,1) PRIMARY KEY,
  Code NVARCHAR(50) NOT NULL UNIQUE,
  Description NVARCHAR(255) NULL,
  DiscountType NVARCHAR(20) NOT NULL
    CONSTRAINT CK_Voucher_Type CHECK (DiscountType IN ('Percent','Fixed')),
  DiscountValue DECIMAL(12,2) NOT NULL,
  MinOrderValue DECIMAL(12,2) DEFAULT 0.00,
  ExpiryDate DATE NULL,
  UsageLimit INT NULL,
  CreatedBy INT NULL,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  CONSTRAINT FK_Voucher_Admin FOREIGN KEY (CreatedBy) REFERENCES Admin(AdminId)
);
GO


-- -----------------------
-- Table: UserVoucher
-- -----------------------
DROP TABLE IF EXISTS UserVoucher;
CREATE TABLE UserVoucher (
  UserVoucherId INT IDENTITY(1,1) PRIMARY KEY,
  UserId INT NOT NULL,
  VoucherId INT NOT NULL,
  IsUsed BIT NOT NULL DEFAULT 0,
  UsedAt DATETIME2 NULL,
  GrantedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  CONSTRAINT FK_UserVoucher_User FOREIGN KEY (UserId) REFERENCES [User](UserId),
  CONSTRAINT FK_UserVoucher_Voucher FOREIGN KEY (VoucherId) REFERENCES Voucher(VoucherId)
);
GO


-- -----------------------
-- Table: Review
-- -----------------------
DROP TABLE IF EXISTS Review;
CREATE TABLE Review (
  ReviewId INT IDENTITY(1,1) PRIMARY KEY,
  TripId INT NULL,
  FromUserId INT NOT NULL,
  ToUserId INT NOT NULL,
  Rating TINYINT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
  Comment NVARCHAR(1000) NULL,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  CONSTRAINT FK_Review_Trip FOREIGN KEY (TripId) REFERENCES Trip(TripId),
  CONSTRAINT FK_Review_From FOREIGN KEY (FromUserId) REFERENCES [User](UserId),
  CONSTRAINT FK_Review_To FOREIGN KEY (ToUserId) REFERENCES [User](UserId)
);
GO


-- -----------------------
-- Table: Notification
-- -----------------------
DROP TABLE IF EXISTS Notification;
CREATE TABLE Notification (
  NotificationId INT IDENTITY(1,1) PRIMARY KEY,
  UserId INT NULL,
  Title NVARCHAR(150),
  Message NVARCHAR(1000),
  Type NVARCHAR(20) DEFAULT 'System'
    CONSTRAINT CK_Notification_Type CHECK (Type IN ('Trip','Booking','System','Admin')),
  IsRead BIT NOT NULL DEFAULT 0,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  CreatedBy INT NULL,
  CONSTRAINT FK_Notification_User FOREIGN KEY (UserId) REFERENCES [User](UserId),
  CONSTRAINT FK_Notification_Admin FOREIGN KEY (CreatedBy) REFERENCES Admin(AdminId)
);
GO


-- -----------------------
-- Table: Message
-- -----------------------
DROP TABLE IF EXISTS Message;
CREATE TABLE Message (
  MessageId INT IDENTITY(1,1) PRIMARY KEY,
  TripId INT NULL,
  FromUserId INT NOT NULL,
  ToUserId INT NOT NULL,
  MessageText NVARCHAR(2000) NOT NULL,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  IsRead BIT NOT NULL DEFAULT 0,
  CONSTRAINT FK_Message_Trip FOREIGN KEY (TripId) REFERENCES Trip(TripId),
  CONSTRAINT FK_Message_From FOREIGN KEY (FromUserId) REFERENCES [User](UserId),
  CONSTRAINT FK_Message_To FOREIGN KEY (ToUserId) REFERENCES [User](UserId)
);
GO


-- -----------------------
-- Table: Report
-- -----------------------
DROP TABLE IF EXISTS Report;
CREATE TABLE Report (
  ReportId INT IDENTITY(1,1) PRIMARY KEY,
  ReporterId INT NOT NULL,
  ReportedUserId INT NOT NULL,
  TripId INT NULL,
  Reason NVARCHAR(1000) NOT NULL,
  Details NVARCHAR(2000) NULL,
  Status NVARCHAR(20) NOT NULL DEFAULT 'Pending'
    CONSTRAINT CK_Report_Status CHECK (Status IN ('Pending','Reviewed','ActionTaken','Dismissed')),
  AdminId INT NULL,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  UpdatedAt DATETIME2 NULL,
  CONSTRAINT FK_Report_Reporter FOREIGN KEY (ReporterId) REFERENCES [User](UserId),
  CONSTRAINT FK_Report_Reported FOREIGN KEY (ReportedUserId) REFERENCES [User](UserId),
  CONSTRAINT FK_Report_Trip FOREIGN KEY (TripId) REFERENCES Trip(TripId),
  CONSTRAINT FK_Report_Admin FOREIGN KEY (AdminId) REFERENCES Admin(AdminId)
);
GO


-- -----------------------
-- Table: PasswordResetToken
-- -----------------------
DROP TABLE IF EXISTS PasswordResetToken;
CREATE TABLE PasswordResetToken (
  TokenId INT IDENTITY(1,1) PRIMARY KEY,
  UserId INT NOT NULL,
  Token NVARCHAR(255) NOT NULL UNIQUE,
  ExpiresAt DATETIME2 NOT NULL,
  Used BIT NOT NULL DEFAULT 0,
  CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
  CONSTRAINT FK_PasswordResetToken_User FOREIGN KEY (UserId) REFERENCES [User](UserId)
);
GO
==================================================================================================
USE PalRide;
GO

-- Admin
INSERT INTO Admin (FullName, Email, PasswordHash, Role, IsActive)
VALUES
(N'Super Admin', 'admin1@palride.local', 'HASHED_PASSWORD_1', 'SuperAdmin', 1),
(N'Mod John', 'admin2@palride.local', 'HASHED_PASSWORD_2', 'Moderator', 1);
GO

-- User
INSERT INTO [User] (FullName, Email, PhoneNumber, PasswordHash, Role, Gender, DateOfBirth, StudentId, University, GmailVerified, NationalId, NationalIdVerified, PhoneVerified, RatingAverage, IsActive)
VALUES
(N'Alice Passenger', 'alice@palride.local', '0901111111', 'HASHED_PASS_A', 'Passenger', 'Female', '2001-05-10', 'STU001', 'FPT University', 1, '0123456789', 1, 1, 4.8, 1),
(N'Bob Driver', 'bob@palride.local', '0902222222', 'HASHED_PASS_B', 'Driver', 'Male', '1998-03-15', 'STU002', 'FPT University', 1, '9876543210', 1, 1, 4.5, 1),
(N'Charlie Both', 'charlie@palride.local', '0903333333', 'HASHED_PASS_C', 'Both', 'Male', '2000-07-20', 'STU003', 'FPT University', 0, NULL, 0, 1, 4.2, 1);
GO

-- Vehicle
INSERT INTO Vehicle (UserId, Type, Brand, Model, Color, Year, SeatCount, LicensePlate, Verified)
VALUES
(2, 'Car', 'Toyota', 'Vios', 'Black', 2019, 4, '51A-12345', 1),
(3, 'Motorbike', 'Honda', 'AirBlade', 'Red', 2021, 1, '59X2-67890', 1);
GO

-- Trip
INSERT INTO Trip (DriverId, VehicleId, PickupLocation, DropoffLocation, StartTime, EndTime, SeatTotal, SeatAvailable, PricePerSeat, Status)
VALUES
(2, 1, N'FPT University HCM', N'Ben Thanh Market', '2025-09-20 08:00:00', '2025-09-20 08:45:00', 4, 3, 50000, 'Active'),
(3, 2, N'HCM City Center', N'Tan Son Nhat Airport', '2025-09-20 10:00:00', '2025-09-20 10:30:00', 1, 1, 70000, 'Pending');
GO

-- Booking
INSERT INTO Booking (TripId, PassengerId, SeatCount, TotalPrice, Status)
VALUES
(1, 1, 1, 50000, 'Accepted'),
(2, 1, 1, 70000, 'Pending');
GO

-- Wallet
INSERT INTO Wallet (UserId, Balance)
VALUES
(1, 200000),
(2, 150000),
(3, 100000);
GO

-- Transaction
INSERT INTO [Transaction] (WalletId, Amount, Type, ReferenceType, ReferenceId)
VALUES
(1, 200000, 'Deposit', 'Manual', NULL),
(2, 50000, 'Payment', 'Booking', 1),
(3, 100000, 'Deposit', 'Manual', NULL);
GO

-- Voucher (CreatedBy = AdminId: 1 hoặc 2)
INSERT INTO Voucher (Code, Description, DiscountType, DiscountValue, MinOrderValue, ExpiryDate, UsageLimit, CreatedBy)
VALUES
('DISCOUNT10', N'Giảm 10% cho mọi chuyến đi', 'Percent', 10, 0, '2025-12-31', NULL, 1),
('FIX50K', N'Giảm 50.000đ cho chuyến đi trên 200.000đ', 'Fixed', 50000, 200000, '2025-10-31', 100, 2);
GO

-- UserVoucher
INSERT INTO UserVoucher (UserId, VoucherId, IsUsed)
VALUES
(1, 1, 0),
(2, 2, 0);
GO

-- Review
INSERT INTO Review (TripId, FromUserId, ToUserId, Rating, Comment)
VALUES
(1, 1, 2, 5, N'Tài xế thân thiện, lái xe an toàn'),
(1, 2, 1, 4, N'Hành khách đúng giờ');
GO

-- Notification (CreatedBy = AdminId: 1 hoặc 2)
INSERT INTO Notification (UserId, Title, Message, Type, IsRead, CreatedBy)
VALUES
(1, N'Booking Accepted', N'Chuyến đi của bạn đã được tài xế xác nhận', 'Booking', 0, 1),
(NULL, N'Hệ thống bảo trì', N'Dịch vụ sẽ bảo trì lúc 2:00 AM', 'System', 0, 2);
GO

-- Message
INSERT INTO Message (TripId, FromUserId, ToUserId, MessageText, IsRead)
VALUES
(1, 1, 2, N'Anh ơi em đang chờ ở cổng trường nhé!', 0),
(1, 2, 1, N'Ok, 5 phút nữa anh tới.', 0);
GO

-- Report (AdminId có thể NULL vì chưa xử lý)
INSERT INTO Report (ReporterId, ReportedUserId, TripId, Reason, Details, Status, AdminId)
VALUES
(1, 3, 2, N'Tài xế không đến điểm hẹn', N'Đợi 15 phút không thấy tới', 'Pending', NULL);
GO

-- PasswordResetToken
INSERT INTO PasswordResetToken (UserId, Token, ExpiresAt, Used)
VALUES
(1, 'RESET_TOKEN_ABC123', DATEADD(HOUR, 1, SYSDATETIME()), 0),
(2, 'RESET_TOKEN_DEF456', DATEADD(HOUR, 1, SYSDATETIME()), 0);
GO
