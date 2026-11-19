/* =====================================================================================
   SideGigPh schema change 024: post-needed-date.

   What it adds and why:
   - JobPosts.NeededOn (date, NULL): the day the household needs the work done, picked on the post form's
     calendar ("When"). Optional (NULL = any day). Workers see it on the request row and page. It never
     touches search (search keeps the interview's "When" answer only to word the first message).

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

/* ---------- A job post's day (optional) ---------- */
IF OBJECT_ID(N'dbo.JobPosts', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobPosts', N'NeededOn') IS NULL
    EXEC (N'ALTER TABLE dbo.JobPosts ADD NeededOn date NULL;');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '024-schema-post-needed-date')
    INSERT dbo.SchemaChanges (Script) VALUES ('024-schema-post-needed-date');

COMMIT TRANSACTION;
