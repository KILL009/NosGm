SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.BazaarRecollectOperation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BazaarRecollectOperation
    (
        OperationId uniqueidentifier NOT NULL,
        BazaarItemId bigint NOT NULL,
        SellerCharacterId bigint NOT NULL,
        BazaarItemInstanceId uniqueidentifier NOT NULL,
        ItemVNum smallint NOT NULL,
        ListingAmount smallint NOT NULL,
        RemainingAmount smallint NOT NULL,
        SoldAmount smallint NOT NULL,
        UnitPrice bigint NOT NULL,
        Tax bigint NOT NULL,
        Proceeds bigint NOT NULL,
        GoldBefore bigint NOT NULL,
        GoldAfter bigint NOT NULL,
        CompletedAtUtc datetime2(3) NOT NULL,
        CONSTRAINT PK_BazaarRecollectOperation PRIMARY KEY CLUSTERED (OperationId)
    );

    CREATE INDEX IX_BazaarRecollectOperation_BazaarItem_CompletedAtUtc
        ON dbo.BazaarRecollectOperation (BazaarItemId, CompletedAtUtc DESC);

    CREATE INDEX IX_BazaarRecollectOperation_Seller_CompletedAtUtc
        ON dbo.BazaarRecollectOperation (SellerCharacterId, CompletedAtUtc DESC);
END;
