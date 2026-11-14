/* =====================================================================================
   SideGigPh schema change 008: a price guide on every job.

   What it adds and why:
     - dbo.Services.PriceMin / PriceAverage / PriceHigh: what this job usually costs (pesos, labor only),
       filled by 004-config.sql. PriceMin is never below the NCR daily minimum wage (ConfigOptions
       'minimum-wage'). Skilled Workers pick one of the three as their rate ('minimum', 'average',
       'above-average'); Seekers pick a budget range built from them. NULL on retired jobs.
     - CK_Services_Prices: the three go up (min <= average <= high) when they are set.

   How: run AFTER every earlier schema script, THEN run 004-config.sql to fill the prices, then deploy
   the API. Use the portal Query editor and run the WHOLE file. Safe to run again: every step checks
   first and skips what is already there (see .claude/skills/db-migration).
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

/* ---------- Services: the job's price guide ---------- */
IF OBJECT_ID(N'dbo.Services', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Services', N'PriceMin') IS NULL
    EXEC (N'ALTER TABLE dbo.Services ADD PriceMin int NULL;');

IF OBJECT_ID(N'dbo.Services', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Services', N'PriceAverage') IS NULL
    EXEC (N'ALTER TABLE dbo.Services ADD PriceAverage int NULL;');

IF OBJECT_ID(N'dbo.Services', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Services', N'PriceHigh') IS NULL
    EXEC (N'ALTER TABLE dbo.Services ADD PriceHigh int NULL;');

IF OBJECT_ID(N'dbo.Services', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.CK_Services_Prices') IS NULL
    EXEC (N'ALTER TABLE dbo.Services ADD CONSTRAINT CK_Services_Prices CHECK (
        (PriceMin IS NULL AND PriceAverage IS NULL AND PriceHigh IS NULL)
        OR (PriceMin > 0 AND PriceMin <= PriceAverage AND PriceAverage <= PriceHigh));');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '008-schema-service-prices')
    INSERT dbo.SchemaChanges (Script) VALUES ('008-schema-service-prices');

COMMIT TRANSACTION;
