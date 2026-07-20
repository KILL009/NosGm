SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.ItemTrace', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM dbo.ItemTrace
        WHERE InventoryTypeBefore NOT BETWEEN 0 AND 255
           OR InventoryTypeAfter NOT BETWEEN 0 AND 255
    )
    BEGIN
        THROW 51000, 'ItemTrace contains an inventory type outside the tinyint range.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.ItemTrace')
          AND name = N'InventoryTypeBefore'
          AND system_type_id <> TYPE_ID(N'tinyint')
    )
        ALTER TABLE dbo.ItemTrace ALTER COLUMN InventoryTypeBefore tinyint NULL;

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.ItemTrace')
          AND name = N'InventoryTypeAfter'
          AND system_type_id <> TYPE_ID(N'tinyint')
    )
        ALTER TABLE dbo.ItemTrace ALTER COLUMN InventoryTypeAfter tinyint NULL;
END;

COMMIT TRANSACTION;
