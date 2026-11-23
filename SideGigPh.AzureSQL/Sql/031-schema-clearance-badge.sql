/* =====================================================================================
   SideGigPh schema change 031: clearance-badge.

   What it adds and why:
     - WorkerVerifications.Kind: allows 'clearance', a fourth badge (an NBI or police clearance, checked by a person,
       like a TESDA certificate). The old check knew only id / tesda / developer.

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

/* ---------- WorkerVerifications.Kind: allow 'clearance' (NBI or police clearance) ---------- */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_WorkerVerifications_Kind' AND definition NOT LIKE N'%clearance%')
    EXEC (N'ALTER TABLE dbo.WorkerVerifications DROP CONSTRAINT CK_WorkerVerifications_Kind;');

IF OBJECT_ID(N'dbo.WorkerVerifications', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.CK_WorkerVerifications_Kind') IS NULL
    EXEC (N'ALTER TABLE dbo.WorkerVerifications ADD CONSTRAINT CK_WorkerVerifications_Kind CHECK (Kind IN (''id'', ''tesda'', ''developer'', ''clearance''));');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '031-schema-clearance-badge')
    INSERT dbo.SchemaChanges (Script) VALUES ('031-schema-clearance-badge');

COMMIT TRANSACTION;
