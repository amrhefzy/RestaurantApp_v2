-- ═══════════════════════════════════════════════════════════════════════════
-- RestaurantMS  — Dev Seed Data
-- Run AFTER dev_migration_v1.sql
-- BCrypt hashes below use workFactor=11, password = "Admin@123", PIN = "1234"
-- ═══════════════════════════════════════════════════════════════════════════

SET NOCOUNT ON;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Roles  (matches EF seed in RestaurantDbContext)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'SUPERADMIN')
BEGIN
    INSERT INTO AspNetRoles (Name, NormalizedName, ConcurrencyStamp) VALUES
    ('SuperAdmin',   'SUPERADMIN',   NEWID()),
    ('Manager',      'MANAGER',      NEWID()),
    ('Cashier',      'CASHIER',      NEWID()),
    ('Waiter',       'WAITER',       NEWID()),
    ('KitchenStaff', 'KITCHENSTAFF', NEWID()),
    ('Accountant',   'ACCOUNTANT',   NEWID());
    PRINT 'Roles seeded.';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Users  (password: Admin@123, PIN hash for "1234")
-- ─────────────────────────────────────────────────────────────────────────────

DECLARE @pwHash    NVARCHAR(MAX) = N'$2a$11$xJ9mJ9.Zfp1vQ9EXAMPLE_HASH_REPLACE_WITH_REAL';
DECLARE @pinHash   NVARCHAR(100) = N'$2a$11$xJ9mJ9.Zfp1vQ9EXAMPLE_PIN_REPLACE_WITH_REAL';
DECLARE @secStamp  NVARCHAR(MAX) = NEWID();

IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE NormalizedUserName = 'ADMIN@RESTAURANT.COM')
BEGIN
    INSERT INTO AspNetUsers
        (FullNameAr, FullNameEn, ManagerPin, IsActive,
         UserName, NormalizedUserName, Email, NormalizedEmail,
         EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
         LockoutEnabled, AccessFailedCount)
    VALUES
    -- SuperAdmin
    (N'مدير النظام', 'System Admin',    @pinHash, 1,
     'admin@restaurant.com', 'ADMIN@RESTAURANT.COM',
     'admin@restaurant.com', 'ADMIN@RESTAURANT.COM',
     1, @pwHash, @secStamp, NEWID(), 1, 0),

    -- Manager
    (N'أحمد محمد', 'Ahmed Mohamed',     @pinHash, 1,
     'manager@restaurant.com', 'MANAGER@RESTAURANT.COM',
     'manager@restaurant.com', 'MANAGER@RESTAURANT.COM',
     1, @pwHash, NEWID(), NEWID(), 1, 0),

    -- Cashier 1
    (N'سارة علي', 'Sara Ali',           NULL, 1,
     'cashier1@restaurant.com', 'CASHIER1@RESTAURANT.COM',
     'cashier1@restaurant.com', 'CASHIER1@RESTAURANT.COM',
     1, @pwHash, NEWID(), NEWID(), 1, 0),

    -- Cashier 2
    (N'محمد خالد', 'Mohamed Khaled',    NULL, 1,
     'cashier2@restaurant.com', 'CASHIER2@RESTAURANT.COM',
     'cashier2@restaurant.com', 'CASHIER2@RESTAURANT.COM',
     1, @pwHash, NEWID(), NEWID(), 1, 0),

    -- KitchenStaff
    (N'علي حسن', 'Ali Hassan',          NULL, 1,
     'kitchen@restaurant.com', 'KITCHEN@RESTAURANT.COM',
     'kitchen@restaurant.com', 'KITCHEN@RESTAURANT.COM',
     1, @pwHash, NEWID(), NEWID(), 1, 0);

    PRINT 'Users seeded. NOTE: Replace password/PIN hashes with real BCrypt values!';
END
GO

-- Assign roles
IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = 1)
BEGIN
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT u.Id, r.Id FROM AspNetUsers u CROSS JOIN AspNetRoles r
    WHERE u.NormalizedUserName = 'ADMIN@RESTAURANT.COM'     AND r.NormalizedName = 'SUPERADMIN'
    UNION ALL
    SELECT u.Id, r.Id FROM AspNetUsers u CROSS JOIN AspNetRoles r
    WHERE u.NormalizedUserName = 'MANAGER@RESTAURANT.COM'   AND r.NormalizedName = 'MANAGER'
    UNION ALL
    SELECT u.Id, r.Id FROM AspNetUsers u CROSS JOIN AspNetRoles r
    WHERE u.NormalizedUserName = 'CASHIER1@RESTAURANT.COM'  AND r.NormalizedName = 'CASHIER'
    UNION ALL
    SELECT u.Id, r.Id FROM AspNetUsers u CROSS JOIN AspNetRoles r
    WHERE u.NormalizedUserName = 'CASHIER2@RESTAURANT.COM'  AND r.NormalizedName = 'CASHIER'
    UNION ALL
    SELECT u.Id, r.Id FROM AspNetUsers u CROSS JOIN AspNetRoles r
    WHERE u.NormalizedUserName = 'KITCHEN@RESTAURANT.COM'   AND r.NormalizedName = 'KITCHENSTAFF';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Categories
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM Categories WHERE IsDeleted = 0)
BEGIN
    INSERT INTO Categories (NameAr, NameEn, Icon, SortOrder, IsActive, CreatedAt) VALUES
    (N'برغر',       'Burgers',  N'🍔', 1, 1, SYSUTCDATETIME()),
    (N'بيتزا',      'Pizza',    N'🍕', 2, 1, SYSUTCDATETIME()),
    (N'مشروبات',    'Drinks',   N'🥤', 3, 1, SYSUTCDATETIME()),
    (N'حلويات',     'Desserts', N'🍰', 4, 1, SYSUTCDATETIME()),
    (N'سلطات',      'Salads',   N'🥗', 5, 1, SYSUTCDATETIME()),
    (N'أطباق خاصة', 'Specials', N'⭐', 6, 1, SYSUTCDATETIME());
    PRINT 'Categories seeded.';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 4. Menu Items (20 items)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM MenuItems WHERE IsDeleted = 0)
BEGIN
    DECLARE @catBurger  INT = (SELECT Id FROM Categories WHERE NameEn = 'Burgers');
    DECLARE @catPizza   INT = (SELECT Id FROM Categories WHERE NameEn = 'Pizza');
    DECLARE @catDrinks  INT = (SELECT Id FROM Categories WHERE NameEn = 'Drinks');
    DECLARE @catDessert INT = (SELECT Id FROM Categories WHERE NameEn = 'Desserts');
    DECLARE @catSalad   INT = (SELECT Id FROM Categories WHERE NameEn = 'Salads');
    DECLARE @catSpecial INT = (SELECT Id FROM Categories WHERE NameEn = 'Specials');

    INSERT INTO MenuItems (CategoryId, NameAr, NameEn, Price, IsAvailable, SortOrder, CreatedAt) VALUES
    -- Burgers
    (@catBurger,  N'برغر كلاسيك',      'Classic Burger',        3.500, 1, 1,  SYSUTCDATETIME()),
    (@catBurger,  N'برغر جبن مزدوج',   'Double Cheese Burger',  4.500, 1, 2,  SYSUTCDATETIME()),
    (@catBurger,  N'برغر مشروم',        'Mushroom Burger',       4.000, 1, 3,  SYSUTCDATETIME()),
    (@catBurger,  N'دجاج كريسبي',       'Crispy Chicken',        3.750, 1, 4,  SYSUTCDATETIME()),
    -- Pizza
    (@catPizza,   N'بيتزا مارغريتا',    'Margherita Pizza',      5.000, 1, 1,  SYSUTCDATETIME()),
    (@catPizza,   N'بيتزا دجاج BBQ',   'BBQ Chicken Pizza',     6.500, 1, 2,  SYSUTCDATETIME()),
    (@catPizza,   N'بيتزا خضار',        'Veggie Pizza',          5.500, 1, 3,  SYSUTCDATETIME()),
    -- Drinks
    (@catDrinks,  N'كولا',              'Cola',                  1.000, 1, 1,  SYSUTCDATETIME()),
    (@catDrinks,  N'ليموناضة',          'Lemonade',              1.500, 1, 2,  SYSUTCDATETIME()),
    (@catDrinks,  N'عصير برتقال',       'Orange Juice',          2.000, 1, 3,  SYSUTCDATETIME()),
    (@catDrinks,  N'ماء معدني',         'Mineral Water',         0.500, 1, 4,  SYSUTCDATETIME()),
    -- Desserts
    (@catDessert, N'كيكة شوكولاتة',    'Chocolate Cake',        2.500, 1, 1,  SYSUTCDATETIME()),
    (@catDessert, N'آيس كريم',          'Ice Cream',             2.000, 1, 2,  SYSUTCDATETIME()),
    (@catDessert, N'كنافة',             'Kunafa',                3.000, 1, 3,  SYSUTCDATETIME()),
    -- Salads
    (@catSalad,   N'سلطة سيزر',         'Caesar Salad',          3.500, 1, 1,  SYSUTCDATETIME()),
    (@catSalad,   N'سلطة خضراء',        'Green Salad',           2.500, 1, 2,  SYSUTCDATETIME()),
    (@catSalad,   N'سلطة فتوش',         'Fattoush Salad',        2.750, 1, 3,  SYSUTCDATETIME()),
    -- Specials
    (@catSpecial, N'طبق اليوم',         'Daily Special',         7.000, 1, 1,  SYSUTCDATETIME()),
    (@catSpecial, N'ريش لحم مشوية',     'Grilled Ribs',          12.000, 1, 2, SYSUTCDATETIME()),
    (@catSpecial, N'سيفود ميكس',        'Seafood Mix',           15.000, 1, 3, SYSUTCDATETIME());

    PRINT 'Menu items seeded (20 items).';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 5. Shift Templates
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM ShiftTemplates WHERE IsDeleted = 0)
BEGIN
    INSERT INTO ShiftTemplates (NameAr, NameEn, StartTime, EndTime, IsActive, CreatedAt) VALUES
    (N'وردية صباح',  'Morning Shift',   '08:00:00', '16:00:00', 1, SYSUTCDATETIME()),
    (N'وردية مساء',  'Afternoon Shift', '14:00:00', '22:00:00', 1, SYSUTCDATETIME()),
    (N'وردية ليل',   'Night Shift',     '22:00:00', '06:00:00', 1, SYSUTCDATETIME());
    PRINT 'Shift templates seeded.';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 6. Printer Profiles
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM PrinterProfiles WHERE IsDeleted = 0)
BEGIN
    INSERT INTO PrinterProfiles
        (Name, IpAddress, Port, PrinterType, PaperWidth, Location, IsActive, CreatedAt)
    VALUES
    (N'Cashier Receipt',  '192.168.1.100', 9100, 1, 2, 1, 1, SYSUTCDATETIME()),
    (N'Kitchen Printer',  '192.168.1.101', 9100, 3, 2, 2, 1, SYSUTCDATETIME());
    PRINT 'Printer profiles seeded.';
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 7. Sample Orders (5 paid orders for today)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM PosOrders WHERE IsDeleted = 0 AND Status = 2)
BEGIN
    DECLARE @cashierId INT = (SELECT TOP 1 Id FROM AspNetUsers
                              WHERE NormalizedUserName = 'CASHIER1@RESTAURANT.COM');
    DECLARE @today     DATE = CAST(SYSUTCDATETIME() AS DATE);

    -- Order 1
    INSERT INTO PosOrders
        (OrderNumber, TableNumber, OrderType, Status, CashierId,
         SubTotal, TaxAmount, DiscountAmount, TotalAmount, PaidAt, CreatedAt)
    VALUES (CONCAT(N'ORD-', FORMAT(@today,'yyyyMMdd'), N'-0001'),
            N'T1', 1, 2, @cashierId,
            8.500, 1.190, 0, 9.690, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Order 2
    INSERT INTO PosOrders
        (OrderNumber, TableNumber, OrderType, Status, CashierId,
         SubTotal, TaxAmount, DiscountAmount, TotalAmount, PaidAt, CreatedAt)
    VALUES (CONCAT(N'ORD-', FORMAT(@today,'yyyyMMdd'), N'-0002'),
            N'T2', 1, 2, @cashierId,
            12.000, 1.680, 1.000, 12.680, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Order 3 (Takeaway)
    INSERT INTO PosOrders
        (OrderNumber, TableNumber, OrderType, Status, CashierId,
         SubTotal, TaxAmount, DiscountAmount, TotalAmount, PaidAt, CreatedAt)
    VALUES (CONCAT(N'ORD-', FORMAT(@today,'yyyyMMdd'), N'-0003'),
            NULL, 2, 2, @cashierId,
            5.000, 0.700, 0, 5.700, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Order 4
    INSERT INTO PosOrders
        (OrderNumber, TableNumber, OrderType, Status, CashierId,
         SubTotal, TaxAmount, DiscountAmount, TotalAmount, PaidAt, CreatedAt)
    VALUES (CONCAT(N'ORD-', FORMAT(@today,'yyyyMMdd'), N'-0004'),
            N'T3', 1, 2, @cashierId,
            20.000, 2.800, 0, 22.800, SYSUTCDATETIME(), SYSUTCDATETIME());

    -- Order 5 (Voided)
    INSERT INTO PosOrders
        (OrderNumber, TableNumber, OrderType, Status, CashierId,
         SubTotal, TaxAmount, DiscountAmount, TotalAmount, CreatedAt)
    VALUES (CONCAT(N'ORD-', FORMAT(@today,'yyyyMMdd'), N'-0005'),
            N'T4', 1, 3, @cashierId,
            6.000, 0.840, 0, 6.840, SYSUTCDATETIME());

    -- OrderItems for Order 1
    DECLARE @o1 INT = SCOPE_IDENTITY() - 4;
    DECLARE @item1 INT = (SELECT TOP 1 Id FROM MenuItems WHERE NameEn = 'Classic Burger');
    DECLARE @item2 INT = (SELECT TOP 1 Id FROM MenuItems WHERE NameEn = 'Cola');
    INSERT INTO PosOrderItems (OrderId, MenuItemId, Quantity, UnitPrice, LineTotal, CreatedAt) VALUES
    (@o1,     @item1, 2, 3.500, 7.000, SYSUTCDATETIME()),
    (@o1,     @item2, 2, 1.000, 2.000, SYSUTCDATETIME());

    -- Payments for Orders 1-4 (cash)
    INSERT INTO Payments (OrderId, CashierId, PaymentMethod, Amount, CashReceived, ChangeGiven, PaidAt, CreatedAt)
    SELECT Id, CashierId, 1, TotalAmount, TotalAmount, 0, PaidAt, SYSUTCDATETIME()
    FROM PosOrders WHERE Status = 2 AND IsDeleted = 0;

    PRINT 'Sample orders and payments seeded.';
END
GO
