-- Called by the stock import of the warehouse. Two imports can run at the same time.
CREATE OR ALTER PROCEDURE dbo.UpsertStock
    @Sku varchar(32),
    @Available int
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.StockLevels AS t
    USING (VALUES (@Sku, @Available)) AS s (Sku, Available)
        ON t.Sku = s.Sku
    WHEN MATCHED THEN
        UPDATE SET Available = s.Available, UpdatedAt = SYSDATETIMEOFFSET()
    WHEN NOT MATCHED THEN
        INSERT (Sku, Available, UpdatedAt) VALUES (s.Sku, s.Available, SYSDATETIMEOFFSET());
END;
