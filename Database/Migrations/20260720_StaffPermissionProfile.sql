SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.StaffPermissionProfile', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StaffPermissionProfile
    (
        AccountId bigint NOT NULL,
        PermissionMask bigint NOT NULL CONSTRAINT DF_StaffPermissionProfile_PermissionMask DEFAULT (0),
        IsEnabled bit NOT NULL CONSTRAINT DF_StaffPermissionProfile_IsEnabled DEFAULT (0),
        UpdatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_StaffPermissionProfile_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedByAccountId bigint NULL,
        UpdatedByCharacterId bigint NULL,
        Reason nvarchar(500) NULL,
        CONSTRAINT PK_StaffPermissionProfile PRIMARY KEY CLUSTERED (AccountId),
        CONSTRAINT CK_StaffPermissionProfile_PermissionMask CHECK (PermissionMask >= 0 AND PermissionMask <= 127)
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.StaffPermissionProfile')
      AND name = N'IX_StaffPermissionProfile_Enabled'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_StaffPermissionProfile_Enabled
        ON dbo.StaffPermissionProfile (IsEnabled, UpdatedAtUtc DESC)
        INCLUDE (PermissionMask, UpdatedByAccountId, UpdatedByCharacterId);
END;

PRINT N'StaffPermissionProfile migration completed.';
