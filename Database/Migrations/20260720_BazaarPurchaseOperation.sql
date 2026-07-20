SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.BazaarPurchaseOperation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BazaarPurchaseOperation
    (
        OperationId uniqueidentifier NOT NULL,
        BazaarItemId bigint NOT NULL,
        BuyerAccountId bigint NOT NULL,
        BuyerCharacterId bigint NOT NULL,
        SellerCharacterId bigint NOT NULL,
        BazaarItemInstanceId uniqueidentifier NOT NULL,
        ItemVNum smallint NOT NULL,
        Amount smallint NOT NULL,
        UnitPrice bigint NOT NULL,
        GoldBefore bigint NOT NULL,
        GoldAfter bigint NOT NULL,
        GoldBankBefore bigint NOT NULL,
        GoldBankAfter bigint NOT NULL,
        BazaarAmountBefore smallint NOT NULL,
        BazaarAmountAfter smallint NOT NULL,
        CreatedItemCount int NOT NULL,
        CompletedAtUtc datetime2(3) NOT NULL,
        CONSTRAINT PK_BazaarPurchaseOperation PRIMARY KEY CLUSTERED (OperationId)
    );

    CREATE INDEX IX_BazaarPurchaseOperation_BazaarItem_CompletedAtUtc
        ON dbo.BazaarPurchaseOperation (BazaarItemId, CompletedAtUtc DESC);

    CREATE INDEX IX_BazaarPurchaseOperation_Buyer_CompletedAtUtc
        ON dbo.BazaarPurchaseOperation (BuyerCharacterId, CompletedAtUtc DESC);
END;
