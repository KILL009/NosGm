SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.GmCase', N'U') IS NULL OR OBJECT_ID(N'dbo.GmCaseNote', N'U') IS NULL
BEGIN
    THROW 51000, 'Apply 20260720_GmCase.sql before the GM sanction migration.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.GmCaseNote')
      AND name = N'CK_GmCaseNote_NoteType'
)
BEGIN
    ALTER TABLE dbo.GmCaseNote DROP CONSTRAINT CK_GmCaseNote_NoteType;
END;

ALTER TABLE dbo.GmCaseNote WITH CHECK
ADD CONSTRAINT CK_GmCaseNote_NoteType CHECK (NoteType BETWEEN 1 AND 8);

IF OBJECT_ID(N'dbo.GmSanctionAction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GmSanctionAction
    (
        ActionId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_GmSanctionAction PRIMARY KEY,
        OperationId UNIQUEIDENTIFIER NOT NULL,
        CaseId BIGINT NOT NULL,
        OccurredAtUtc DATETIME2(3) NOT NULL,
        ActionType TINYINT NOT NULL,
        PenaltyLogId INT NULL,
        AffectedPenaltyCount INT NOT NULL,
        SubjectAccountId BIGINT NOT NULL,
        SubjectCharacterId BIGINT NULL,
        SubjectName NVARCHAR(64) NULL,
        DurationValue INT NOT NULL,
        PenaltyEnd DATETIME2(3) NULL,
        Reason NVARCHAR(255) NOT NULL,
        ActorAccountId BIGINT NOT NULL,
        ActorCharacterId BIGINT NULL,
        ActorName NVARCHAR(64) NULL,
        CONSTRAINT FK_GmSanctionAction_GmCase
            FOREIGN KEY (CaseId) REFERENCES dbo.GmCase(CaseId),
        CONSTRAINT CK_GmSanctionAction_ActionType
            CHECK (ActionType BETWEEN 1 AND 6),
        CONSTRAINT CK_GmSanctionAction_AffectedPenaltyCount
            CHECK (AffectedPenaltyCount >= 1),
        CONSTRAINT CK_GmSanctionAction_DurationValue
            CHECK (DurationValue >= 0)
    );
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmSanctionAction')
      AND name = N'UX_GmSanctionAction_OperationId'
)
BEGIN
    CREATE UNIQUE INDEX UX_GmSanctionAction_OperationId
        ON dbo.GmSanctionAction(OperationId);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmSanctionAction')
      AND name = N'IX_GmSanctionAction_CaseOccurred'
)
BEGIN
    CREATE INDEX IX_GmSanctionAction_CaseOccurred
        ON dbo.GmSanctionAction(CaseId, OccurredAtUtc DESC, ActionId DESC)
        INCLUDE (ActionType, SubjectAccountId, SubjectCharacterId, SubjectName,
                 ActorAccountId, ActorCharacterId, ActorName, PenaltyLogId);
END;

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.GmSanctionAction')
      AND name = N'IX_GmSanctionAction_SubjectOccurred'
)
BEGIN
    CREATE INDEX IX_GmSanctionAction_SubjectOccurred
        ON dbo.GmSanctionAction(SubjectAccountId, OccurredAtUtc DESC)
        INCLUDE (CaseId, ActionType, SubjectCharacterId, SubjectName, ActorName);
END;

COMMIT TRANSACTION;
