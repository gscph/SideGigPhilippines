/* =====================================================================================
   SideGigPh schema change 017: others-and-ranges.

   What it adds and why:
   - Professionals.OtherWorkText: what a worker typed under "Others" (a kind of work we don't list, or a job their
     trade doesn't list). Search matches it against what a Seeker typed under "Others" (SearchRanker, shared words).
   - JobPosts.BudgetMinPesos: the low end of a household's budget ("from"); BudgetPesos stays the top ("up to"),
     so older apps keep reading the post. NULL = only a top was given.

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

/* ---------- A worker's "Others" text ---------- */
IF OBJECT_ID(N'dbo.Professionals', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Professionals', N'OtherWorkText') IS NULL
    EXEC (N'ALTER TABLE dbo.Professionals ADD OtherWorkText nvarchar(100) NULL;');

/* ---------- A job post's budget "from" ---------- */
IF OBJECT_ID(N'dbo.JobPosts', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobPosts', N'BudgetMinPesos') IS NULL
    EXEC (N'ALTER TABLE dbo.JobPosts ADD BudgetMinPesos int NULL;');

IF OBJECT_ID(N'dbo.JobPosts', N'U') IS NOT NULL AND OBJECT_ID(N'CK_JobPosts_BudgetMin') IS NULL
    EXEC (N'ALTER TABLE dbo.JobPosts ADD CONSTRAINT CK_JobPosts_BudgetMin CHECK (BudgetMinPesos IS NULL OR BudgetMinPesos BETWEEN 0 AND BudgetPesos);');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '017-schema-others-and-ranges')
    INSERT dbo.SchemaChanges (Script) VALUES ('017-schema-others-and-ranges');

COMMIT TRANSACTION;
