/* =====================================================================================
   SideGigPh schema change 028: meet-location.

   (027 is taken by the post-title branch.)

   What it adds and why (once a household picks an offer, the worker has to find the house; until now the chat
   was the only way to say where):
   - dbo.JobOffers.MeetLatitude / MeetLongitude: the exact point the household chose to send to the worker it
     picked (WGS 84, from the phone's precise location, sent only when the household taps "Send my location").
     PRIVATE: only that household and that worker get it, and only while the picked job is open. It is cleared
     when the job is called off or both mark it done.
   - dbo.JobOffers.MeetSharedAt: when it was sent (the worker's card says "sent 5 min ago").
   - Chat message kind 'pin' (dbo.ChatMessages.Kind, dbo.Chats.LastMessageKind): "Customer's location" in the chat,
     sent by the household when it sends its place. It holds NO coordinates (Text is null, like a call): a tap opens
     the place kept on the picked job, so deleting that point leaves nothing behind in the chat.

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

/* ---------- JobOffers: the point the household sent the picked worker ---------- */
IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobOffers', N'MeetLatitude') IS NULL
    EXEC (N'ALTER TABLE dbo.JobOffers ADD
              MeetLatitude  float NULL CONSTRAINT CK_JobOffers_MeetLatitude  CHECK (MeetLatitude  BETWEEN -90 AND 90),
              MeetLongitude float NULL CONSTRAINT CK_JobOffers_MeetLongitude CHECK (MeetLongitude BETWEEN -180 AND 180),
              CONSTRAINT CK_JobOffers_MeetLatLng CHECK ((MeetLatitude IS NULL AND MeetLongitude IS NULL) OR (MeetLatitude IS NOT NULL AND MeetLongitude IS NOT NULL));');

IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.JobOffers', N'MeetSharedAt') IS NULL
    EXEC (N'ALTER TABLE dbo.JobOffers ADD MeetSharedAt datetimeoffset(3) NULL;');

/* ---------- Chat message kind 'pin' (the old checks knew only text / call) ---------- */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ChatMessages_Kind' AND definition NOT LIKE N'%pin%')
    EXEC (N'ALTER TABLE dbo.ChatMessages DROP CONSTRAINT CK_ChatMessages_Kind;');

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.CK_ChatMessages_Kind') IS NULL
    EXEC (N'ALTER TABLE dbo.ChatMessages ADD CONSTRAINT CK_ChatMessages_Kind CHECK (Kind IN (''text'', ''call'', ''pin''));');

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ChatMessages_Text' AND definition NOT LIKE N'%pin%')
    EXEC (N'ALTER TABLE dbo.ChatMessages DROP CONSTRAINT CK_ChatMessages_Text;');

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.CK_ChatMessages_Text') IS NULL
    EXEC (N'ALTER TABLE dbo.ChatMessages ADD CONSTRAINT CK_ChatMessages_Text CHECK ((Kind IN (''call'', ''pin'') AND Text IS NULL) OR (Kind = ''text'' AND Text IS NOT NULL));');

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Chats_LastMessageKind' AND definition NOT LIKE N'%pin%')
    EXEC (N'ALTER TABLE dbo.Chats DROP CONSTRAINT CK_Chats_LastMessageKind;');

IF OBJECT_ID(N'dbo.Chats', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.CK_Chats_LastMessageKind') IS NULL
    EXEC (N'ALTER TABLE dbo.Chats ADD CONSTRAINT CK_Chats_LastMessageKind CHECK (LastMessageKind IN (''text'', ''call'', ''pin''));');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '028-schema-meet-location')
    INSERT dbo.SchemaChanges (Script) VALUES ('028-schema-meet-location');

COMMIT TRANSACTION;
