-- A picker takes stock from the shelf. Returns 0 when done, 1 when the stock is too low.
CREATE OR ALTER PROCEDURE dbo.PickStock
    @Sku varchar(32),
    @Quantity int
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.StockLevels
    SET Available = Available - @Quantity, UpdatedAt = SYSDATETIMEOFFSET()
    WHERE Sku = @Sku AND Available >= @Quantity;

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK TRANSACTION;
        RETURN 1;
    END;

    INSERT dbo.StockAudit (Sku, Change, Reason) VALUES (@Sku, -@Quantity, N'pick');

    COMMIT TRANSACTION;
    RETURN 0;
END;
