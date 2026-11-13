/* =====================================================================================
   SideGigPh schema change 007: the app config tables.

   What it adds and why: the tables behind GET config and search, which 001-schema.sql gained after
   the live database was made, so the live database never got them (search failed with
   "Invalid object name 'ConfigOptions'"):
     - dbo.Categories, dbo.Services: the trades and their jobs
     - dbo.ConfigOptions: towns, price tiers, experience, urgency, income goals
     - dbo.OtherJobNotes: what people typed after picking "Others" (analytics only)
   The same definitions as in 001, so a database that already has them skips each step.

   How: run AFTER every earlier schema script, THEN run 004-config.sql to fill these tables, then
   deploy the API. Use the portal Query editor and run the WHOLE file. Safe to run again: every step
   checks first and skips what is already there (see .claude/skills/db-migration).
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

/* ---------- Categories and Services: the trades and their jobs ---------- */
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        Id            varchar(40)    NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,  -- stable: never renamed, only retired
        LabelEnglish  nvarchar(60)   NOT NULL,
        LabelTaglish  nvarchar(60)   NOT NULL,
        Icon          nvarchar(16)   NULL,                                            -- an emoji
        HintEnglish   nvarchar(120)  NULL,
        HintTaglish   nvarchar(120)  NULL,
        SortOrder     int            NOT NULL,
        IsActive      bit            NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1)  -- 0 = retired: no longer offered
    );
END;

IF OBJECT_ID(N'dbo.Services', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Services
    (
        Id            varchar(40)    NOT NULL CONSTRAINT PK_Services PRIMARY KEY,    -- stable, unique across all trades
        CategoryId    varchar(40)    NOT NULL CONSTRAINT FK_Services_Categories REFERENCES dbo.Categories (Id),
        LabelEnglish  nvarchar(100)  NOT NULL,
        LabelTaglish  nvarchar(100)  NOT NULL,
        Icon          nvarchar(16)   NULL,
        SortOrder     int            NOT NULL,                                        -- most-requested first, "Others" last
        IsActive      bit            NOT NULL CONSTRAINT DF_Services_IsActive DEFAULT (1)
    );
END;

IF OBJECT_ID(N'dbo.Services', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Services_Category' AND object_id = OBJECT_ID(N'dbo.Services'))
    EXEC (N'CREATE INDEX IX_Services_Category ON dbo.Services (CategoryId, SortOrder);');

/* ---------- ConfigOptions: the other answer lists in GET config ---------- */
IF OBJECT_ID(N'dbo.ConfigOptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfigOptions
    (
        ListId        varchar(40)    NOT NULL,   -- 'towns', 'price-tiers', 'experience', 'urgencies', 'income-goals'
        Id            varchar(40)    NOT NULL,   -- stable, like the trades
        LabelEnglish  nvarchar(100)  NOT NULL,
        LabelTaglish  nvarchar(100)  NOT NULL,
        Icon          nvarchar(16)   NULL,
        Amount        int            NOT NULL CONSTRAINT DF_ConfigOptions_Amount DEFAULT (0),  -- pesos (price tiers, goals), else 0
        SortOrder     int            NOT NULL,
        IsActive      bit            NOT NULL CONSTRAINT DF_ConfigOptions_IsActive DEFAULT (1),
        CONSTRAINT PK_ConfigOptions PRIMARY KEY (ListId, Id)
    );
END;

/* ---------- OtherJobNotes: what people typed after picking "Others" ---------- */
IF OBJECT_ID(N'dbo.OtherJobNotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OtherJobNotes
    (
        Id          bigint            IDENTITY(1, 1) NOT NULL CONSTRAINT PK_OtherJobNotes PRIMARY KEY,
        Uid         varchar(128)      NULL,      -- who typed it; cleared when they delete their account
        Side        varchar(12)       NOT NULL CONSTRAINT CK_OtherJobNotes_Side CHECK (Side IN ('seeker', 'professional')),
        CategoryId  varchar(40)       NOT NULL,
        Text        nvarchar(100)     NOT NULL,
        CreatedAt   datetimeoffset(3) NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.OtherJobNotes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OtherJobNotes_Category' AND object_id = OBJECT_ID(N'dbo.OtherJobNotes'))
    EXEC (N'CREATE INDEX IX_OtherJobNotes_Category ON dbo.OtherJobNotes (CategoryId, CreatedAt);');

IF OBJECT_ID(N'dbo.OtherJobNotes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OtherJobNotes_Uid' AND object_id = OBJECT_ID(N'dbo.OtherJobNotes'))
    EXEC (N'CREATE INDEX IX_OtherJobNotes_Uid ON dbo.OtherJobNotes (Uid);');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '007-schema-config-tables')
    INSERT dbo.SchemaChanges (Script) VALUES ('007-schema-config-tables');

COMMIT TRANSACTION;
