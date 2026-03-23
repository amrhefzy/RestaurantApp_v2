-- ═══════════════════════════════════════════════════════════════════════════
-- RestaurantMS  — Stored Procedures
-- Run AFTER dev_migration_v1.sql
-- ═══════════════════════════════════════════════════════════════════════════

SET NOCOUNT ON;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- sp_GetDailySalesReport
-- Returns daily sales totals + category breakdown for a given date.
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('dbo.sp_GetDailySalesReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetDailySalesReport;
GO

CREATE PROCEDURE dbo.sp_GetDailySalesReport
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Summary row
    SELECT
        @Date                                           AS ReportDate,
        COUNT(o.Id)                                     AS OrderCount,
        ISNULL(SUM(o.SubTotal),       0)                AS SubTotal,
        ISNULL(SUM(o.TaxAmount),      0)                AS TaxAmount,
        ISNULL(SUM(o.DiscountAmount), 0)                AS DiscountAmount,
        ISNULL(SUM(o.TotalAmount),    0)                AS NetSales,
        ISNULL(SUM(CASE WHEN p.PaymentMethod = 1 THEN p.Amount ELSE 0 END), 0) AS CashTotal,
        ISNULL(SUM(CASE WHEN p.PaymentMethod = 2 THEN p.Amount ELSE 0 END), 0) AS CardTotal,
        SUM(CASE WHEN o.Status = 3 THEN 1 ELSE 0 END)  AS VoidedOrders,
        CASE WHEN COUNT(o.Id) > 0
             THEN ISNULL(SUM(o.TotalAmount), 0) / COUNT(o.Id)
             ELSE 0 END                                 AS AvgOrderValue
    FROM PosOrders o
    LEFT JOIN Payments p ON p.OrderId = o.Id AND p.IsDeleted = 0
    WHERE CAST(o.PaidAt AS DATE) = @Date
      AND o.IsDeleted = 0
      AND o.Status = 2;  -- Paid only

    -- Category breakdown
    SELECT
        c.NameAr                        AS CategoryNameAr,
        c.NameEn                        AS CategoryNameEn,
        SUM(oi.Quantity)                AS TotalQuantity,
        SUM(oi.LineTotal)               AS CategoryRevenue
    FROM PosOrderItems oi
    INNER JOIN PosOrders o ON o.Id = oi.OrderId
    INNER JOIN MenuItems m ON m.Id = oi.MenuItemId
    INNER JOIN Categories c ON c.Id = m.CategoryId
    WHERE CAST(o.PaidAt AS DATE) = @Date
      AND o.IsDeleted  = 0
      AND oi.IsDeleted = 0
      AND m.IsDeleted  = 0
      AND o.Status = 2
    GROUP BY c.Id, c.NameAr, c.NameEn
    ORDER BY CategoryRevenue DESC;
END
GO


-- ─────────────────────────────────────────────────────────────────────────────
-- sp_GetZReport
-- Full Z-Report (end-of-day) data for a given date.
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('dbo.sp_GetZReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetZReport;
GO

CREATE PROCEDURE dbo.sp_GetZReport
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Main totals
    SELECT
        @Date                                                   AS ReportDate,
        COUNT(DISTINCT o.Id)                                    AS TotalOrders,
        ISNULL(SUM(o.SubTotal),       0)                        AS SubTotal,
        ISNULL(SUM(o.TaxAmount),      0)                        AS TaxAmount,
        ISNULL(SUM(o.DiscountAmount), 0)                        AS DiscountAmount,
        ISNULL(SUM(o.TotalAmount),    0)                        AS NetSales,
        ISNULL(SUM(CASE WHEN p.PaymentMethod = 1 THEN p.Amount ELSE 0 END), 0) AS CashSales,
        ISNULL(SUM(CASE WHEN p.PaymentMethod = 2 THEN p.Amount ELSE 0 END), 0) AS CardSales,
        ISNULL(SUM(CASE WHEN p.PaymentMethod = 3 THEN p.Amount ELSE 0 END), 0) AS SplitSales,
        SUM(CASE WHEN o.Status = 3 THEN 1 ELSE 0 END)          AS VoidedOrders,
        -- Refunds would be here in a full implementation
        0                                                       AS TotalRefunds
    FROM PosOrders o
    LEFT JOIN Payments p ON p.OrderId = o.Id AND p.IsDeleted = 0
    WHERE CAST(o.CreatedAt AS DATE) = @Date
      AND o.IsDeleted = 0;

    -- Payment method breakdown
    SELECT
        CASE p.PaymentMethod
            WHEN 1 THEN 'Cash'
            WHEN 2 THEN 'Card'
            WHEN 3 THEN 'Split'
            ELSE 'Unknown'
        END                    AS PaymentMethodName,
        COUNT(p.Id)            AS TransactionCount,
        SUM(p.Amount)          AS TotalAmount
    FROM Payments p
    INNER JOIN PosOrders o ON o.Id = p.OrderId
    WHERE CAST(o.PaidAt AS DATE) = @Date
      AND o.IsDeleted = 0
      AND p.IsDeleted = 0
      AND o.Status    = 2
    GROUP BY p.PaymentMethod
    ORDER BY p.PaymentMethod;

    -- Top 5 items for Z-Report
    SELECT TOP 5
        m.NameAr,
        m.NameEn,
        SUM(oi.Quantity)  AS TotalQty,
        SUM(oi.LineTotal) AS TotalRevenue
    FROM PosOrderItems oi
    INNER JOIN PosOrders  o ON o.Id  = oi.OrderId
    INNER JOIN MenuItems  m ON m.Id  = oi.MenuItemId
    WHERE CAST(o.PaidAt AS DATE) = @Date
      AND o.IsDeleted  = 0
      AND oi.IsDeleted = 0
      AND m.IsDeleted  = 0
      AND o.Status     = 2
    GROUP BY m.Id, m.NameAr, m.NameEn
    ORDER BY TotalRevenue DESC;
END
GO


-- ─────────────────────────────────────────────────────────────────────────────
-- sp_GetXReport
-- Current-shift snapshot (or a specific shift if @ShiftId is provided).
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('dbo.sp_GetXReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetXReport;
GO

CREATE PROCEDURE dbo.sp_GetXReport
    @ShiftId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME2, @To DATETIME2;

    IF @ShiftId IS NOT NULL
    BEGIN
        SELECT @From = PlannedStart, @To = ISNULL(ClockOut, SYSUTCDATETIME())
        FROM EmployeeShifts WHERE Id = @ShiftId AND IsDeleted = 0;
    END
    ELSE
    BEGIN
        -- Default: today
        SET @From = CAST(CAST(SYSUTCDATETIME() AS DATE) AS DATETIME2);
        SET @To   = SYSUTCDATETIME();
    END

    SELECT
        @ShiftId                                                AS ShiftId,
        @From                                                   AS PeriodFrom,
        @To                                                     AS PeriodTo,
        COUNT(DISTINCT o.Id)                                    AS TotalOrders,
        ISNULL(SUM(o.SubTotal),       0)                        AS SubTotal,
        ISNULL(SUM(o.TaxAmount),      0)                        AS TaxAmount,
        ISNULL(SUM(o.TotalAmount),    0)                        AS NetSales,
        ISNULL(SUM(CASE WHEN p.PaymentMethod = 1 THEN p.Amount ELSE 0 END), 0) AS CashTotal,
        ISNULL(SUM(CASE WHEN p.PaymentMethod = 2 THEN p.Amount ELSE 0 END), 0) AS CardTotal
    FROM PosOrders o
    LEFT JOIN Payments p ON p.OrderId = o.Id AND p.IsDeleted = 0
    WHERE o.CreatedAt BETWEEN @From AND @To
      AND o.IsDeleted = 0
      AND o.Status    = 2;

    -- Hourly breakdown
    SELECT
        DATEPART(HOUR, o.PaidAt)    AS [Hour],
        COUNT(o.Id)                 AS OrderCount,
        ISNULL(SUM(o.TotalAmount), 0) AS TotalSales
    FROM PosOrders o
    WHERE o.PaidAt BETWEEN @From AND @To
      AND o.IsDeleted = 0
      AND o.Status    = 2
    GROUP BY DATEPART(HOUR, o.PaidAt)
    ORDER BY [Hour];
END
GO


-- ─────────────────────────────────────────────────────────────────────────────
-- sp_CloseHandover
-- Closes a cash handover: calculates difference, sets appropriate status.
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('dbo.sp_CloseHandover', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CloseHandover;
GO

CREATE PROCEDURE dbo.sp_CloseHandover
    @HandoverId   INT,
    @ActualCash   DECIMAL(18,3),
    @ManagerId    INT,
    @Threshold    DECIMAL(18,3) = 5.000   -- difference threshold from appsettings
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Expected   DECIMAL(18,3);
    DECLARE @Diff       DECIMAL(18,3);
    DECLARE @NewStatus  INT;

    SELECT @Expected = OpenFloat + CashSalesTotal - CashRefundsTotal
    FROM CashHandovers
    WHERE Id = @HandoverId AND IsDeleted = 0;

    IF @Expected IS NULL
    BEGIN
        RAISERROR('Handover not found: %d', 16, 1, @HandoverId);
        RETURN;
    END

    SET @Diff = @ActualCash - @Expected;

    IF ABS(@Diff) > @Threshold
        SET @NewStatus = 4;   -- Flagged
    ELSE
        SET @NewStatus = 2;   -- PendingApproval

    UPDATE CashHandovers
    SET
        ActualCash  = @ActualCash,
        Difference  = @Diff,
        Status      = @NewStatus,
        ClosedAt    = SYSUTCDATETIME(),
        ManagerId   = @ManagerId,
        UpdatedAt   = SYSUTCDATETIME(),
        UpdatedBy   = @ManagerId
    WHERE Id = @HandoverId;

    -- Return the result
    SELECT Id, Status, Difference, ClosedAt FROM CashHandovers WHERE Id = @HandoverId;
END
GO


-- ─────────────────────────────────────────────────────────────────────────────
-- sp_GetBestSellingItems
-- Top N best-selling items by revenue within a date range.
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('dbo.sp_GetBestSellingItems', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBestSellingItems;
GO

CREATE PROCEDURE dbo.sp_GetBestSellingItems
    @FromDate DATE,
    @ToDate   DATE,
    @Top      INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
        m.Id              AS MenuItemId,
        m.NameAr,
        m.NameEn,
        SUM(oi.Quantity)  AS TotalQty,
        SUM(oi.LineTotal) AS TotalRevenue,
        COUNT(DISTINCT o.Id) AS OrderCount
    FROM PosOrderItems oi
    INNER JOIN PosOrders  o ON o.Id  = oi.OrderId
    INNER JOIN MenuItems  m ON m.Id  = oi.MenuItemId
    WHERE CAST(o.PaidAt AS DATE) BETWEEN @FromDate AND @ToDate
      AND o.IsDeleted  = 0
      AND oi.IsDeleted = 0
      AND m.IsDeleted  = 0
      AND o.Status     = 2
    GROUP BY m.Id, m.NameAr, m.NameEn
    ORDER BY TotalRevenue DESC;
END
GO


-- ─────────────────────────────────────────────────────────────────────────────
-- sp_GetHourlySales
-- Returns order count and sales total for each hour (0-23) on a given date.
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('dbo.sp_GetHourlySales', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetHourlySales;
GO

CREATE PROCEDURE dbo.sp_GetHourlySales
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Generate all 24 hours, left-join to actual sales
    WITH Hours AS (
        SELECT 0  AS Hr UNION ALL SELECT Hr + 1 FROM Hours WHERE Hr < 23
    )
    SELECT
        h.Hr                                        AS [Hour],
        ISNULL(s.OrderCount, 0)                     AS OrderCount,
        ISNULL(s.TotalSales,  0)                    AS TotalSales
    FROM Hours h
    LEFT JOIN (
        SELECT
            DATEPART(HOUR, o.PaidAt)    AS Hr,
            COUNT(o.Id)                 AS OrderCount,
            SUM(o.TotalAmount)          AS TotalSales
        FROM PosOrders o
        WHERE CAST(o.PaidAt AS DATE) = @Date
          AND o.IsDeleted = 0
          AND o.Status    = 2
        GROUP BY DATEPART(HOUR, o.PaidAt)
    ) s ON s.Hr = h.Hr
    ORDER BY h.Hr
    OPTION (MAXRECURSION 25);
END
GO

PRINT 'stored_procedures.sql: all procedures created/replaced.';
GO
