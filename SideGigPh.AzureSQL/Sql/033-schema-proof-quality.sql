/* =====================================================================================
   SideGigPh schema change 033: proof-quality.

   What it adds and why (badge proof photos that the operator can read, measured before anything is refused):
     - WorkerVerifications.PhotoProblems: what the server's photo check found ('blurry,glare,dark,too-small'), shown
       to the operator and to the worker as a gentle "take it again?"; NULL = nothing found.
     - WorkerVerifications.PhotoSharpness: the check's sharpness number, kept to set real cut-offs from data later.
     - WorkerVerifications.ScanConfidence: how sure the AI reader was that it read an ID (0-1), a hint for the operator.
     - WorkerVerifications.RejectCode: why the operator rejected ('blurry', 'glare', 'cut-off', 'wrong-document',
       'expired', 'mismatch', 'other'), so we can see whether unreadable photos go down.
     - VerificationAudit.Action: allows 'retaken', a worker's new photo in place of the one still waiting (the old
       file is deleted), so the log shows every photo the operator could have seen.

   How: run AFTER every earlier schema script and BEFORE deploying the API that uses it. Use the portal
   Query editor and run the WHOLE file. Safe to run again: every step checks first and skips what
   is already there (see .claude/skills/db-migration).
   ===================================================================================== */

SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* ---------- Which schema changes this database has had ---------- */
IF OBJECT_ID(N'dbo.SchemaChanges', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaChanges
    (
        Script     varchar(100)      NOT NULL CONSTRAINT PK_SchemaChanges PRIMARY KEY,
        AppliedAt  datetimeoffset(3) NOT NULL CONSTRAINT DF_SchemaChanges_AppliedAt DEFAULT (SYSDATETIMEOFFSET())
    );
END;

/* ---------- WorkerVerifications: the photo check, the AI's confidence and the operator's reason ---------- */
IF OBJECT_ID(N'dbo.WorkerVerifications', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.WorkerVerifications', N'PhotoProblems') IS NULL
    EXEC (N'ALTER TABLE dbo.WorkerVerifications ADD PhotoProblems varchar(100) NULL;');

IF OBJECT_ID(N'dbo.WorkerVerifications', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.WorkerVerifications', N'PhotoSharpness') IS NULL
    EXEC (N'ALTER TABLE dbo.WorkerVerifications ADD PhotoSharpness float NULL;');

IF OBJECT_ID(N'dbo.WorkerVerifications', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.WorkerVerifications', N'ScanConfidence') IS NULL
    EXEC (N'ALTER TABLE dbo.WorkerVerifications ADD ScanConfidence float NULL;');

IF OBJECT_ID(N'dbo.WorkerVerifications', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.WorkerVerifications', N'RejectCode') IS NULL
    EXEC (N'ALTER TABLE dbo.WorkerVerifications ADD RejectCode varchar(30) NULL;');

/* ---------- VerificationAudit.Action: allow 'retaken' ---------- */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_VerificationAudit_Action' AND definition NOT LIKE N'%retaken%')
    EXEC (N'ALTER TABLE dbo.VerificationAudit DROP CONSTRAINT CK_VerificationAudit_Action;');

IF OBJECT_ID(N'dbo.VerificationAudit', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.CK_VerificationAudit_Action') IS NULL
    EXEC (N'ALTER TABLE dbo.VerificationAudit ADD CONSTRAINT CK_VerificationAudit_Action CHECK (Action IN (''submitted'', ''scanned'', ''viewed'', ''approved'', ''rejected'', ''deleted'', ''retaken''));');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '033-schema-proof-quality')
    INSERT dbo.SchemaChanges (Script) VALUES ('033-schema-proof-quality');

COMMIT TRANSACTION;
