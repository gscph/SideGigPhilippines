/* =====================================================================================
   SideGigPh schema change 025: feedback-source.

   What it adds and why (the feedback board on the web: the public page GET suggest, given to Play testers as the
   "Feedback URL", shows the same board as the app; visitors upvote a suggestion that is already there or post theirs):
   - dbo.FeedbackPosts.Source: where a suggestion was written, 'app' (the app's board, every row so far) or 'web'
     (the public page). The board shows both the same way; the operator can tell them apart in SQL.
   Web visitors vote in dbo.FeedbackVotes like app users, with VoterUid 'web-<random id>' from a cookie, so no new
   table is needed: one person, one vote, the same counts everywhere.

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

/* ---------- FeedbackPosts.Source: 'app' or 'web' (every existing row came from the app) ---------- */
IF OBJECT_ID(N'dbo.FeedbackPosts', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.FeedbackPosts', N'Source') IS NULL
    EXEC (N'ALTER TABLE dbo.FeedbackPosts ADD Source varchar(10) NOT NULL CONSTRAINT DF_FeedbackPosts_Source DEFAULT (''app'');');

IF OBJECT_ID(N'dbo.FeedbackPosts', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.CK_FeedbackPosts_Source') IS NULL
    EXEC (N'ALTER TABLE dbo.FeedbackPosts ADD CONSTRAINT CK_FeedbackPosts_Source CHECK (Source IN (''app'', ''web''));');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '025-schema-feedback-source')
    INSERT dbo.SchemaChanges (Script) VALUES ('025-schema-feedback-source');

COMMIT TRANSACTION;
