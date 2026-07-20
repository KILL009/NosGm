SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.ItemTrace', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItemTrace
    (
        Id uniqueidentifier NOT NULL,
        OperationId uniqueidentifier NOT NULL,
        Sequence int NOT NULL,
        OccurredAtUtc datetime2(3) NOT NULL,
        Action int NOT NULL,
        Source int NOT NULL,
        ItemInstanceId uniqueidentifier NOT NULL,
        EquipmentSerialId uniqueidentifier NULL,
        ItemVNum smallint NOT NULL,
        AmountBefore int NULL,
        AmountAfter int NULL,
        OwnerCharacterIdBefore bigint NULL,
        OwnerCharacterIdAfter bigint NULL,
        InventoryTypeBefore tinyint NULL,
        InventoryTypeAfter tinyint NULL,
        SlotBefore smallint NULL,
        SlotAfter smallint NULL,
        ActorAccountId bigint NULL,
        ActorCharacterId bigint NULL,
        ActorName nvarchar(64) NULL,
        Reason nvarchar(500) NULL,
        Metadata nvarchar(4000) NULL,
        IsSuspicious bit NOT NULL CONSTRAINT DF_ItemTrace_IsSuspicious DEFAULT (0),
        CONSTRAINT PK_ItemTrace PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UX_ItemTrace_OperationSequence UNIQUE NONCLUSTERED (OperationId, Sequence)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ItemTrace') AND name = N'IX_ItemTrace_ItemInstanceId')
    CREATE NONCLUSTERED INDEX IX_ItemTrace_ItemInstanceId
        ON dbo.ItemTrace (ItemInstanceId, OccurredAtUtc DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ItemTrace') AND name = N'IX_ItemTrace_EquipmentSerialId')
    CREATE NONCLUSTERED INDEX IX_ItemTrace_EquipmentSerialId
        ON dbo.ItemTrace (EquipmentSerialId, OccurredAtUtc DESC)
        WHERE EquipmentSerialId IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ItemTrace') AND name = N'IX_ItemTrace_Suspicious')
    CREATE NONCLUSTERED INDEX IX_ItemTrace_Suspicious
        ON dbo.ItemTrace (OccurredAtUtc DESC)
        WHERE IsSuspicious = 1;

COMMIT TRANSACTION;

-- No foreign key intentionally points to ItemInstance. The ledger must survive
-- item deletion so administrators can reconstruct an object's complete history.
