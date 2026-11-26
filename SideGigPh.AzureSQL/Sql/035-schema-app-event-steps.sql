/* =====================================================================================
   SideGigPh schema change 035: app-event-steps.

   What it adds and why:
     - app_events.step: the sign-up step an 'onboarding_step' event names ('role', 'pro_skills' … 'publish_ready',
       'seeker_category' … 'seeker_finding'; AppEventSteps), so the funnel shows where people stop.
     - app_events.service_id: the job (a service's stable id, 'aircon-cleaning') of a search, a post form opened
       ('post_started') or a post ('request_created'). A tapped search has no typed words, so the trade alone
       ('aircon') said too little about what people look for.
     - dbo.v_events selects e.*, which SQL Server fixes when the view is made: it is refreshed so it has both.

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

/* ---------- app_events: the sign-up step, and the job ---------- */
IF OBJECT_ID(N'dbo.app_events', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.app_events', N'step') IS NULL
    EXEC (N'ALTER TABLE dbo.app_events ADD step varchar(40) NULL;');

IF OBJECT_ID(N'dbo.app_events', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.app_events', N'service_id') IS NULL
    EXEC (N'ALTER TABLE dbo.app_events ADD service_id varchar(60) NULL;');

/* ---------- v_events: pick up the new columns ---------- */
IF OBJECT_ID(N'dbo.v_events', N'V') IS NOT NULL
    EXEC sp_refreshview N'dbo.v_events';

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '035-schema-app-event-steps')
    INSERT dbo.SchemaChanges (Script) VALUES ('035-schema-app-event-steps');

COMMIT TRANSACTION;
