/* =====================================================================================
   SideGigPh schema change 034: clearance-valid-until.

   What it adds and why:
     - WorkerVerifications.ValidUntil (date, NULL): the "valid until" date the operator reads off an NBI or police
       clearance and types when approving it. It is the one fact kept after the decision: the clearance badge lapses
       the day after it (ClearanceRules), and the worker may then send a new one. NULL for every other badge.
       Clearances approved before this script get the shortest term, 6 months from the decision (a police
       clearance's; nobody typed their date), so none shows forever.
     - WorkerVerifications.ScannedQr (nvarchar(1000), NULL): what the server read from the clearance's QR code, shown
       only to the operator as a hint (an official NBI / PNP address to open), never a reason to refuse. Cleared with
       the proof when the operator decides, like the other Scanned* columns.

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

/* ---------- WorkerVerifications.ValidUntil: a clearance's "valid until", typed by the operator ---------- */
IF OBJECT_ID(N'dbo.WorkerVerifications', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.WorkerVerifications', N'ValidUntil') IS NULL
    EXEC (N'ALTER TABLE dbo.WorkerVerifications ADD ValidUntil date NULL;');

/* ---------- Clearances approved before 034: 6 months from the decision (Philippine date), the police term ---------- */
IF COL_LENGTH(N'dbo.WorkerVerifications', N'ValidUntil') IS NOT NULL
    EXEC (N'UPDATE dbo.WorkerVerifications
            SET ValidUntil = DATEADD(month, 6, CAST(SWITCHOFFSET(DecidedAt, ''+08:00'') AS date))
            WHERE Kind = ''clearance'' AND Status = ''approved'' AND ValidUntil IS NULL AND DecidedAt IS NOT NULL;');

/* ---------- WorkerVerifications.ScannedQr: the clearance's QR as read, for the operator only ---------- */
IF OBJECT_ID(N'dbo.WorkerVerifications', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.WorkerVerifications', N'ScannedQr') IS NULL
    EXEC (N'ALTER TABLE dbo.WorkerVerifications ADD ScannedQr nvarchar(1000) NULL;');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '034-schema-clearance-valid-until')
    INSERT dbo.SchemaChanges (Script) VALUES ('034-schema-clearance-valid-until');

COMMIT TRANSACTION;
