SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.BazaarListingOperation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BazaarListingOperation
    (
        OperationId uniqueidentifier NOT NULL,
        BazaarItemId bigint NOT NULL,
        SellerAccountId bigint NOT NULL,
        SellerCharacterId bigint NOT NULL,
        SourceItemInstanceId uniqueidentifier NOT NULL,
        BazaarItemInstanceId uniqueidentifier NOT NULL,
        ItemVNum smallint NOT NULL,
        AmountBefore smallint NOT NULL,
        ListedAmount smallint NOT NULL,
        AmountAfter smallint NOT NULL,
        UnitPrice bigint NOT NULL,
        Tax bigint NOT NULL,
        GoldBefore bigint NOT NULL,
        GoldAfter bigint NOT NULL,
        FullTransfer bit NOT NULL,
        CompletedAtUtc datetime2(3) NOT NULL,
        CONSTRAINT PK_BazaarListingOperation PRIMARY KEY CLUSTERED (OperationId)
    );

    CREATE UNIQUE INDEX UX_BazaarListingOperation_BazaarItem
        ON dbo.BazaarListingOperation (BazaarItemId);

    CREATE INDEX IX_BazaarListingOperation_Seller_CompletedAtUtc
        ON dbo.BazaarListingOperation (SellerCharacterId, CompletedAtUtc DESC);

    CREATE INDEX IX_BazaarListingOperation_SourceItem
        ON dbo.BazaarListingOperation (SourceItemInstanceId, CompletedAtUtc DESC);
END;
