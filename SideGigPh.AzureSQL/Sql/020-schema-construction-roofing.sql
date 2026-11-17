/* =====================================================================================
   SideGigPh schema change 020: Roofing & Gutter moves under Construction too (data only, no new objects).

   What it changes and why: 004-config.sql now retires the trade 'roofing' and adds the Construction job
   'construction-roofing'. Every row naming the old trade moves under Construction, as 019 did for Plumber and Welder:
   - dbo.Professionals.CategoryId 'roofing' -> 'construction'
   - dbo.ProfessionalServices: any 'roofing-*' job -> one 'construction-roofing' row per worker
   - dbo.JobPosts (trade and job), dbo.Chats.ServiceId, dbo.Reviews.ServiceId: the same job mapping.
   dbo.OtherJobNotes keeps what people typed (analytics history).

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

/* ---------- Workers: their trade, then their jobs ---------- */
IF OBJECT_ID(N'dbo.Professionals', N'U') IS NOT NULL
    EXEC (N'UPDATE dbo.Professionals SET CategoryId = ''construction'' WHERE CategoryId = ''roofing'';');

IF OBJECT_ID(N'dbo.ProfessionalServices', N'U') IS NOT NULL
    EXEC (N'
        INSERT dbo.ProfessionalServices (ProfessionalId, ServiceId)
        SELECT DISTINCT old.ProfessionalId, ''construction-roofing''
        FROM dbo.ProfessionalServices AS old
        WHERE old.ServiceId LIKE ''roofing-%''
          AND NOT EXISTS (SELECT 1 FROM dbo.ProfessionalServices AS done
                          WHERE done.ProfessionalId = old.ProfessionalId AND done.ServiceId = ''construction-roofing'');
        DELETE dbo.ProfessionalServices WHERE ServiceId LIKE ''roofing-%'';');

/* ---------- Requests households posted ---------- */
IF OBJECT_ID(N'dbo.JobPosts', N'U') IS NOT NULL
    EXEC (N'UPDATE dbo.JobPosts SET CategoryId = ''construction'', ServiceId = ''construction-roofing'' WHERE CategoryId = ''roofing'';');

/* ---------- The job a chat or review was about ---------- */
IF OBJECT_ID(N'dbo.Chats', N'U') IS NOT NULL
    EXEC (N'UPDATE dbo.Chats SET ServiceId = ''construction-roofing'' WHERE ServiceId LIKE ''roofing-%'';');

IF OBJECT_ID(N'dbo.Reviews', N'U') IS NOT NULL
    EXEC (N'UPDATE dbo.Reviews SET ServiceId = ''construction-roofing'' WHERE ServiceId LIKE ''roofing-%'';');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '020-schema-construction-roofing')
    INSERT dbo.SchemaChanges (Script) VALUES ('020-schema-construction-roofing');

COMMIT TRANSACTION;
