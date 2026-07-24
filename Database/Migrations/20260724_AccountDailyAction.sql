SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.AccountDailyAction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccountDailyAction
    (
        AccountId BIGINT NOT NULL,
        ActionKey NVARCHAR(64) NOT NULL,
        ActionDate DATE NOT NULL,
        CharacterId BIGINT NULL,
        CompletedAtUtc DATETIME2(3) NOT NULL,
        CONSTRAINT PK_AccountDailyAction
            PRIMARY KEY CLUSTERED (AccountId, ActionKey, ActionDate)
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.AccountDailyAction')
      AND name = N'IX_AccountDailyAction_CompletedAtUtc'
)
BEGIN
    CREATE INDEX IX_AccountDailyAction_CompletedAtUtc
        ON dbo.AccountDailyAction(CompletedAtUtc DESC)
        INCLUDE (AccountId, ActionKey, ActionDate, CharacterId);
END;

-- Preserve actions already completed before this migration so an account cannot
-- claim the same reward or refresh again on the deployment day.
IF OBJECT_ID(N'dbo.GeneralLog', N'U') IS NOT NULL
BEGIN
    ;WITH ExistingActions AS
    (
        SELECT
            AccountId,
            LogData AS ActionKey,
            CONVERT(DATE, [Timestamp]) AS ActionDate,
            MAX(CharacterId) AS CharacterId,
            MAX([Timestamp]) AS CompletedAt
        FROM dbo.GeneralLog
        WHERE AccountId IS NOT NULL
          AND LogData IN
          (
              N'DAILY_REWARD',
              N'PRIMALQUEST_REFRESH',
              N'DUELCOUNT_REFRESH',
              N'ICEFLOWER_REFRESH'
          )
        GROUP BY AccountId, LogData, CONVERT(DATE, [Timestamp])
    )
    INSERT INTO dbo.AccountDailyAction
    (
        AccountId,
        ActionKey,
        ActionDate,
        CharacterId,
        CompletedAtUtc
    )
    SELECT
        source.AccountId,
        source.ActionKey,
        source.ActionDate,
        source.CharacterId,
        CONVERT(DATETIME2(3), source.CompletedAt)
    FROM ExistingActions AS source
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.AccountDailyAction AS target
        WHERE target.AccountId = source.AccountId
          AND target.ActionKey = source.ActionKey
          AND target.ActionDate = source.ActionDate
    );
END;
