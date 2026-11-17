/* =====================================================================================
   SideGigPh schema change 022: Electrician, Carpentry and Masonry move under Construction (data only, no new objects).

   What it changes and why: 004-config.sql now retires the trades 'electrical', 'carpentry' and 'masonry_tiling'
   and adds the Construction job 'construction-electrical' (Carpentry and Masonry were already Construction jobs).
   Every row naming the old trades moves under Construction, as 019 and 020 did for Plumber, Welder and Roofing:
   - dbo.Professionals.CategoryId 'electrical' / 'carpentry' / 'masonry_tiling' -> 'construction'
   - dbo.ProfessionalServices: any 'electrical-*' job -> 'construction-electrical', 'carpentry-*' ->
     'construction-carpentry', 'masonry-*' -> 'construction-masonry' (one row each, since a worker had several)
   - dbo.JobPosts (trade and job), dbo.Chats.ServiceId, dbo.Reviews.ServiceId: the same job mapping.
   dbo.OtherJobNotes keeps what people typed under the old trades (analytics history).

   How: run AFTER every earlier schema script. The deploy runs 004-config.sql after it (tick "Also run
   004-config.sql"), which adds the Construction row these point at. Safe to run again: each step only touches
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
    EXEC (N'UPDATE dbo.Professionals SET CategoryId = ''construction'' WHERE CategoryId IN (''electrical'', ''carpentry'', ''masonry_tiling'');');

IF OBJECT_ID(N'dbo.ProfessionalServices', N'U') IS NOT NULL
    EXEC (N'
        INSERT dbo.ProfessionalServices (ProfessionalId, ServiceId)
        SELECT DISTINCT moved.ProfessionalId, moved.NewServiceId
        FROM (SELECT old.ProfessionalId,
                     CASE WHEN old.ServiceId LIKE ''electrical-%'' THEN ''construction-electrical''
                          WHEN old.ServiceId LIKE ''carpentry-%'' THEN ''construction-carpentry''
                          ELSE ''construction-masonry'' END AS NewServiceId
              FROM dbo.ProfessionalServices AS old
              WHERE old.ServiceId LIKE ''electrical-%'' OR old.ServiceId LIKE ''carpentry-%'' OR old.ServiceId LIKE ''masonry-%'') AS moved
        WHERE NOT EXISTS (SELECT 1 FROM dbo.ProfessionalServices AS done
                          WHERE done.ProfessionalId = moved.ProfessionalId AND done.ServiceId = moved.NewServiceId);
        DELETE dbo.ProfessionalServices
        WHERE ServiceId LIKE ''electrical-%'' OR ServiceId LIKE ''carpentry-%'' OR ServiceId LIKE ''masonry-%'';');

/* ---------- Requests households posted ---------- */
IF OBJECT_ID(N'dbo.JobPosts', N'U') IS NOT NULL
    EXEC (N'
        UPDATE dbo.JobPosts SET CategoryId = ''construction'', ServiceId = ''construction-electrical'' WHERE CategoryId = ''electrical'';
        UPDATE dbo.JobPosts SET CategoryId = ''construction'', ServiceId = ''construction-carpentry'' WHERE CategoryId = ''carpentry'';
        UPDATE dbo.JobPosts SET CategoryId = ''construction'', ServiceId = ''construction-masonry'' WHERE CategoryId = ''masonry_tiling'';');

/* ---------- The job a chat or review was about ---------- */
IF OBJECT_ID(N'dbo.Chats', N'U') IS NOT NULL
    EXEC (N'
        UPDATE dbo.Chats SET ServiceId = ''construction-electrical'' WHERE ServiceId LIKE ''electrical-%'';
        UPDATE dbo.Chats SET ServiceId = ''construction-carpentry'' WHERE ServiceId LIKE ''carpentry-%'';
        UPDATE dbo.Chats SET ServiceId = ''construction-masonry'' WHERE ServiceId LIKE ''masonry-%'';');

IF OBJECT_ID(N'dbo.Reviews', N'U') IS NOT NULL
    EXEC (N'
        UPDATE dbo.Reviews SET ServiceId = ''construction-electrical'' WHERE ServiceId LIKE ''electrical-%'';
        UPDATE dbo.Reviews SET ServiceId = ''construction-carpentry'' WHERE ServiceId LIKE ''carpentry-%'';
        UPDATE dbo.Reviews SET ServiceId = ''construction-masonry'' WHERE ServiceId LIKE ''masonry-%'';');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '022-schema-construction-trades')
    INSERT dbo.SchemaChanges (Script) VALUES ('022-schema-construction-trades');

COMMIT TRANSACTION;
