/* =====================================================================================
   SideGigPh schema change 011: safety.

   What it adds and why (App Store guideline 1.2 / Google Play user-generated content policy, Terms of Service §8):
   - dbo.Reports: a user reporting a listing, review, chat or person (POST reports, SafetyStore). Read by the
     operator, who answers within 5 business days (docs/legal/README.md has the query). ReporterUid is cleared when
     the reporter deletes their account; reports ABOUT someone stay while they are being handled.
   - IX_Reports_Status: the operator's "open reports, oldest first".
   - dbo.Blocks: a user blocking another (PUT blocks/{uid}). Either direction hides the two from each other: no
     search result, no public profile, no Inbox row, no new message.
   - IX_Blocks_Blocked: finds who blocked the caller (search and the Inbox read both directions).

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

/* ---------- Reports: a user reporting another user's listing, review, chat or account ---------- */
IF OBJECT_ID(N'dbo.Reports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reports
    (
        Id          bigint            IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Reports PRIMARY KEY,
        ReporterUid varchar(128)      NULL,      -- null once the reporter deleted their account
        TargetUid   varchar(128)      NOT NULL,  -- the person reported
        Kind        varchar(10)       NOT NULL CONSTRAINT CK_Reports_Kind CHECK (Kind IN ('listing', 'review', 'chat', 'user')),
        SubjectId   varchar(260)      NULL,      -- the chat id for a chat report
        Reason      varchar(20)       NOT NULL CONSTRAINT CK_Reports_Reason CHECK (Reason IN ('scam', 'harassment', 'fake', 'inappropriate', 'employment', 'other')),
        Note        nvarchar(500)     NULL,
        Status      varchar(10)       NOT NULL CONSTRAINT DF_Reports_Status DEFAULT ('open')
                                      CONSTRAINT CK_Reports_Status CHECK (Status IN ('open', 'actioned', 'dismissed')),
        CreatedAt   datetimeoffset(3) NOT NULL,
        ResolvedAt  datetimeoffset(3) NULL
    );

    CREATE INDEX IX_Reports_Status ON dbo.Reports (Status, CreatedAt);
END;

/* ---------- Blocks: who blocked whom (either direction hides the two from each other) ---------- */
IF OBJECT_ID(N'dbo.Blocks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Blocks
    (
        BlockerUid  varchar(128)      NOT NULL,
        BlockedUid  varchar(128)      NOT NULL,
        CreatedAt   datetimeoffset(3) NOT NULL,
        CONSTRAINT PK_Blocks PRIMARY KEY (BlockerUid, BlockedUid),
        CONSTRAINT CK_Blocks_NotSelf CHECK (BlockerUid <> BlockedUid)
    );

    CREATE INDEX IX_Blocks_Blocked ON dbo.Blocks (BlockedUid);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '011-schema-safety')
    INSERT dbo.SchemaChanges (Script) VALUES ('011-schema-safety');

COMMIT TRANSACTION;
