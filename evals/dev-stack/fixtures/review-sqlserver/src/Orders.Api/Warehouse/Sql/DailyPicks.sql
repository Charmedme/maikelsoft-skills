-- Picks per day in Amsterdam time. Runs during the day, while orders reserve stock.
CREATE OR ALTER PROCEDURE dbo.DailyPicks
    @From date,
    @To date
AS
BEGIN
    SET NOCOUNT ON;

    -- NOLOCK: the report must not block the reservations.
    SELECT CAST(a.ChangedAt AT TIME ZONE 'Europe/Amsterdam' AS date) AS Day,
           COUNT(*) AS Picks,
           SUM(-a.Change) AS Units
    FROM dbo.StockAudit AS a WITH (NOLOCK)
    WHERE a.Reason = N'pick'
      AND a.ChangedAt >= @From
      AND a.ChangedAt < DATEADD(day, 1, CAST(@To AS datetime))
    GROUP BY CAST(a.ChangedAt AT TIME ZONE 'Europe/Amsterdam' AS date)
    ORDER BY Day;
END;
