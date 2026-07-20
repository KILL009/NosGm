SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.TradeOperation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TradeOperation
    (
        OperationId uniqueidentifier NOT NULL,
        FirstCharacterId bigint NOT NULL,
        SecondCharacterId bigint NOT NULL,
        FirstGoldBefore bigint NOT NULL,
        FirstGoldAfter bigint NOT NULL,
        FirstGoldBankBefore bigint NOT NULL,
        FirstGoldBankAfter bigint NOT NULL,
        SecondGoldBefore bigint NOT NULL,
        SecondGoldAfter bigint NOT NULL,
        SecondGoldBankBefore bigint NOT NULL,
        SecondGoldBankAfter bigint NOT NULL,
        AffectedItemCount int NOT NULL,
        CompletedAtUtc datetime2(3) NOT NULL,
        CONSTRAINT PK_TradeOperation PRIMARY KEY CLUSTERED (OperationId)
    );

    CREATE INDEX IX_TradeOperation_Characters_CompletedAtUtc
        ON dbo.TradeOperation (FirstCharacterId, SecondCharacterId, CompletedAtUtc DESC);

    CREATE INDEX IX_TradeOperation_CompletedAtUtc
        ON dbo.TradeOperation (CompletedAtUtc DESC);
END;

-- No foreign keys are used intentionally. The audit row must survive character
-- deletion and remain useful during economy investigations.
