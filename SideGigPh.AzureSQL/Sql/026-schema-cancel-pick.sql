/* =====================================================================================
   SideGigPh schema change 026: cancel-pick.

   What it adds and why (QA: once a household picked an offer there was no way back; a worker who can't come, or
   one who never shows up, left the post closed and the household had to post again):
   - dbo.JobOffers.Status may now be 'cancelled': either side called off a picked job before both marked it done.
     The post opens again (its other offers can be picked), the picked worker's number stops showing, and the chat
     between the two stays.
   - dbo.JobOffers.CancelledAt / CancelledBy ('household' or 'worker') / CancelReason (an id from the app's short
     list, e.g. 'no-show') / CancelNote (optional words, up to 300): why it was called off, for the operator and
     for the other side's chat message. Nothing is shown publicly.

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

/* ---------- JobOffers.Status: allow 'cancelled' (the old check knew only sent / picked / withdrawn) ---------- */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_JobOffers_Status' AND definition NOT LIKE N'%cancelled%')
    EXEC (N'ALTER TABLE dbo.JobOffers DROP CONSTRAINT CK_JobOffers_Status;');

IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.CK_JobOffers_Status') IS NULL
    EXEC (N'ALTER TABLE dbo.JobOffers ADD CONSTRAINT CK_JobOffers_Status CHECK (Status IN (''sent'', ''picked'', ''withdrawn'', ''cancelled''));');

/* ---------- JobOffers: who called a picked job off, when and why ---------- */
IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobOffers', N'CancelledAt') IS NULL
    EXEC (N'ALTER TABLE dbo.JobOffers ADD CancelledAt datetimeoffset(3) NULL;');

IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobOffers', N'CancelledBy') IS NULL
    EXEC (N'ALTER TABLE dbo.JobOffers ADD CancelledBy varchar(10) NULL;');

IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.CK_JobOffers_CancelledBy') IS NULL
    EXEC (N'ALTER TABLE dbo.JobOffers ADD CONSTRAINT CK_JobOffers_CancelledBy CHECK (CancelledBy IS NULL OR CancelledBy IN (''household'', ''worker''));');

IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobOffers', N'CancelReason') IS NULL
    EXEC (N'ALTER TABLE dbo.JobOffers ADD CancelReason varchar(20) NULL;');

IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobOffers', N'CancelNote') IS NULL
    EXEC (N'ALTER TABLE dbo.JobOffers ADD CancelNote nvarchar(300) NULL;');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '026-schema-cancel-pick')
    INSERT dbo.SchemaChanges (Script) VALUES ('026-schema-cancel-pick');

COMMIT TRANSACTION;
