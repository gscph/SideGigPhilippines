/* =====================================================================================
   SideGigPh schema change 010: error-reports.

   What it adds and why:
   - dbo.ErrorReports: the problems the app had on people's phones (errors, crashes, screens stuck loading), sent in
     the background by the app (POST error-reports, ErrorReportStore) so we can fix what we can't reproduce. Keyed by
     a random install id, never the account: no uid, name, number or chat text (the app and the server both scrub
     them, ErrorReportRules.Scrub). Kept 90 days (ErrorReportStore.KeepFor), then deleted.
   - IX_ErrorReports_ReceivedAt: the 90-day clean-up and "what came in lately".
   - IX_ErrorReports_Source: "how often does this error happen, on which versions".

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

/* ---------- ErrorReports: problems the app had on a phone (diagnostics, no personal data) ---------- */
IF OBJECT_ID(N'dbo.ErrorReports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ErrorReports
    (
        Id          bigint            IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ErrorReports PRIMARY KEY,
        InstallId   varchar(36)       NOT NULL,  -- random per app install, not the account
        AppVersion  varchar(30)       NOT NULL,
        Platform    varchar(30)       NOT NULL,  -- e.g. "android 14"
        Device      nvarchar(80)      NULL,      -- e.g. "samsung SM-A155F"
        Kind        varchar(12)       NOT NULL CONSTRAINT CK_ErrorReports_Kind CHECK (Kind IN ('error', 'crash', 'slow')),
        Source      varchar(200)      NOT NULL,  -- where in the app (the logger's category)
        Message     nvarchar(1000)    NOT NULL,
        Detail      nvarchar(4000)    NULL,      -- the exception and stack trace
        Route       varchar(200)      NULL,      -- the screen it happened on
        Trail       nvarchar(2000)    NULL,      -- the screens and steps just before it
        Count       int               NOT NULL,  -- the same problem repeated on the phone
        FirstAt     datetimeoffset(3) NOT NULL,
        LastAt      datetimeoffset(3) NOT NULL,
        ReceivedAt  datetimeoffset(3) NOT NULL
    );

    CREATE INDEX IX_ErrorReports_ReceivedAt ON dbo.ErrorReports (ReceivedAt);
    CREATE INDEX IX_ErrorReports_Source ON dbo.ErrorReports (Source, AppVersion);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '010-schema-error-reports')
    INSERT dbo.SchemaChanges (Script) VALUES ('010-schema-error-reports');

COMMIT TRANSACTION;
