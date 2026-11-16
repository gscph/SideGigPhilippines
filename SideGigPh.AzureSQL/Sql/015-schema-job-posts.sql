/* =====================================================================================
   SideGigPh schema change 015: job posts (a household posts the work it needs; workers of that trade nearby see it).

   What it adds and why:
   - dbo.JobPosts: one post by a household (POST job-posts, JobPostStore): the trade and service, what is wrong, a
     budget in pesos, the town, and a point (WGS 84). The point is the phone's when it sent one, else the town's
     centre. PRIVATE: workers only ever get the town and a rounded distance. Status 'open' until the household
     closes it ('closed') or the operator hides it ('hidden').
   - dbo.JobPosts.Location: the same point as geography (SRID 4326), computed and stored, so the workers' feed reads
     Professionals.Location.STDistance(JobPosts.Location) <= Professionals.RadiusKm * 1000 (metres).
   - SIX_JobPosts_Location: the spatial index for those distance reads.
   - IX_JobPosts_Open: the workers' feed (open posts of one trade, newest first).
   - IX_JobPosts_Seeker: a household's own posts, newest first.
   - dbo.JobPostPhotos: up to 3 photos of the problem per post (ids of blobs in the public photos container).

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

/* ---------- JobPosts: the work a household needs done ---------- */
IF OBJECT_ID(N'dbo.JobPosts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobPosts
    (
        Id           bigint            IDENTITY(1, 1) NOT NULL CONSTRAINT PK_JobPosts PRIMARY KEY,
        SeekerUid    varchar(128)      NOT NULL,
        CategoryId   varchar(40)       NOT NULL,
        ServiceId    varchar(40)       NOT NULL,
        Description  nvarchar(600)     NOT NULL,
        BudgetPesos  int               NOT NULL CONSTRAINT CK_JobPosts_Budget CHECK (BudgetPesos BETWEEN 50 AND 1000000),
        CityId       varchar(40)       NOT NULL,
        Latitude     float             NULL CONSTRAINT CK_JobPosts_Latitude  CHECK (Latitude  BETWEEN -90 AND 90),
        Longitude    float             NULL CONSTRAINT CK_JobPosts_Longitude CHECK (Longitude BETWEEN -180 AND 180),
        Status       varchar(10)       NOT NULL CONSTRAINT DF_JobPosts_Status DEFAULT ('open')
                                       CONSTRAINT CK_JobPosts_Status CHECK (Status IN ('open', 'closed', 'hidden')),
        CreatedAt    datetimeoffset(3) NOT NULL,
        ClosedAt     datetimeoffset(3) NULL,
        Location     AS (CASE WHEN Latitude IS NOT NULL AND Longitude IS NOT NULL THEN geography::Point(Latitude, Longitude, 4326) END) PERSISTED,
        CONSTRAINT CK_JobPosts_LatLng CHECK ((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL))
    );

    CREATE INDEX IX_JobPosts_Open ON dbo.JobPosts (Status, CategoryId, CreatedAt DESC);
    CREATE INDEX IX_JobPosts_Seeker ON dbo.JobPosts (SeekerUid, CreatedAt DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'SIX_JobPosts_Location' AND object_id = OBJECT_ID(N'dbo.JobPosts'))
    EXEC (N'CREATE SPATIAL INDEX SIX_JobPosts_Location ON dbo.JobPosts (Location) USING GEOGRAPHY_AUTO_GRID;');

/* ---------- JobPostPhotos: photos of the problem ---------- */
IF OBJECT_ID(N'dbo.JobPostPhotos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobPostPhotos
    (
        PostId    bigint       NOT NULL CONSTRAINT FK_JobPostPhotos_Post REFERENCES dbo.JobPosts (Id) ON DELETE CASCADE,
        PhotoId   varchar(64)  NOT NULL,
        Position  smallint     NOT NULL,
        CONSTRAINT PK_JobPostPhotos PRIMARY KEY (PostId, PhotoId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '015-schema-job-posts')
    INSERT dbo.SchemaChanges (Script) VALUES ('015-schema-job-posts');

COMMIT TRANSACTION;
