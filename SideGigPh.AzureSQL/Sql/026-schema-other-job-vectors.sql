/* =====================================================================================
   SideGigPh schema change 026: other-job-vectors.

   What it adds and why (the operator site groups what people typed under "Others", so the most requested missing
   work shows at the top; texts that mean the same, "linis ng pool" and "pool cleaning", are merged by comparing
   their Azure OpenAI embeddings):
   - dbo.OtherJobTextVectors: one embedding per cleaned text (lower case, no accents or punctuation) and model, so
     each distinct text is sent to Azure once, not on every look at the page. TextKey is the cleaned text (no uid,
     nothing about who typed it; Delete account has nothing to remove here), Model the deployment and size that made
     the vector (a new model makes new rows, the old ones are simply not read), Vector the numbers as float32
     little-endian bytes. TextKey compares byte for byte (BIN2): the database's usual case-, width- and kana-blind
     comparison would make two different cleaned texts one key, and the second's vector could never be stored.

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

/* ---------- OtherJobTextVectors: an "Others" text's embedding, made once per model ---------- */
IF OBJECT_ID(N'dbo.OtherJobTextVectors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OtherJobTextVectors
    (
        TextKey    nvarchar(100)     COLLATE Latin1_General_100_BIN2 NOT NULL,
        Model      varchar(64)       NOT NULL,
        Vector     varbinary(max)    NOT NULL,
        CreatedAt  datetimeoffset(3) NOT NULL CONSTRAINT DF_OtherJobTextVectors_CreatedAt DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT PK_OtherJobTextVectors PRIMARY KEY (TextKey, Model)
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '026-schema-other-job-vectors')
    INSERT dbo.SchemaChanges (Script) VALUES ('026-schema-other-job-vectors');

COMMIT TRANSACTION;
