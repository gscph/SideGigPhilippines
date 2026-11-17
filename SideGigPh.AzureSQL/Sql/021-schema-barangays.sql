/* =====================================================================================
   SideGigPh schema change 021: barangays.

   What it adds and why (the narrower launch: critical mass in one town, then one barangay at a time):
   - dbo.Barangays: the barangays we serve, per town (CityId = a 'towns' id in dbo.ConfigOptions). Filled by
     004-config.sql like the trades: ids are stable and have no accents, a barangay we stop serving is retired
     (IsActive = 0), never deleted. GET config sends them; a household may pick one (never required).
   - JobPosts.BarangayId: the barangay a household picked on its post (optional, NULL = not sure / not given).
     Workers see "Angono · Brgy. San Isidro" next to the distance; the household's point stays private.
   The launch's default town is data only (ConfigOptions list 'default-town' in 004-config.sql): no schema change.

   How: run AFTER every earlier schema script and BEFORE deploying the API that uses it, then run 004-config.sql
   (the deploy's manual run with "Also run 004-config.sql" ticked) to fill dbo.Barangays. Safe to run again: every
   step checks first and skips what is already there (see .claude/skills/db-migration).
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

/* ---------- Barangays: the ones we serve, per town (GET config). Rows come from 004-config.sql ---------- */
IF OBJECT_ID(N'dbo.Barangays', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Barangays
    (
        Id            varchar(40)    NOT NULL CONSTRAINT PK_Barangays PRIMARY KEY,  -- stable, town first: 'angono-san-isidro'
        CityId        varchar(40)    NOT NULL,                                       -- the town ('towns' in dbo.ConfigOptions)
        LabelEnglish  nvarchar(60)   NOT NULL,                                       -- "San Isidro" (the app adds "Brgy.")
        LabelTaglish  nvarchar(60)   NOT NULL,
        SortOrder     int            NOT NULL,
        IsActive      bit            NOT NULL CONSTRAINT DF_Barangays_IsActive DEFAULT (1)  -- 0 = retired: no longer offered
    );
END;

IF OBJECT_ID(N'dbo.Barangays', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Barangays_City' AND object_id = OBJECT_ID(N'dbo.Barangays'))
    EXEC (N'CREATE INDEX IX_Barangays_City ON dbo.Barangays (CityId, SortOrder);');

/* ---------- A job post's barangay (optional) ---------- */
IF OBJECT_ID(N'dbo.JobPosts', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobPosts', N'BarangayId') IS NULL
    EXEC (N'ALTER TABLE dbo.JobPosts ADD BarangayId varchar(40) NULL;');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '021-schema-barangays')
    INSERT dbo.SchemaChanges (Script) VALUES ('021-schema-barangays');

COMMIT TRANSACTION;
