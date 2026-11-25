/* =====================================================================================
   SideGigPh schema change 035: user-restore.

   What it adds and why: a household's setup lived only on the phone (Android backup is off), so after a reinstall or
   on a new phone a household answered the whole interview again, and someone on both sides came back as a worker only.
   The app now reads it back after sign-in (AccountRestore, GET users/me):
     - Users.SeekerFinishedAt (datetimeoffset(3), NULL): when the account first finished the household interview; kept,
       never cleared by a later save. NULL = the household side was never set up.
     - Users.HomeTownId (varchar(40), NULL): the household's one home town (a 'towns' id in dbo.ConfigOptions). Only a
       served town is saved (UserStore).
     - Users.HomeBarangayId (varchar(40), NULL): its barangay (dbo.Barangays), optional, only one of that town's.
   Households that already posted a request or started a chat get SeekerFinishedAt (their account's creation time),
   and those who posted get their latest post's town and barangay as their home, so they come back too.

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

/* ---------- Users.SeekerFinishedAt: the household side is set up ---------- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Users', N'SeekerFinishedAt') IS NULL
    EXEC (N'ALTER TABLE dbo.Users ADD SeekerFinishedAt datetimeoffset(3) NULL;');

/* ---------- Users.HomeTownId / HomeBarangayId: the household's home ---------- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Users', N'HomeTownId') IS NULL
    EXEC (N'ALTER TABLE dbo.Users ADD HomeTownId varchar(40) NULL;');

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Users', N'HomeBarangayId') IS NULL
    EXEC (N'ALTER TABLE dbo.Users ADD HomeBarangayId varchar(40) NULL;');

/* ---------- Households that already posted or chatted finished the interview ---------- */
IF COL_LENGTH(N'dbo.Users', N'SeekerFinishedAt') IS NOT NULL AND OBJECT_ID(N'dbo.JobPosts', N'U') IS NOT NULL
    EXEC (N'UPDATE u SET SeekerFinishedAt = u.CreatedAt
            FROM dbo.Users AS u
            WHERE u.SeekerFinishedAt IS NULL
              AND (EXISTS (SELECT 1 FROM dbo.JobPosts AS p WHERE p.SeekerUid = u.Uid)
                   OR EXISTS (SELECT 1 FROM dbo.Chats AS c WHERE c.SeekerId = u.Uid));');

/* ---------- Their home: the town and barangay of their latest post ---------- */
IF COL_LENGTH(N'dbo.Users', N'HomeTownId') IS NOT NULL AND COL_LENGTH(N'dbo.JobPosts', N'BarangayId') IS NOT NULL
    EXEC (N'UPDATE u SET HomeTownId = latest.CityId, HomeBarangayId = latest.BarangayId
            FROM dbo.Users AS u
            CROSS APPLY (SELECT TOP (1) p.CityId, p.BarangayId FROM dbo.JobPosts AS p
                         WHERE p.SeekerUid = u.Uid ORDER BY p.CreatedAt DESC) AS latest
            WHERE u.HomeTownId IS NULL;');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '035-schema-user-restore')
    INSERT dbo.SchemaChanges (Script) VALUES ('035-schema-user-restore');

COMMIT TRANSACTION;
