/* =====================================================================================
   SideGigPh schema change 027: post-title.

   What it adds and why:
   - JobPosts.Title (nvarchar(80), NULL): the household's own short title for what they need done
     ("Tumutulo ang aircon sa sala"), typed on the post form. It is the post's header for both sides;
     the service ("Linis ng aircon") shows under it as a hint. NULL only on posts made before this
     change (the app shows the service as the header then); new and edited posts always send one.

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

/* ---------- A job post's title ---------- */
IF OBJECT_ID(N'dbo.JobPosts', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobPosts', N'Title') IS NULL
    EXEC (N'ALTER TABLE dbo.JobPosts ADD Title nvarchar(80) NULL;');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '027-schema-post-title')
    INSERT dbo.SchemaChanges (Script) VALUES ('027-schema-post-title');

COMMIT TRANSACTION;
