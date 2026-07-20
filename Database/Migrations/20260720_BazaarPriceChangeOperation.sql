SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.BazaarPriceChangeOperation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BazaarPriceChangeOperation
    (
        OperationId uniqueidentifier NOT NULL,
        BazaarItemId bigint NOT NULL,
        SellerAccountId bigint NOT NULL,
        SellerCharacterId bigint NOT NULL,
        BazaarItemInstanceId uniqueidentifier NOT NULL,
        ItemVNum smallint NOT NULL,
        Amount smallint NOT NULL,
        OldUnitPrice bigint NOT NULL,
        NewUnitPrice bigint NOT NULL,
        CompletedAtUtc datetime2(3) NOT NULL,
        CONSTRAINT PK_BazaarPriceChangeOperation PRIMARY KEY CLUSTERED (OperationId)
    );

    CREATE INDEX IX_BazaarPriceChangeOperation_BazaarItem_CompletedAtUtc
        ON dbo.BazaarPriceChangeOperation (BazaarItemId, CompletedAtUtc DESC);

    CREATE INDEX IX_BazaarPriceChangeOperation_Seller_CompletedAtUtc
        ON dbo.BazaarPriceChangeOperation (SellerCharacterId, CompletedAtUtc DESC);
END;
