/* =====================================================================================
   SideGigPh schema change 014: worker verification (the ID Verified, TESDA Certified and developer badges).

   What it adds and why (a worker sends proof, a person checks it, the badge shows on the listing):
   - dbo.WorkerVerifications: one request per proof a worker sent (POST verifications/{kind}, VerificationStore).
       Kind         id (a government ID), tesda (a National Certificate) or developer (worked on a named developer's
                    project: BadgeId says which, ProjectName the building when given).
       Status       pending until the operator decides, then approved (the badge) or rejected.
       DocumentBlob the proof's name in the PRIVATE "verification" blob container. Cleared when the operator decides:
                    the file is deleted then and only the result is kept (Data Privacy Act: keep the least).
       Scan…        what Azure AI Document Intelligence read from an ID (name, number, expiry), to help the operator.
                    Cleared with the file. Never shown to anyone but the operator.
       ConsentAt    when the worker ticked the consent box for this upload (required).
   - UX_WorkerVerifications_Pending: one open request per worker per badge.
   - IX_WorkerVerifications_Uid: a worker's own requests, and the approved badges on listings.
   - IX_WorkerVerifications_Status: the operator's queue, oldest first.
   - dbo.VerificationAudit: who did what to a request and when (submitted, scanned, viewed, approved, rejected,
     deleted): the audit log of every look at a sensitive file. Rows stay when the request is deleted.

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

/* ---------- WorkerVerifications: a proof a worker sent for a badge ---------- */
IF OBJECT_ID(N'dbo.WorkerVerifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WorkerVerifications
    (
        Id             bigint            IDENTITY(1, 1) NOT NULL CONSTRAINT PK_WorkerVerifications PRIMARY KEY,
        Uid            varchar(128)      NOT NULL,
        Kind           varchar(12)       NOT NULL CONSTRAINT CK_WorkerVerifications_Kind CHECK (Kind IN ('id', 'tesda', 'developer')),
        BadgeId        varchar(40)       NOT NULL CONSTRAINT DF_WorkerVerifications_BadgeId DEFAULT (''),  -- developer: ayala-land, megaworld…; else ''
        ProjectName    nvarchar(100)     NULL,      -- developer: the building or condo, when given
        Status         varchar(10)       NOT NULL CONSTRAINT DF_WorkerVerifications_Status DEFAULT ('pending')
                                         CONSTRAINT CK_WorkerVerifications_Status CHECK (Status IN ('pending', 'approved', 'rejected')),
        DocumentBlob   varchar(200)      NULL,      -- private container; null once deleted
        ScanStatus     varchar(12)       NOT NULL CONSTRAINT DF_WorkerVerifications_ScanStatus DEFAULT ('none')
                                         CONSTRAINT CK_WorkerVerifications_ScanStatus CHECK (ScanStatus IN ('none', 'read', 'unreadable', 'failed')),
        ScannedType    varchar(60)       NULL,
        ScannedName    nvarchar(120)     NULL,
        ScannedNumber  nvarchar(60)      NULL,
        ScannedExpiry  date              NULL,
        ConsentAt      datetimeoffset(3) NOT NULL,
        CreatedAt      datetimeoffset(3) NOT NULL,
        DecidedAt      datetimeoffset(3) NULL,
        DecidedBy      varchar(128)      NULL,
        RejectReason   nvarchar(200)     NULL
    );

    CREATE UNIQUE INDEX UX_WorkerVerifications_Pending ON dbo.WorkerVerifications (Uid, Kind, BadgeId) WHERE Status = 'pending';
    CREATE INDEX IX_WorkerVerifications_Uid ON dbo.WorkerVerifications (Uid, Status) INCLUDE (Kind, BadgeId, ProjectName);
    CREATE INDEX IX_WorkerVerifications_Status ON dbo.WorkerVerifications (Status, CreatedAt);
END;

/* ---------- VerificationAudit: every action on a request, and every look at its file ---------- */
IF OBJECT_ID(N'dbo.VerificationAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VerificationAudit
    (
        Id              bigint            IDENTITY(1, 1) NOT NULL CONSTRAINT PK_VerificationAudit PRIMARY KEY,
        VerificationId  bigint            NOT NULL,   -- no foreign key: the log outlives the request
        ActorUid        varchar(128)      NOT NULL,
        Action          varchar(12)       NOT NULL CONSTRAINT CK_VerificationAudit_Action
                                          CHECK (Action IN ('submitted', 'scanned', 'viewed', 'approved', 'rejected', 'deleted')),
        At              datetimeoffset(3) NOT NULL
    );

    CREATE INDEX IX_VerificationAudit_Verification ON dbo.VerificationAudit (VerificationId, At);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '014-schema-verification')
    INSERT dbo.SchemaChanges (Script) VALUES ('014-schema-verification');

COMMIT TRANSACTION;
