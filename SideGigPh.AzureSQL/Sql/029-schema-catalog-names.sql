/* =====================================================================================
   SideGigPh schema change 029: catalog-names.

   What it adds and why: the operator site can now rename, add and retire trades and jobs (ops/catalog). A rename
   must never rewrite what a household posted or what two people agreed on, the way an order line in an ERP keeps
   the product's title and price from the day it was ordered. So posts and chats keep their own copy of the names:
   - JobPosts.TradeLabelEnglish / TradeLabelTaglish / ServiceLabelEnglish / ServiceLabelTaglish: the trade's and the
     job's names when the post was made (or its job last changed). Picked jobs read them from their post.
   - Chats.ServiceLabelEnglish / ServiceLabelTaglish: the job's name when the chat started (or a pick moved it).
     NULL on rows for "Others" (it lives in the app's code, not in Categories) and when the id is unknown.
     Existing rows are filled from today's names.
   - Categories.EditedAt / EditedBy and Services.EditedAt / EditedBy: when and which operator last changed the row in
     the operator site. 004-config.sql leaves such rows alone (no overwrite, no retire), so an operator's edit is not
     undone the next time the config script runs.

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

/* ---------- A post keeps the names it was made with ---------- */
IF OBJECT_ID(N'dbo.JobPosts', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobPosts', N'TradeLabelEnglish') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.JobPosts ADD TradeLabelEnglish nvarchar(60) NULL, TradeLabelTaglish nvarchar(60) NULL,
                                         ServiceLabelEnglish nvarchar(100) NULL, ServiceLabelTaglish nvarchar(100) NULL;');
    EXEC (N'UPDATE p SET p.TradeLabelEnglish = c.LabelEnglish, p.TradeLabelTaglish = c.LabelTaglish
            FROM dbo.JobPosts AS p JOIN dbo.Categories AS c ON c.Id = p.CategoryId;');
    EXEC (N'UPDATE p SET p.ServiceLabelEnglish = s.LabelEnglish, p.ServiceLabelTaglish = s.LabelTaglish
            FROM dbo.JobPosts AS p JOIN dbo.Services AS s ON s.Id = p.ServiceId;');
END;

/* ---------- A chat keeps the job's name it was about ---------- */
IF OBJECT_ID(N'dbo.Chats', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Chats', N'ServiceLabelEnglish') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.Chats ADD ServiceLabelEnglish nvarchar(100) NULL, ServiceLabelTaglish nvarchar(100) NULL;');
    EXEC (N'UPDATE ch SET ch.ServiceLabelEnglish = s.LabelEnglish, ch.ServiceLabelTaglish = s.LabelTaglish
            FROM dbo.Chats AS ch JOIN dbo.Services AS s ON s.Id = ch.ServiceId;');
END;

/* ---------- Which trades and jobs an operator changed ---------- */
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Categories', N'EditedAt') IS NULL
    EXEC (N'ALTER TABLE dbo.Categories ADD EditedAt datetimeoffset(3) NULL, EditedBy varchar(128) NULL;');

IF OBJECT_ID(N'dbo.Services', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Services', N'EditedAt') IS NULL
    EXEC (N'ALTER TABLE dbo.Services ADD EditedAt datetimeoffset(3) NULL, EditedBy varchar(128) NULL;');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '029-schema-catalog-names')
    INSERT dbo.SchemaChanges (Script) VALUES ('029-schema-catalog-names');

COMMIT TRANSACTION;
