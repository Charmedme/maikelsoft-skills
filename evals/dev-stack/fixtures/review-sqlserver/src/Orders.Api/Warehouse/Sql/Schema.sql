-- Warehouse database: stock tables. The warehouse DBA team deploys this script.

CREATE TABLE dbo.StockLevels (
    Sku varchar(32) NOT NULL CONSTRAINT PK_StockLevels PRIMARY KEY,
    Available int NOT NULL,
    UpdatedAt datetimeoffset(7) NOT NULL
);

CREATE TABLE dbo.StockAudit (
    Id uniqueidentifier NOT NULL CONSTRAINT DF_StockAudit_Id DEFAULT NEWID(),
    Sku varchar(32) NOT NULL,
    Change int NOT NULL,
    Reason nvarchar(200) NOT NULL,
    ChangedAt datetime NOT NULL CONSTRAINT DF_StockAudit_ChangedAt DEFAULT GETDATE(),
    CONSTRAINT PK_StockAudit PRIMARY KEY CLUSTERED (Id)
);
