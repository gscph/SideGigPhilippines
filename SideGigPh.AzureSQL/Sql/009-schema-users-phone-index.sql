/* =====================================================================================
   SideGigPh schema change 009: users-phone-index.

   What it adds and why:
   - IX_Users_PhoneNumber on dbo.Users (PhoneNumber) INCLUDE (Role), only rows with a number: publishing a listing
     checks that no other account uses the number as a Job Seeker (anti-fraud rule 1,
     ProfessionalStore.PublishAsync). Without it that check reads every user.

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

/* ---------- Users: find an account by its verified number ---------- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_PhoneNumber' AND object_id = OBJECT_ID(N'dbo.Users'))
    EXEC (N'CREATE INDEX IX_Users_PhoneNumber ON dbo.Users (PhoneNumber) INCLUDE (Role) WHERE PhoneNumber IS NOT NULL;');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '009-schema-users-phone-index')
    INSERT dbo.SchemaChanges (Script) VALUES ('009-schema-users-phone-index');

COMMIT TRANSACTION;
