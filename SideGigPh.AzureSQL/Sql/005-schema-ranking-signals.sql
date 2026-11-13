/* =====================================================================================
   SideGigPh schema change 005: search ranking signals.

   Adds what search ranking reads about how Skilled Workers behave (docs/search-ranking.md):
     - dbo.Chats.ProfessionalRepliedAt: the worker's first reply in a chat (responsiveness).
       Filled from the messages already sent.
     - dbo.Users.LastActiveAt: when the person last used the app (a worker reading their Inbox,
       stamped at most hourly).

   How: run AFTER 001-schema.sql and BEFORE deploying the API that reads these columns. Use the portal
   Query editor and run the WHOLE file. Safe to run again: every step checks first and skips what
   is already there. Every schema change after 001 is a script like this one
   (see .claude/skills/db-migration).
   ===================================================================================== */

SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* ---------- Which schema changes this database has had ---------- */
IF OBJECT_ID(N'dbo.SchemaChanges', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SchemaChanges
    (
        Script     varchar(100)      NOT NULL CONSTRAINT PK_SchemaChanges PRIMARY KEY,  -- e.g. '005-schema-ranking-signals'
        AppliedAt  datetimeoffset(3) NOT NULL CONSTRAINT DF_SchemaChanges_AppliedAt DEFAULT (SYSDATETIMEOFFSET())
    );
END;

/* ---------- Chats.ProfessionalRepliedAt ---------- */
IF OBJECT_ID(N'dbo.Chats', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Chats', N'ProfessionalRepliedAt') IS NULL
BEGIN
    -- EXEC: the column is new in this run, so the UPDATE must be compiled after the ALTER.
    EXEC (N'ALTER TABLE dbo.Chats ADD ProfessionalRepliedAt datetimeoffset(3) NULL;');
    EXEC (N'UPDATE c SET ProfessionalRepliedAt =
                (SELECT MIN(m.At) FROM dbo.ChatMessages m WHERE m.ChatId = c.Id AND m.SenderId = c.ProfessionalId)
            FROM dbo.Chats c;');
END;

/* ---------- Users.LastActiveAt ---------- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Users', N'LastActiveAt') IS NULL
    EXEC (N'ALTER TABLE dbo.Users ADD LastActiveAt datetimeoffset(3) NULL;');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '005-schema-ranking-signals')
    INSERT dbo.SchemaChanges (Script) VALUES ('005-schema-ranking-signals');

COMMIT TRANSACTION;
