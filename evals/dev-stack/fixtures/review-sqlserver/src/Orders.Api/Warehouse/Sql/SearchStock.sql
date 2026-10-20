-- Stock search for the back office: SKU prefix and a sort column.
CREATE OR ALTER PROCEDURE dbo.SearchStock
    @SkuPrefix varchar(32),
    @SortColumn sysname = N'Sku'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @sql nvarchar(max) =
        N'SELECT TOP (100) Sku, Available FROM dbo.StockLevels WHERE Sku LIKE ''' + @SkuPrefix + N'%'' ORDER BY ' + @SortColumn;
    EXEC (@sql);
END;
