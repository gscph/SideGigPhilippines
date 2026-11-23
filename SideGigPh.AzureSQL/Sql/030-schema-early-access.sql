/* =====================================================================================
   SideGigPh schema change 030: early-access.

   What it adds and why:
     - dbo.EarlyAccessSignups: the landing page's (tarbaho.com) Early Access sign-ups, one row per email address
       (POST early-access, EarlyAccessStore). They used to be emailed to the team; now they are kept here, so the
       list is read in SQL. The same address sent again updates its row (a repeat is safe).

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

/* ---------- EarlyAccessSignups: who asked for the beta on tarbaho.com ---------- */
IF OBJECT_ID(N'dbo.EarlyAccessSignups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EarlyAccessSignups
    (
        Email      nvarchar(254)     NOT NULL CONSTRAINT PK_EarlyAccessSignups PRIMARY KEY,  -- trimmed, lower case
        Phone      varchar(10)       NOT NULL,  -- 'android' (invite to the Play testing list) or 'iphone' (TestFlight)
        Role       varchar(12)       NULL,      -- 'household', 'worker' or not picked
        Language   varchar(2)        NULL,      -- 'en' or 'tl'
        CreatedAt  datetimeoffset(3) NOT NULL,
        UpdatedAt  datetimeoffset(3) NOT NULL,
        CONSTRAINT CK_EarlyAccessSignups_Phone CHECK (Phone IN ('android', 'iphone'))
    );
END;

IF OBJECT_ID(N'dbo.EarlyAccessSignups', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EarlyAccessSignups_Created' AND object_id = OBJECT_ID(N'dbo.EarlyAccessSignups'))
    EXEC (N'CREATE INDEX IX_EarlyAccessSignups_Created ON dbo.EarlyAccessSignups (CreatedAt);');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '030-schema-early-access')
    INSERT dbo.SchemaChanges (Script) VALUES ('030-schema-early-access');

COMMIT TRANSACTION;
