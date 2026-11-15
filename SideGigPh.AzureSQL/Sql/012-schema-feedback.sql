/* =====================================================================================
   SideGigPh schema change 012: feedback.

   What it adds and why (the feedback board: people suggest features and vote, most voted on top):
   - dbo.FeedbackPosts: one suggestion (POST feedback, FeedbackStore). Shown to every signed-in user WITHOUT the
     writer's name. Votes is the count of its FeedbackVotes rows, kept on the row so the board sorts without counting.
     Status is the operator's: open (new), planned, done, declined or hidden (the last two leave the board).
     AuthorUid is cleared when the writer deletes their account; the suggestion stays.
   - IX_FeedbackPosts_Top: the board's order, most voted first, then newest.
   - IX_FeedbackPosts_Author: a person's own suggestions (the same one sent twice is answered with the first).
   - dbo.FeedbackVotes: one row per person per suggestion (PUT / DELETE feedback/{id}/vote), so nobody votes twice.
     Removed with the suggestion (cascade) and when the voter deletes their account.
   - IX_FeedbackVotes_Voter: which suggestions the caller voted for.

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

/* ---------- FeedbackPosts: a feature someone suggested ---------- */
IF OBJECT_ID(N'dbo.FeedbackPosts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FeedbackPosts
    (
        Id          bigint            IDENTITY(1, 1) NOT NULL CONSTRAINT PK_FeedbackPosts PRIMARY KEY,
        AuthorUid   varchar(128)      NULL,      -- null once the writer deleted their account
        Side        varchar(20)       NULL CONSTRAINT CK_FeedbackPosts_Side CHECK (Side IN ('seeker', 'professional')),
        Title       nvarchar(100)     NOT NULL,
        Details     nvarchar(600)     NULL,
        Votes       int               NOT NULL CONSTRAINT DF_FeedbackPosts_Votes DEFAULT (0)
                                      CONSTRAINT CK_FeedbackPosts_Votes CHECK (Votes >= 0),
        Status      varchar(10)       NOT NULL CONSTRAINT DF_FeedbackPosts_Status DEFAULT ('open')
                                      CONSTRAINT CK_FeedbackPosts_Status CHECK (Status IN ('open', 'planned', 'done', 'declined', 'hidden')),
        CreatedAt   datetimeoffset(3) NOT NULL
    );

    CREATE INDEX IX_FeedbackPosts_Top ON dbo.FeedbackPosts (Status, Votes DESC, CreatedAt DESC);
    CREATE INDEX IX_FeedbackPosts_Author ON dbo.FeedbackPosts (AuthorUid) WHERE AuthorUid IS NOT NULL;
END;

/* ---------- FeedbackVotes: who voted for which suggestion (one vote each) ---------- */
IF OBJECT_ID(N'dbo.FeedbackVotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FeedbackVotes
    (
        PostId      bigint            NOT NULL CONSTRAINT FK_FeedbackVotes_Post REFERENCES dbo.FeedbackPosts (Id) ON DELETE CASCADE,
        VoterUid    varchar(128)      NOT NULL,
        CreatedAt   datetimeoffset(3) NOT NULL,
        CONSTRAINT PK_FeedbackVotes PRIMARY KEY (PostId, VoterUid)
    );

    CREATE INDEX IX_FeedbackVotes_Voter ON dbo.FeedbackVotes (VoterUid);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '012-schema-feedback')
    INSERT dbo.SchemaChanges (Script) VALUES ('012-schema-feedback');

COMMIT TRANSACTION;
