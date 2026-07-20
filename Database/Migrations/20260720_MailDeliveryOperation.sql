/*
    NosGM idempotent mail/reward delivery ledger.

    Safe to run more than once. This table intentionally has no foreign key to dbo.Mail:
    the operation must survive after the parcel is claimed or deleted so a retried purchase,
    reward, or RPC call cannot recreate it.
*/
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.MailDeliveryOperation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MailDeliveryOperation
    (
        OperationId uniqueidentifier NOT NULL,
        IsSenderCopy bit NOT NULL,
        MailId bigint NULL,
        DeliverySource int NOT NULL,
        ReceiverId bigint NOT NULL,
        CreatedAtUtc datetime2(3) NOT NULL
            CONSTRAINT DF_MailDeliveryOperation_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        ClaimedAtUtc datetime2(3) NULL,
        ClaimItemInstanceId uniqueidentifier NULL,
        CONSTRAINT PK_MailDeliveryOperation
            PRIMARY KEY CLUSTERED (OperationId, IsSenderCopy)
    );
END;

IF NOT EXISTS
(
    SELECT 1
      FROM sys.indexes
     WHERE object_id = OBJECT_ID(N'dbo.MailDeliveryOperation')
       AND name = N'UX_MailDeliveryOperation_MailId'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_MailDeliveryOperation_MailId
        ON dbo.MailDeliveryOperation (MailId)
        WHERE MailId IS NOT NULL;
END;

IF NOT EXISTS
(
    SELECT 1
      FROM sys.indexes
     WHERE object_id = OBJECT_ID(N'dbo.MailDeliveryOperation')
       AND name = N'IX_MailDeliveryOperation_Receiver_Created'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MailDeliveryOperation_Receiver_Created
        ON dbo.MailDeliveryOperation (ReceiverId, CreatedAtUtc DESC)
        INCLUDE (OperationId, IsSenderCopy, MailId, DeliverySource, ClaimedAtUtc, ClaimItemInstanceId);
END;
