/* =====================================================================================
   SideGigPh schema change 017: job done, and households' reviews (verified two-way reviews, brief item 12).

   What it adds and why:
   - dbo.JobOffers.HouseholdDoneAt / WorkerDoneAt: on a picked offer, when each side marked the work done
     (POST picked-jobs/{postId}/done). Both set = a completed job: the household's review of that worker shows
     "Verified job", the worker can rate the household, and the job counts on the worker's profile.
   - IX_JobOffers_Done: the completed jobs per worker (filtered to both marks set).
   - dbo.HouseholdReviews: a worker's stars (and optional comment) for a household, one per worker per household,
     writing again replaces it (PUT household-reviews). Only after a completed job between the two. Workers see a
     household's average on its posts.
   - IX_HouseholdReviews_Seeker: a household's stars, for the posts in a worker's feed.

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

/* ---------- JobOffers: each side marks a picked job done ---------- */
IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobOffers', N'HouseholdDoneAt') IS NULL
    EXEC (N'ALTER TABLE dbo.JobOffers ADD HouseholdDoneAt datetimeoffset(3) NULL;');

IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobOffers', N'WorkerDoneAt') IS NULL
    EXEC (N'ALTER TABLE dbo.JobOffers ADD WorkerDoneAt datetimeoffset(3) NULL;');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobOffers_Done' AND object_id = OBJECT_ID(N'dbo.JobOffers'))
    EXEC (N'CREATE INDEX IX_JobOffers_Done ON dbo.JobOffers (ProUid) WHERE HouseholdDoneAt IS NOT NULL AND WorkerDoneAt IS NOT NULL;');

/* ---------- HouseholdReviews: a worker's stars for a household ---------- */
IF OBJECT_ID(N'dbo.HouseholdReviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HouseholdReviews
    (
        ProUid     varchar(128)      NOT NULL,
        SeekerUid  varchar(128)      NOT NULL,
        Rating     tinyint           NOT NULL CONSTRAINT CK_HouseholdReviews_Rating CHECK (Rating BETWEEN 1 AND 5),
        Comment    nvarchar(1000)    NULL,
        CreatedAt  datetimeoffset(3) NOT NULL,
        UpdatedAt  datetimeoffset(3) NOT NULL,
        CONSTRAINT PK_HouseholdReviews PRIMARY KEY (ProUid, SeekerUid)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HouseholdReviews_Seeker' AND object_id = OBJECT_ID(N'dbo.HouseholdReviews'))
    EXEC (N'CREATE INDEX IX_HouseholdReviews_Seeker ON dbo.HouseholdReviews (SeekerUid) INCLUDE (Rating);');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '017-schema-job-done')
    INSERT dbo.SchemaChanges (Script) VALUES ('017-schema-job-done');

COMMIT TRANSACTION;
