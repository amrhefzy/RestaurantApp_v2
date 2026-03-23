-- ═══════════════════════════════════════════════════════════════════════════
-- RestaurantMS  — Dev Migration v1
-- Run on SQL Server 2019+ (or Azure SQL)
-- All text columns: NVARCHAR
-- All decimals:     DECIMAL(18,3)
-- All tables include: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted
-- ═══════════════════════════════════════════════════════════════════════════

SET NOCOUNT ON;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. ASP.NET Core Identity tables
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetRoles')
BEGIN
    CREATE TABLE AspNetRoles (
        Id               INT           NOT NULL IDENTITY(1,1),
        Name             NVARCHAR(256) NULL,
        NormalizedName   NVARCHAR(256) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX IX_AspNetRoles_NormalizedName ON AspNetRoles (NormalizedName)
        WHERE NormalizedName IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUsers')
BEGIN
    CREATE TABLE AspNetUsers (
        Id                   INT            NOT NULL IDENTITY(1,1),
        -- Custom columns
        FullNameAr           NVARCHAR(100)  NOT NULL DEFAULT N'',
        FullNameEn           NVARCHAR(100)  NOT NULL DEFAULT N'',
        ManagerPin           NVARCHAR(100)  NULL,
        IsActive             BIT            NOT NULL DEFAULT 1,
        -- Standard Identity columns
        UserName             NVARCHAR(256)  NULL,
        NormalizedUserName   NVARCHAR(256)  NULL,
        Email                NVARCHAR(256)  NULL,
        NormalizedEmail      NVARCHAR(256)  NULL,
        EmailConfirmed       BIT            NOT NULL DEFAULT 0,
        PasswordHash         NVARCHAR(MAX)  NULL,
        SecurityStamp        NVARCHAR(MAX)  NULL,
        ConcurrencyStamp     NVARCHAR(MAX)  NULL,
        PhoneNumber          NVARCHAR(50)   NULL,
        PhoneNumberConfirmed BIT            NOT NULL DEFAULT 0,
        TwoFactorEnabled     BIT            NOT NULL DEFAULT 0,
        LockoutEnd           DATETIMEOFFSET NULL,
        LockoutEnabled       BIT            NOT NULL DEFAULT 1,
        AccessFailedCount    INT            NOT NULL DEFAULT 0,
        CONSTRAINT PK_AspNetUsers PRIMARY KEY (Id)
    );
    CREATE UNIQUE INDEX IX_AspNetUsers_NormalizedUserName ON AspNetUsers (NormalizedUserName)
        WHERE NormalizedUserName IS NOT NULL;
    CREATE UNIQUE INDEX IX_AspNetUsers_NormalizedEmail ON AspNetUsers (NormalizedEmail)
        WHERE NormalizedEmail IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUserRoles')
BEGIN
    CREATE TABLE AspNetUserRoles (
        UserId INT NOT NULL,
        RoleId INT NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_Roles FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id)  ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUserClaims')
BEGIN
    CREATE TABLE AspNetUserClaims (
        Id         INT           NOT NULL IDENTITY(1,1),
        UserId     INT           NOT NULL,
        ClaimType  NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserClaims PRIMARY KEY (Id),
        CONSTRAINT FK_AspNetUserClaims_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUserLogins')
BEGIN
    CREATE TABLE AspNetUserLogins (
        LoginProvider       NVARCHAR(128) NOT NULL,
        ProviderKey         NVARCHAR(128) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId              INT           NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetUserTokens')
BEGIN
    CREATE TABLE AspNetUserTokens (
        UserId        INT           NOT NULL,
        LoginProvider NVARCHAR(128) NOT NULL,
        Name          NVARCHAR(128) NOT NULL,
        Value         NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_Users FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetRoleClaims')
BEGIN
    CREATE TABLE AspNetRoleClaims (
        Id         INT           NOT NULL IDENTITY(1,1),
        RoleId     INT           NOT NULL,
        ClaimType  NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY (Id),
        CONSTRAINT FK_AspNetRoleClaims_Roles FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
    );
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Categories
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Categories')
BEGIN
    CREATE TABLE Categories (
        Id          INT            NOT NULL IDENTITY(1,1),
        NameAr      NVARCHAR(100)  NOT NULL,
        NameEn      NVARCHAR(100)  NOT NULL,
        Icon        NVARCHAR(50)   NULL,
        SortOrder   INT            NOT NULL DEFAULT 0,
        IsActive    BIT            NOT NULL DEFAULT 1,
        -- BaseEntity
        CreatedAt   DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy   INT            NULL,
        UpdatedAt   DATETIME2      NULL,
        UpdatedBy   INT            NULL,
        IsDeleted   BIT            NOT NULL DEFAULT 0,
        CONSTRAINT PK_Categories PRIMARY KEY (Id)
    );
    CREATE INDEX IX_Categories_IsDeleted ON Categories (IsDeleted);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. MenuItems
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MenuItems')
BEGIN
    CREATE TABLE MenuItems (
        Id          INT              NOT NULL IDENTITY(1,1),
        CategoryId  INT              NOT NULL,
        NameAr      NVARCHAR(200)    NOT NULL,
        NameEn      NVARCHAR(200)    NOT NULL,
        Description NVARCHAR(500)    NULL,
        Price       DECIMAL(18,3)    NOT NULL DEFAULT 0,
        Cost        DECIMAL(18,3)    NULL,
        Barcode     NVARCHAR(100)    NULL,
        Icon        NVARCHAR(50)     NULL,
        ImageUrl    NVARCHAR(300)    NULL,
        IsAvailable BIT              NOT NULL DEFAULT 1,
        SortOrder   INT              NOT NULL DEFAULT 0,
        -- BaseEntity
        CreatedAt   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy   INT              NULL,
        UpdatedAt   DATETIME2        NULL,
        UpdatedBy   INT              NULL,
        IsDeleted   BIT              NOT NULL DEFAULT 0,
        CONSTRAINT PK_MenuItems    PRIMARY KEY (Id),
        CONSTRAINT FK_MenuItems_Category FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
    );
    CREATE INDEX IX_MenuItems_CategoryId ON MenuItems (CategoryId);
    CREATE INDEX IX_MenuItems_IsDeleted  ON MenuItems (IsDeleted);
    CREATE UNIQUE INDEX IX_MenuItems_Barcode ON MenuItems (Barcode)
        WHERE Barcode IS NOT NULL AND IsDeleted = 0;
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 4. PosOrders
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PosOrders')
BEGIN
    CREATE TABLE PosOrders (
        Id             INT           NOT NULL IDENTITY(1,1),
        OrderNumber    NVARCHAR(50)  NOT NULL,
        TableNumber    NVARCHAR(20)  NULL,
        OrderType      INT           NOT NULL DEFAULT 1,  -- 1=DineIn 2=Takeaway 3=Delivery
        Status         INT           NOT NULL DEFAULT 1,  -- 1=Open 2=Paid 3=Voided 4=Held
        CashierId      INT           NOT NULL,
        ShiftId        INT           NULL,
        SubTotal       DECIMAL(18,3) NOT NULL DEFAULT 0,
        TaxAmount      DECIMAL(18,3) NOT NULL DEFAULT 0,
        DiscountAmount DECIMAL(18,3) NOT NULL DEFAULT 0,
        TotalAmount    DECIMAL(18,3) NOT NULL DEFAULT 0,
        Notes          NVARCHAR(500) NULL,
        PaidAt         DATETIME2     NULL,
        -- BaseEntity
        CreatedAt      DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy      INT           NULL,
        UpdatedAt      DATETIME2     NULL,
        UpdatedBy      INT           NULL,
        IsDeleted      BIT           NOT NULL DEFAULT 0,
        CONSTRAINT PK_PosOrders PRIMARY KEY (Id),
        CONSTRAINT FK_PosOrders_Cashier FOREIGN KEY (CashierId) REFERENCES AspNetUsers(Id)
    );
    CREATE UNIQUE INDEX IX_PosOrders_OrderNumber ON PosOrders (OrderNumber);
    CREATE INDEX IX_PosOrders_CashierId           ON PosOrders (CashierId);
    CREATE INDEX IX_PosOrders_Status              ON PosOrders (Status);
    CREATE INDEX IX_PosOrders_PaidAt              ON PosOrders (PaidAt);
    CREATE INDEX IX_PosOrders_IsDeleted           ON PosOrders (IsDeleted);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 5. PosOrderItems
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PosOrderItems')
BEGIN
    CREATE TABLE PosOrderItems (
        Id         INT           NOT NULL IDENTITY(1,1),
        OrderId    INT           NOT NULL,
        MenuItemId INT           NOT NULL,
        Quantity   DECIMAL(18,3) NOT NULL DEFAULT 1,
        UnitPrice  DECIMAL(18,3) NOT NULL DEFAULT 0,
        LineTotal  DECIMAL(18,3) NOT NULL DEFAULT 0,
        Notes      NVARCHAR(300) NULL,
        -- BaseEntity
        CreatedAt  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy  INT           NULL,
        UpdatedAt  DATETIME2     NULL,
        UpdatedBy  INT           NULL,
        IsDeleted  BIT           NOT NULL DEFAULT 0,
        CONSTRAINT PK_PosOrderItems PRIMARY KEY (Id),
        CONSTRAINT FK_PosOrderItems_Order    FOREIGN KEY (OrderId)    REFERENCES PosOrders(Id)  ON DELETE CASCADE,
        CONSTRAINT FK_PosOrderItems_MenuItem FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id)
    );
    CREATE INDEX IX_PosOrderItems_OrderId    ON PosOrderItems (OrderId);
    CREATE INDEX IX_PosOrderItems_MenuItemId ON PosOrderItems (MenuItemId);
    CREATE INDEX IX_PosOrderItems_IsDeleted  ON PosOrderItems (IsDeleted);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 6. Payments
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Payments')
BEGIN
    CREATE TABLE Payments (
        Id            INT           NOT NULL IDENTITY(1,1),
        OrderId       INT           NOT NULL,
        CashierId     INT           NOT NULL,
        PaymentMethod INT           NOT NULL DEFAULT 1,  -- 1=Cash 2=Card 3=Split
        Amount        DECIMAL(18,3) NOT NULL DEFAULT 0,
        CashReceived  DECIMAL(18,3) NULL,
        ChangeGiven   DECIMAL(18,3) NULL,
        PaidAt        DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        -- BaseEntity
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy     INT           NULL,
        UpdatedAt     DATETIME2     NULL,
        UpdatedBy     INT           NULL,
        IsDeleted     BIT           NOT NULL DEFAULT 0,
        CONSTRAINT PK_Payments PRIMARY KEY (Id),
        CONSTRAINT FK_Payments_Order   FOREIGN KEY (OrderId)   REFERENCES PosOrders(Id),
        CONSTRAINT FK_Payments_Cashier FOREIGN KEY (CashierId) REFERENCES AspNetUsers(Id)
    );
    CREATE INDEX IX_Payments_OrderId    ON Payments (OrderId);
    CREATE INDEX IX_Payments_PaidAt     ON Payments (PaidAt);
    CREATE INDEX IX_Payments_IsDeleted  ON Payments (IsDeleted);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 7. CashHandovers
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CashHandovers')
BEGIN
    CREATE TABLE CashHandovers (
        Id                INT           NOT NULL IDENTITY(1,1),
        CashierId         INT           NOT NULL,
        ManagerId         INT           NULL,
        ShiftId           INT           NULL,
        OpenFloat         DECIMAL(18,3) NOT NULL DEFAULT 0,
        CashSalesTotal    DECIMAL(18,3) NOT NULL DEFAULT 0,
        CashRefundsTotal  DECIMAL(18,3) NOT NULL DEFAULT 0,
        -- ExpectedCash is computed in C#: OpenFloat + CashSalesTotal - CashRefundsTotal
        ActualCash        DECIMAL(18,3) NULL,
        Difference        DECIMAL(18,3) NULL,
        Status            INT           NOT NULL DEFAULT 1,  -- 1=Open 2=PendingApproval 3=Closed 4=Flagged
        OpenedAt          DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        ClosedAt          DATETIME2     NULL,
        ManagerApprovedAt DATETIME2     NULL,
        CashierNotes      NVARCHAR(500) NULL,
        ManagerNotes      NVARCHAR(500) NULL,
        -- BaseEntity
        CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy         INT           NULL,
        UpdatedAt         DATETIME2     NULL,
        UpdatedBy         INT           NULL,
        IsDeleted         BIT           NOT NULL DEFAULT 0,
        CONSTRAINT PK_CashHandovers PRIMARY KEY (Id),
        CONSTRAINT FK_CashHandovers_Cashier FOREIGN KEY (CashierId) REFERENCES AspNetUsers(Id),
        CONSTRAINT FK_CashHandovers_Manager FOREIGN KEY (ManagerId) REFERENCES AspNetUsers(Id)
    );
    CREATE INDEX IX_CashHandovers_CashierId  ON CashHandovers (CashierId);
    CREATE INDEX IX_CashHandovers_Status     ON CashHandovers (Status);
    CREATE INDEX IX_CashHandovers_OpenedAt   ON CashHandovers (OpenedAt);
    CREATE INDEX IX_CashHandovers_IsDeleted  ON CashHandovers (IsDeleted);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 8. ShiftTemplates
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ShiftTemplates')
BEGIN
    CREATE TABLE ShiftTemplates (
        Id         INT           NOT NULL IDENTITY(1,1),
        NameAr     NVARCHAR(100) NOT NULL,
        NameEn     NVARCHAR(100) NOT NULL,
        StartTime  TIME          NOT NULL,   -- maps from C# TimeSpan
        EndTime    TIME          NOT NULL,
        IsActive   BIT           NOT NULL DEFAULT 1,
        -- BaseEntity
        CreatedAt  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy  INT           NULL,
        UpdatedAt  DATETIME2     NULL,
        UpdatedBy  INT           NULL,
        IsDeleted  BIT           NOT NULL DEFAULT 0,
        CONSTRAINT PK_ShiftTemplates PRIMARY KEY (Id)
    );
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 9. EmployeeShifts
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmployeeShifts')
BEGIN
    CREATE TABLE EmployeeShifts (
        Id              INT           NOT NULL IDENTITY(1,1),
        EmployeeId      INT           NOT NULL,
        TemplateId      INT           NOT NULL,
        ShiftDate       DATE          NOT NULL,
        ClockIn         DATETIME2     NULL,
        ClockOut        DATETIME2     NULL,
        PlannedStart    DATETIME2     NOT NULL,
        PlannedEnd      DATETIME2     NOT NULL,
        OvertimeMinutes INT           NOT NULL DEFAULT 0,
        Status          INT           NOT NULL DEFAULT 1,  -- ShiftStatus enum
        ManagerId       INT           NULL,
        Notes           NVARCHAR(500) NULL,
        -- BaseEntity
        CreatedAt       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy       INT           NULL,
        UpdatedAt       DATETIME2     NULL,
        UpdatedBy       INT           NULL,
        IsDeleted       BIT           NOT NULL DEFAULT 0,
        CONSTRAINT PK_EmployeeShifts PRIMARY KEY (Id),
        CONSTRAINT FK_EmployeeShifts_Employee FOREIGN KEY (EmployeeId) REFERENCES AspNetUsers(Id),
        CONSTRAINT FK_EmployeeShifts_Template FOREIGN KEY (TemplateId) REFERENCES ShiftTemplates(Id),
        CONSTRAINT FK_EmployeeShifts_Manager  FOREIGN KEY (ManagerId)  REFERENCES AspNetUsers(Id)
    );
    CREATE INDEX IX_EmployeeShifts_EmployeeId ON EmployeeShifts (EmployeeId);
    CREATE INDEX IX_EmployeeShifts_ShiftDate  ON EmployeeShifts (ShiftDate);
    CREATE INDEX IX_EmployeeShifts_Status     ON EmployeeShifts (Status);
    CREATE INDEX IX_EmployeeShifts_IsDeleted  ON EmployeeShifts (IsDeleted);
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 10. PrinterProfiles
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PrinterProfiles')
BEGIN
    CREATE TABLE PrinterProfiles (
        Id            INT           NOT NULL IDENTITY(1,1),
        Name          NVARCHAR(100) NOT NULL,
        IpAddress     NVARCHAR(50)  NOT NULL,
        Port          INT           NOT NULL DEFAULT 9100,
        PrinterType   INT           NOT NULL DEFAULT 1,  -- ThermalReceipt=1 A4Invoice=2 KitchenTicket=3
        PaperWidth    INT           NOT NULL DEFAULT 2,  -- W58mm=1 W80mm=2 A4=3
        Location      INT           NOT NULL DEFAULT 1,  -- Cashier=1 Kitchen=2 Bar=3 Management=4
        IsActive      BIT           NOT NULL DEFAULT 1,
        -- BaseEntity
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy     INT           NULL,
        UpdatedAt     DATETIME2     NULL,
        UpdatedBy     INT           NULL,
        IsDeleted     BIT           NOT NULL DEFAULT 0,
        CONSTRAINT PK_PrinterProfiles PRIMARY KEY (Id)
    );
END
GO

PRINT 'dev_migration_v1.sql: all tables created (IF NOT EXISTS).';
GO
