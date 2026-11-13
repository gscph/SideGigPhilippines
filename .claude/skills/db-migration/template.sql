/* =====================================================================================
   SideGigPh schema change __NUMBER__: __WHAT__.

   What it adds and why: (one line per table / column / index, and the feature that needs it).

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

/* ---------- Example: a new column (replace with the real steps) ----------
IF OBJECT_ID(N'dbo.Table', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Table', N'NewColumn') IS NULL
    EXEC (N'ALTER TABLE dbo.Table ADD NewColumn datetimeoffset(3) NULL;');
*/

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '__SCRIPT__')
    INSERT dbo.SchemaChanges (Script) VALUES ('__SCRIPT__');

COMMIT TRANSACTION;
