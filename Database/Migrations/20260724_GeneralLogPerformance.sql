SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.GeneralLog', N'U') IS NULL
BEGIN
    THROW 50001, 'dbo.GeneralLog does not exist. Apply the base database migrations first.', 1;
END;

-- LogType was originally created as nvarchar(max), which cannot be used as an
-- index key. Refuse to truncate unexpected values silently.
IF EXISTS
(
    SELECT 1
    FROM dbo.GeneralLog
    WHERE LEN(LogType) > 64
)
BEGIN
    THROW 50002, 'GeneralLog contains LogType values longer than 64 characters. Clean those rows before applying this migration.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.GeneralLog')
      AND name = N'LogType'
      AND (max_length = -1 OR max_length > 128)
)
BEGIN
    ALTER TABLE dbo.GeneralLog ALTER COLUMN LogType NVARCHAR(64) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GeneralLog')
      AND name = N'IX_GeneralLog_Account_LogData_Timestamp'
)
BEGIN
    CREATE INDEX IX_GeneralLog_Account_LogData_Timestamp
        ON dbo.GeneralLog(AccountId, LogData, Timestamp DESC, LogId DESC)
        INCLUDE (CharacterId, LogType, IpAddress);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GeneralLog')
      AND name = N'IX_GeneralLog_Account_LogType_Timestamp'
)
BEGIN
    CREATE INDEX IX_GeneralLog_Account_LogType_Timestamp
        ON dbo.GeneralLog(AccountId, LogType, Timestamp DESC, LogId DESC)
        INCLUDE (CharacterId, LogData, IpAddress);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GeneralLog')
      AND name = N'IX_GeneralLog_LogType_Character_Timestamp'
)
BEGIN
    CREATE INDEX IX_GeneralLog_LogType_Character_Timestamp
        ON dbo.GeneralLog(LogType, CharacterId, Timestamp DESC, LogId DESC)
        INCLUDE (AccountId, LogData, IpAddress);
END;
