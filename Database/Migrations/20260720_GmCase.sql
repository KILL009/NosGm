SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.GmCase', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GmCase
    (
        CaseId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_GmCase PRIMARY KEY,
        CorrelationId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        UpdatedAtUtc DATETIME2(3) NOT NULL,
        ClosedAtUtc DATETIME2(3) NULL,
        Status TINYINT NOT NULL,
        Priority TINYINT NOT NULL,
        SubjectType TINYINT NOT NULL,
        SubjectAccountId BIGINT NOT NULL,
        SubjectCharacterId BIGINT NULL,
        SubjectName NVARCHAR(64) NULL,
        Title NVARCHAR(160) NOT NULL,
        Summary NVARCHAR(2000) NULL,
        CreatedByAccountId BIGINT NOT NULL,
        CreatedByCharacterId BIGINT NULL,
        CreatedByName NVARCHAR(64) NULL,
        AssignedAccountId BIGINT NULL,
        AssignedCharacterId BIGINT NULL,
        AssignedName NVARCHAR(64) NULL,
        CONSTRAINT CK_GmCase_Status CHECK (Status BETWEEN 1 AND 5),
        CONSTRAINT CK_GmCase_Priority CHECK (Priority BETWEEN 1 AND 4),
        CONSTRAINT CK_GmCase_SubjectType CHECK (SubjectType BETWEEN 1 AND 2)
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmCase')
      AND name = N'UX_GmCase_CorrelationId'
)
BEGIN
    CREATE UNIQUE INDEX UX_GmCase_CorrelationId
        ON dbo.GmCase(CorrelationId);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmCase')
      AND name = N'IX_GmCase_StatusPriorityUpdated'
)
BEGIN
    CREATE INDEX IX_GmCase_StatusPriorityUpdated
        ON dbo.GmCase(Status, Priority DESC, UpdatedAtUtc DESC)
        INCLUDE (SubjectAccountId, SubjectCharacterId, AssignedAccountId, SubjectName, Title);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmCase')
      AND name = N'IX_GmCase_Subject'
)
BEGIN
    CREATE INDEX IX_GmCase_Subject
        ON dbo.GmCase(SubjectAccountId, SubjectCharacterId, UpdatedAtUtc DESC);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmCase')
      AND name = N'IX_GmCase_Assigned'
)
BEGIN
    CREATE INDEX IX_GmCase_Assigned
        ON dbo.GmCase(AssignedAccountId, Status, UpdatedAtUtc DESC);
END;

IF OBJECT_ID(N'dbo.GmCaseNote', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GmCaseNote
    (
        NoteId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_GmCaseNote PRIMARY KEY,
        CaseId BIGINT NOT NULL,
        OccurredAtUtc DATETIME2(3) NOT NULL,
        NoteType TINYINT NOT NULL,
        AuthorAccountId BIGINT NOT NULL,
        AuthorCharacterId BIGINT NULL,
        AuthorName NVARCHAR(64) NULL,
        [Text] NVARCHAR(2000) NOT NULL,
        [Reference] NVARCHAR(500) NULL,
        Metadata NVARCHAR(2000) NULL,
        CONSTRAINT FK_GmCaseNote_GmCase
            FOREIGN KEY (CaseId) REFERENCES dbo.GmCase(CaseId),
        CONSTRAINT CK_GmCaseNote_NoteType CHECK (NoteType BETWEEN 1 AND 6)
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmCaseNote')
      AND name = N'IX_GmCaseNote_CaseOccurred'
)
BEGIN
    CREATE INDEX IX_GmCaseNote_CaseOccurred
        ON dbo.GmCaseNote(CaseId, OccurredAtUtc DESC, NoteId DESC)
        INCLUDE (NoteType, AuthorAccountId, AuthorCharacterId, AuthorName);
END;

COMMIT TRANSACTION;