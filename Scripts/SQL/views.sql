-- ═══════════════════════════════════════════════════════════════════════════
-- RestaurantMS  — SQL Views
-- Run AFTER dev_migration_v1.sql
-- ═══════════════════════════════════════════════════════════════════════════

SET NOCOUNT ON;
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- vw_HandoverSummary
-- Handovers with cashier name, manager name, formatted difference.
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('dbo.vw_HandoverSummary', 'V') IS NOT NULL
    DROP VIEW dbo.vw_HandoverSummary;
GO

CREATE VIEW dbo.vw_HandoverSummary AS
SELECT
    h.Id,
    h.OpenedAt,
    h.ClosedAt,
    h.ManagerApprovedAt,
    -- Cashier
    c.Id              AS CashierId,
    c.FullNameEn      AS CashierNameEn,
    c.FullNameAr      AS CashierNameAr,
    -- Manager
    m.Id              AS ManagerId,
    m.FullNameEn      AS ManagerNameEn,
    m.FullNameAr      AS ManagerNameAr,
    -- Amounts
    h.OpenFloat,
    h.CashSalesTotal,
    h.CashRefundsTotal,
    h.OpenFloat + h.CashSalesTotal - h.CashRefundsTotal AS ExpectedCash,
    h.ActualCash,
    h.Difference,
    -- Status label
    h.Status,
    CASE h.Status
        WHEN 1 THEN N'مفتوح / Open'
        WHEN 2 THEN N'انتظار اعتماد / Pending'
        WHEN 3 THEN N'مغلق / Closed'
        WHEN 4 THEN N'فرق كبير / Flagged'
        ELSE        N'غير معروف / Unknown'
    END               AS StatusLabel,
    -- Formatted difference (+/-)
    CASE
        WHEN h.Difference IS NULL THEN N'—'
        WHEN h.Difference >= 0   THEN N'+' + CAST(ABS(h.Difference) AS NVARCHAR(20))
        ELSE                          N'-' + CAST(ABS(h.Difference) AS NVARCHAR(20))
    END               AS DifferenceFormatted,
    h.CashierNotes,
    h.ManagerNotes
FROM CashHandovers h
INNER JOIN AspNetUsers c ON c.Id = h.CashierId
LEFT  JOIN AspNetUsers m ON m.Id = h.ManagerId
WHERE h.IsDeleted = 0;
GO


-- ─────────────────────────────────────────────────────────────────────────────
-- vw_DailySalesSummary
-- One row per day: all payment methods, tax, discounts, voided count.
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('dbo.vw_DailySalesSummary', 'V') IS NOT NULL
    DROP VIEW dbo.vw_DailySalesSummary;
GO

CREATE VIEW dbo.vw_DailySalesSummary AS
SELECT
    CAST(o.PaidAt AS DATE)                              AS SaleDate,
    COUNT(DISTINCT o.Id)                                AS TotalOrders,
    SUM(o.SubTotal)                                     AS SubTotal,
    SUM(o.TaxAmount)                                    AS TaxAmount,
    SUM(o.DiscountAmount)                               AS DiscountAmount,
    SUM(o.TotalAmount)                                  AS NetSales,
    SUM(CASE WHEN p.PaymentMethod = 1 THEN p.Amount ELSE 0 END) AS CashSales,
    SUM(CASE WHEN p.PaymentMethod = 2 THEN p.Amount ELSE 0 END) AS CardSales,
    SUM(CASE WHEN p.PaymentMethod = 3 THEN p.Amount ELSE 0 END) AS SplitSales,
    CASE WHEN COUNT(DISTINCT o.Id) > 0
         THEN SUM(o.TotalAmount) / COUNT(DISTINCT o.Id)
         ELSE 0 END                                     AS AvgOrderValue
FROM PosOrders o
LEFT JOIN Payments p ON p.OrderId = o.Id AND p.IsDeleted = 0
WHERE o.IsDeleted = 0
  AND o.Status    = 2       -- Paid only
  AND o.PaidAt    IS NOT NULL
GROUP BY CAST(o.PaidAt AS DATE);
GO


-- ─────────────────────────────────────────────────────────────────────────────
-- vw_BestSellingItems
-- All-time best sellers ranked by total revenue.
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('dbo.vw_BestSellingItems', 'V') IS NOT NULL
    DROP VIEW dbo.vw_BestSellingItems;
GO

CREATE VIEW dbo.vw_BestSellingItems AS
SELECT
    m.Id              AS MenuItemId,
    m.NameAr,
    m.NameEn,
    c.NameAr          AS CategoryNameAr,
    c.NameEn          AS CategoryNameEn,
    SUM(oi.Quantity)  AS TotalQuantity,
    SUM(oi.LineTotal) AS TotalRevenue,
    COUNT(DISTINCT o.Id) AS OrderCount,
    -- Rank by revenue
    RANK() OVER (ORDER BY SUM(oi.LineTotal) DESC) AS RevenueRank
FROM PosOrderItems oi
INNER JOIN PosOrders  o ON o.Id  = oi.OrderId
INNER JOIN MenuItems  m ON m.Id  = oi.MenuItemId
INNER JOIN Categories c ON c.Id  = m.CategoryId
WHERE o.IsDeleted  = 0
  AND oi.IsDeleted = 0
  AND m.IsDeleted  = 0
  AND c.IsDeleted  = 0
  AND o.Status     = 2      -- Paid
GROUP BY m.Id, m.NameAr, m.NameEn, c.NameAr, c.NameEn;
GO


-- ─────────────────────────────────────────────────────────────────────────────
-- vw_ShiftPerformance
-- Shift summary: hours worked, overtime, sales during shift.
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('dbo.vw_ShiftPerformance', 'V') IS NOT NULL
    DROP VIEW dbo.vw_ShiftPerformance;
GO

CREATE VIEW dbo.vw_ShiftPerformance AS
SELECT
    es.Id             AS ShiftId,
    es.ShiftDate,
    -- Employee
    u.Id              AS EmployeeId,
    u.FullNameEn      AS EmployeeNameEn,
    u.FullNameAr      AS EmployeeNameAr,
    -- Template
    st.NameEn         AS TemplateNameEn,
    st.NameAr         AS TemplateNameAr,
    -- Times
    es.PlannedStart,
    es.PlannedEnd,
    es.ClockIn,
    es.ClockOut,
    -- Hours worked (minutes)
    CASE WHEN es.ClockIn IS NOT NULL AND es.ClockOut IS NOT NULL
         THEN DATEDIFF(MINUTE, es.ClockIn, es.ClockOut)
         ELSE NULL
    END               AS MinutesWorked,
    es.OvertimeMinutes,
    -- Shift status
    es.Status,
    CASE es.Status
        WHEN 1 THEN 'Scheduled'
        WHEN 2 THEN 'Active'
        WHEN 3 THEN 'Completed'
        WHEN 4 THEN 'Absent'
    END               AS StatusLabel,
    -- Sales attributed to this shift (orders created between clockin/clockout)
    ISNULL(s.ShiftOrderCount, 0)  AS ShiftOrderCount,
    ISNULL(s.ShiftSalesTotal, 0)  AS ShiftSalesTotal
FROM EmployeeShifts es
INNER JOIN AspNetUsers   u  ON u.Id  = es.EmployeeId
INNER JOIN ShiftTemplates st ON st.Id = es.TemplateId
LEFT JOIN (
    SELECT
        o.ShiftId,
        COUNT(o.Id)      AS ShiftOrderCount,
        SUM(o.TotalAmount) AS ShiftSalesTotal
    FROM PosOrders o
    WHERE o.IsDeleted = 0
      AND o.Status    = 2
      AND o.ShiftId   IS NOT NULL
    GROUP BY o.ShiftId
) s ON s.ShiftId = es.Id
WHERE es.IsDeleted = 0;
GO

PRINT 'views.sql: all views created/replaced.';
GO
