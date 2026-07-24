SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
    NosGM GeneralLog load benchmark

    Safety rules:
    - Run only against a development or copied database.
    - Keep @Mode = N'CREATE' to generate rows.
    - Change @Mode to N'CLEANUP' and paste the printed RunTag to remove them.
*/

DECLARE @Mode NVARCHAR(10) = N'CREATE';
DECLARE @AccountId BIGINT = 10003;
DECLARE @Rows INT = 50000;
DECLARE @RunTag NVARCHAR(100) = NULL;

IF OBJECT_ID(N'dbo.GeneralLog', N'U') IS NULL
BEGIN
    THROW 50001, 'dbo.GeneralLog does not exist.', 1;
END;

IF @Mode = N'CREATE'
BEGIN
    IF @Rows < 1 OR @Rows > 1000000
    BEGIN
        THROW 50002, '@Rows must be between 1 and 1,000,000.', 1;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Account WHERE AccountId = @AccountId)
    BEGIN
        THROW 50003, 'The selected AccountId does not exist.', 1;
    END;

    SET @RunTag = N'NOSGM_BENCH_' + CONVERT(NVARCHAR(36), NEWID());

    DECLARE @StartedAt DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Now DATETIME = GETDATE();

    ;WITH Numbers AS
    (
        SELECT TOP (@Rows)
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Number
        FROM sys.all_objects AS A
        CROSS JOIN sys.all_objects AS B
    )
    INSERT INTO dbo.GeneralLog
    (
        AccountId,
        CharacterId,
        IpAddress,
        LogData,
        LogType,
        [Timestamp]
    )
    SELECT
        @AccountId,
        NULL,
        N'127.0.0.1',
        @RunTag + N'_' + CONVERT(NVARCHAR(20), Number),
        N'Benchmark',
        DATEADD(SECOND, -Number, @Now)
    FROM Numbers;

    DECLARE @CompletedAt DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        @AccountId AS AccountId,
        @RunTag AS RunTag,
        COUNT_BIG(*) AS GeneratedRows,
        DATEDIFF(MILLISECOND, @StartedAt, @CompletedAt) AS InsertMilliseconds
    FROM dbo.GeneralLog
    WHERE AccountId = @AccountId
      AND LogType = N'Benchmark'
      AND LogData LIKE @RunTag + N'_%';

    PRINT N'To remove this benchmark, set @Mode = N''CLEANUP'' and @RunTag = N''' +
          @RunTag + N'''.';
END
ELSE IF @Mode = N'CLEANUP'
BEGIN
    IF @RunTag IS NULL OR @RunTag NOT LIKE N'NOSGM_BENCH_%'
    BEGIN
        THROW 50004, 'Set @RunTag to the exact NOSGM_BENCH value printed during CREATE.', 1;
    END;

    DELETE FROM dbo.GeneralLog
    WHERE AccountId = @AccountId
      AND LogType = N'Benchmark'
      AND LogData LIKE @RunTag + N'_%';

    SELECT @@ROWCOUNT AS DeletedRows, @RunTag AS RunTag;
END
ELSE
BEGIN
    THROW 50005, '@Mode must be CREATE or CLEANUP.', 1;
END;
