/* =====================================================================================
   SideGigPh schema change 019: the Construction trade takes in Plumber and Welder (data only, no new objects).

   What it changes and why: 004-config.sql adds the trade 'construction' (Foreman, Carpentry, Plumbing, Masonry,
   Tiles, Welding, Steelman, Helper, Others) and retires the trades 'plumbing' and 'welder'. So nobody is left
   on a trade the app no longer shows, every row naming them moves under Construction:
   - dbo.Professionals.CategoryId 'plumbing' / 'welder'  -> 'construction'
   - dbo.ProfessionalServices: any 'plumbing-*' job -> 'construction-plumbing', any 'welder-*' -> 'construction-welding'
     (one row each, since a worker had several)
   - dbo.JobPosts (trade and job), dbo.Chats.ServiceId, dbo.Reviews.ServiceId: the same job mapping.
   dbo.OtherJobNotes keeps what people typed under the old trades (analytics history).

   How: run AFTER every earlier schema script. The deploy runs 004-config.sql after it (tick "Also run
   004-config.sql"), which adds the Construction rows these point at. Safe to run again: each step only touches
   rows still naming the old ids.
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

/* ---------- Workers: their trade, then their jobs (one Construction job per old trade) ---------- */
IF OBJECT_ID(N'dbo.Professionals', N'U') IS NOT NULL
    EXEC (N'UPDATE dbo.Professionals SET CategoryId = ''construction'' WHERE CategoryId IN (''plumbing'', ''welder'');');

IF OBJECT_ID(N'dbo.ProfessionalServices', N'U') IS NOT NULL
    EXEC (N'
        INSERT dbo.ProfessionalServices (ProfessionalId, ServiceId)
        SELECT DISTINCT old.ProfessionalId,
               CASE WHEN old.ServiceId LIKE ''plumbing-%'' THEN ''construction-plumbing'' ELSE ''construction-welding'' END
        FROM dbo.ProfessionalServices AS old
        WHERE (old.ServiceId LIKE ''plumbing-%'' OR old.ServiceId LIKE ''welder-%'')
          AND NOT EXISTS (SELECT 1 FROM dbo.ProfessionalServices AS done
                          WHERE done.ProfessionalId = old.ProfessionalId
                            AND done.ServiceId = CASE WHEN old.ServiceId LIKE ''plumbing-%'' THEN ''construction-plumbing'' ELSE ''construction-welding'' END);
        DELETE dbo.ProfessionalServices WHERE ServiceId LIKE ''plumbing-%'' OR ServiceId LIKE ''welder-%'';');

/* ---------- Requests households posted ---------- */
IF OBJECT_ID(N'dbo.JobPosts', N'U') IS NOT NULL
    EXEC (N'
        UPDATE dbo.JobPosts SET CategoryId = ''construction'', ServiceId = ''construction-plumbing'' WHERE CategoryId = ''plumbing'';
        UPDATE dbo.JobPosts SET CategoryId = ''construction'', ServiceId = ''construction-welding'' WHERE CategoryId = ''welder'';');

/* ---------- The job a chat or review was about ---------- */
IF OBJECT_ID(N'dbo.Chats', N'U') IS NOT NULL
    EXEC (N'
        UPDATE dbo.Chats SET ServiceId = ''construction-plumbing'' WHERE ServiceId LIKE ''plumbing-%'';
        UPDATE dbo.Chats SET ServiceId = ''construction-welding'' WHERE ServiceId LIKE ''welder-%'';');

IF OBJECT_ID(N'dbo.Reviews', N'U') IS NOT NULL
    EXEC (N'
        UPDATE dbo.Reviews SET ServiceId = ''construction-plumbing'' WHERE ServiceId LIKE ''plumbing-%'';
        UPDATE dbo.Reviews SET ServiceId = ''construction-welding'' WHERE ServiceId LIKE ''welder-%'';');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '019-schema-construction-trade')
    INSERT dbo.SchemaChanges (Script) VALUES ('019-schema-construction-trade');

COMMIT TRANSACTION;
