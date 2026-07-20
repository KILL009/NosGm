SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.GmCommandAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GmCommandAudit
    (
        AuditId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_GmCommandAudit PRIMARY KEY,
        CorrelationId UNIQUEIDENTIFIER NOT NULL,
        OccurredAtUtc DATETIME2(3) NOT NULL,
        AccountId BIGINT NULL,
        CharacterId BIGINT NULL,
        CharacterName NVARCHAR(64) NULL,
        Authority SMALLINT NOT NULL,
        CommandHeader NVARCHAR(64) NOT NULL,
        CommandText NVARCHAR(1000) NULL,
        RequiredAuthority SMALLINT NOT NULL,
        Outcome TINYINT NOT NULL,
        IpAddress NVARCHAR(64) NULL,
        ChannelId INT NOT NULL,
        MapId SMALLINT NULL,
        SessionId INT NULL,
        Failure NVARCHAR(2000) NULL
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmCommandAudit')
      AND name = N'UX_GmCommandAudit_CorrelationId'
)
BEGIN
    CREATE UNIQUE INDEX UX_GmCommandAudit_CorrelationId
        ON dbo.GmCommandAudit(CorrelationId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmCommandAudit')
      AND name = N'IX_GmCommandAudit_OccurredAtUtc'
)
BEGIN
    CREATE INDEX IX_GmCommandAudit_OccurredAtUtc
        ON dbo.GmCommandAudit(OccurredAtUtc DESC, AuditId DESC);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmCommandAudit')
      AND name = N'IX_GmCommandAudit_AccountId'
)
BEGIN
    CREATE INDEX IX_GmCommandAudit_AccountId
        ON dbo.GmCommandAudit(AccountId, OccurredAtUtc DESC)
        INCLUDE (CharacterId, CharacterName, CommandHeader, Outcome, ChannelId, MapId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmCommandAudit')
      AND name = N'IX_GmCommandAudit_CharacterId'
)
BEGIN
    CREATE INDEX IX_GmCommandAudit_CharacterId
        ON dbo.GmCommandAudit(CharacterId, OccurredAtUtc DESC)
        INCLUDE (AccountId, CharacterName, CommandHeader, Outcome, ChannelId, MapId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmCommandAudit')
      AND name = N'IX_GmCommandAudit_CommandHeader'
)
BEGIN
    CREATE INDEX IX_GmCommandAudit_CommandHeader
        ON dbo.GmCommandAudit(CommandHeader, OccurredAtUtc DESC)
        INCLUDE (AccountId, CharacterId, CharacterName, Outcome, RequiredAuthority);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmCommandAudit')
      AND name = N'IX_GmCommandAudit_Outcome'
)
BEGIN
    CREATE INDEX IX_GmCommandAudit_Outcome
        ON dbo.GmCommandAudit(Outcome, OccurredAtUtc DESC)
        INCLUDE (AccountId, CharacterId, CharacterName, CommandHeader, Failure);
END;

-- Intentionally no foreign keys. Staff audit history must survive account or
-- character deletion and remain available during incident investigation.
