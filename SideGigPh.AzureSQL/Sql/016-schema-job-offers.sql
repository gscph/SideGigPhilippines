/* =====================================================================================
   SideGigPh schema change 016: offers on job posts (inDrive style: a worker takes the household's budget or names
   another price; the household picks one).

   What it adds and why:
   - dbo.JobOffers: one worker's offer on one post (POST job-posts/{id}/offers, JobPostStore): the price in pesos
     (the household's budget, or the worker's own number) and an optional short note. One row per worker per post:
     a new price replaces the old one. Status 'sent' while it waits, 'picked' when the household chose it (the post
     then closes), 'withdrawn' when the worker took it back. Goes with the post (ON DELETE CASCADE).
   - UX_JobOffers_PostPro: one offer per worker per post, and the household's list of offers on a post.
   - IX_JobOffers_Pro: a worker's own offers (and Delete account).

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

/* ---------- JobOffers: a worker's price on a household's post ---------- */
IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobOffers
    (
        Id          bigint            IDENTITY(1, 1) NOT NULL CONSTRAINT PK_JobOffers PRIMARY KEY,
        PostId      bigint            NOT NULL CONSTRAINT FK_JobOffers_Post REFERENCES dbo.JobPosts (Id) ON DELETE CASCADE,
        ProUid      varchar(128)      NOT NULL,
        PricePesos  int               NOT NULL CONSTRAINT CK_JobOffers_Price CHECK (PricePesos BETWEEN 50 AND 1000000),
        Note        nvarchar(300)     NULL,
        Status      varchar(10)       NOT NULL CONSTRAINT DF_JobOffers_Status DEFAULT ('sent')
                                      CONSTRAINT CK_JobOffers_Status CHECK (Status IN ('sent', 'picked', 'withdrawn')),
        CreatedAt   datetimeoffset(3) NOT NULL,
        UpdatedAt   datetimeoffset(3) NOT NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_JobOffers_PostPro' AND object_id = OBJECT_ID(N'dbo.JobOffers'))
    CREATE UNIQUE INDEX UX_JobOffers_PostPro ON dbo.JobOffers (PostId, ProUid);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobOffers_Pro' AND object_id = OBJECT_ID(N'dbo.JobOffers'))
    CREATE INDEX IX_JobOffers_Pro ON dbo.JobOffers (ProUid);

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '016-schema-job-offers')
    INSERT dbo.SchemaChanges (Script) VALUES ('016-schema-job-offers');

COMMIT TRANSACTION;
