/* =====================================================================================
   SideGigPh schema change 032: app-events.

   What it adds and why:
     - dbo.app_events: our own usage events, one row each (no Google or Firebase Analytics). Business events are
       written by the API in the same code path that changes the real data (source 'server': sign_up,
       request_created, request_matched, job_completed, rehire, worker_profile_complete, worker_first_job,
       report_user, block_user); the app's searches are queued on the phone and uploaded later (source 'app':
       search, search_empty, POST events). No personal data: user_id is the account's opaque id (cleared by Delete
       account), area is a town id. event_id is unique, so an upload sent twice is kept once. Rows older than 12
       months are deleted (AppEventStore.KeepFor). Weekly questions: analytics/weekly.sql.
     - dbo.v_events: the same rows with the event time in Philippine time (UTC+8) and the Monday that starts its week.

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

/* ---------- app_events: usage events, no personal data ---------- */
IF OBJECT_ID(N'dbo.app_events', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.app_events
    (
        id                bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_app_events PRIMARY KEY,
        event_id          uniqueidentifier NOT NULL,   -- made by the phone (app) or the API (server); a retry is kept once
        event_name        varchar(40)      NOT NULL,
        source            varchar(10)      NOT NULL,   -- 'app' | 'server'
        user_id           varchar(64)      NULL,       -- the account's opaque id only; NULL after Delete account
        user_type         varchar(10)      NULL,       -- 'customer' | 'worker'
        session_id        varchar(64)      NULL,
        service_category  varchar(40)      NULL,       -- a trade's stable id: 'aircon', 'construction'
        area              varchar(60)      NULL,       -- a town's stable id: 'angono'
        search_term       nvarchar(100)    NULL,
        result_count      int              NULL,
        minutes_to_match  int              NULL,
        reason            varchar(40)      NULL,       -- a report's fixed reason
        request_id        varchar(64)      NULL,       -- the job post's id: links request -> match -> job
        app_version       varchar(20)      NULL,
        client_ts         datetime2        NULL,       -- when it happened on the phone (UTC), app events only
        server_ts         datetime2        NOT NULL CONSTRAINT DF_app_events_server_ts DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_app_events_event_id UNIQUE (event_id)
    );
END;

IF OBJECT_ID(N'dbo.app_events', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_app_events_name_time' AND object_id = OBJECT_ID(N'dbo.app_events'))
    EXEC (N'CREATE INDEX IX_app_events_name_time ON dbo.app_events (event_name, server_ts);');

IF OBJECT_ID(N'dbo.app_events', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_app_events_user' AND object_id = OBJECT_ID(N'dbo.app_events'))
    EXEC (N'CREATE INDEX IX_app_events_user ON dbo.app_events (user_id, server_ts);');

/* ---------- v_events: Philippine time (UTC+8) and Monday-start weeks, for the reports ---------- */
IF OBJECT_ID(N'dbo.v_events', N'V') IS NULL
    EXEC (N'CREATE VIEW dbo.v_events AS
SELECT e.*,
       DATEADD(HOUR, 8, COALESCE(e.client_ts, e.server_ts)) AS event_ts_ph,
       DATEADD(DAY,
               -(DATEDIFF(DAY, ''19000101'', DATEADD(HOUR, 8, COALESCE(e.client_ts, e.server_ts))) % 7),
               CAST(DATEADD(HOUR, 8, COALESCE(e.client_ts, e.server_ts)) AS date)) AS week_start_ph
FROM dbo.app_events AS e;');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '032-schema-app-events')
    INSERT dbo.SchemaChanges (Script) VALUES ('032-schema-app-events');

COMMIT TRANSACTION;
