/* =====================================================================================
   SideGigPh: SAMPLE data for testing SEARCH RANKING (docs/search-ranking-test-plan.md).

   Each trade tests one thing. Its workers are identical except for that one thing, so the order you
   see is caused by it alone:
     S1 Construction     the job is a hard filter
     S2 Electrician      the town: your town first, "Other towns nearby" after, unserved towns hidden
     S3 Carpentry        reviews: stars (smoothed) + how many
     S4 Errands          responsiveness: reply rate + reply speed
     S5 Masonry          activity: last opened the app
     S6 Car Mechanic     tenure: how long on the app
     S7 Roofing          budget fit (the three rates, "depends on the job", and two listings from old app versions)
     S8 Solar            experience
     S9 Aircon           everything together, with paging (4 per page) and the nearby group
   The ordinary worker (unless a scenario changes it): Taytay, the job's AVERAGE price (rate 'average', from the
   job's price guide in dbo.Services), 1–3 years, no reviews, no chats, joined 90 days ago, last active yesterday.
   Names start with their scenario ("S4 …") so you can see at a glance which test a card belongs to.

   Rates are what the app saves now: a price level of each job's guide ('minimum', 'average', 'above-average')
   or 'quote' (depends on the job). Only S7's two "Old App" workers keep an old price tier id ('500-1000',
   '5000-up'), the way listings published before price guides still look. The sample chats carry the budget a
   Seeker gets first for the job: the "Tipid" range, the job's lowest to average price ("755-1000").

   SEED TAG: every id starts 'seed-s2-' (the older sample data used 'seed-s1-'). Only those rows are
   touched; real users and their listings never are.
   Numbers use the unassigned +63 900 2xx xxxx range, so a Call never reaches a real person.
   These aren't Firebase accounts: nobody can sign in as them, and a chat with one never gets a reply.
   No photos (there are no blobs for them): cards show initials, and the profile shows 1 reference.

   Needs: 001-schema.sql, 005-schema-ranking-signals.sql, 007-schema-config-tables.sql,
   008-schema-service-prices.sql and 004-config.sql already run (the script stops with a message if the price
   guides are missing). 011-schema-safety.sql is optional: when it has run, blocks and reports about these
   workers are removed with them.
   How: portal Query editor, run the WHOLE file. Safe to run again: it deletes its own rows, then
   inserts them fresh with times relative to now (so "active yesterday" stays yesterday).
   To remove it: set @RemoveOnly = 1 below and run it.

   Search caches: the API keeps a trade's listings 30 s and the app keeps a result page 60 s. After
   running this, wait a minute (or restart the Function App) before searching.
   ===================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @RemoveOnly bit = 0;   -- 1 = only delete this seed's rows

DECLARE @now datetimeoffset(3) = SYSDATETIMEOFFSET();
DECLARE @Tag varchar(20) = 'seed-s2-';
DECLARE @TagPattern varchar(21) = @Tag + '%';

BEGIN TRANSACTION;

/* ---------- Remove this seed's rows (chats real Seekers started with these workers go too) ---------- */
DELETE FROM dbo.Chats WHERE ProfessionalId LIKE @TagPattern OR SeekerId LIKE @TagPattern;  -- messages cascade
DELETE FROM dbo.Reviews WHERE ProfessionalId LIKE @TagPattern OR SeekerId LIKE @TagPattern;
DELETE FROM dbo.Professionals WHERE Id LIKE @TagPattern;                                     -- jobs, towns, photos, references cascade
DELETE FROM dbo.Users WHERE Uid LIKE @TagPattern;

-- Blocks and reports real users made about these workers (011-schema-safety.sql, when it has run).
IF OBJECT_ID(N'dbo.Blocks', N'U') IS NOT NULL
    DELETE FROM dbo.Blocks WHERE BlockerUid LIKE @TagPattern OR BlockedUid LIKE @TagPattern;
IF OBJECT_ID(N'dbo.Reports', N'U') IS NOT NULL
    DELETE FROM dbo.Reports WHERE TargetUid LIKE @TagPattern OR ReporterUid LIKE @TagPattern;

IF @RemoveOnly = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT 'Search ranking seed removed.';
    RETURN;
END;

/* ---------- Each job's "Tipid" budget (lowest to average price), from the price guides ----------
   Read through EXEC so a database without 008-schema-service-prices.sql gets the message below instead of
   "Invalid column name". */
IF COL_LENGTH(N'dbo.Services', N'PriceMin') IS NULL
    THROW 50001, 'dbo.Services has no price guides: run 008-schema-service-prices.sql, then 004-config.sql, then this script.', 1;

DECLARE @Budgets TABLE (ServiceId varchar(40) NOT NULL PRIMARY KEY, BudgetId varchar(40) NOT NULL);
INSERT INTO @Budgets (ServiceId, BudgetId)
EXEC (N'SELECT Id, CONCAT(PriceMin, ''-'', PriceAverage) FROM dbo.Services WHERE IsActive = 1 AND PriceMin IS NOT NULL;');

/* ---------- The workers ----------
   Rate: 'minimum' / 'average' / 'above-average' = that point of each listed job's price guide, 'quote' = depends
   on the job; '500-1000' / '5000-up' = an old price tier (a listing from before price guides).
   Reviews: how many 5/4/3/2/1-star reviews. Chats (all started 2–20 days ago unless "Fresh"):
     Answered  the worker replied in the chat after ReplyMinutes
     Ignored   the Seeker wrote, the worker never replied
     CallOnly  the Seeker only tapped Call (not counted: they may have talked on the phone)
     Fresh     the Seeker wrote 2 hours ago, no reply yet (not counted until a day has passed) */
DECLARE @Pros TABLE
(
    Num           int           NOT NULL PRIMARY KEY,
    Code          varchar(30)   NOT NULL,
    Name          nvarchar(60)  NOT NULL,
    CategoryId    varchar(40)   NOT NULL,
    ServiceIds    varchar(200)  NOT NULL,
    CityIds       varchar(200)  NOT NULL,
    ExperienceId  varchar(40)   NOT NULL,
    RateTierId    varchar(40)   NOT NULL,
    JoinedDays    int           NOT NULL,
    ActiveDays    int           NOT NULL,
    R5 int NOT NULL, R4 int NOT NULL, R3 int NOT NULL, R2 int NOT NULL, R1 int NOT NULL,
    Answered      int           NOT NULL,
    ReplyMinutes  int           NOT NULL,
    Ignored       int           NOT NULL,
    CallOnly      int           NOT NULL,
    Fresh         int           NOT NULL
);

INSERT INTO @Pros VALUES
--  #  code             name                                  trade             jobs                                    towns              exp        rate             joined active  5  4  3  2  1  ans  min  ign call fresh
-- S1 Construction: only workers who list the job
( 1, 's1-tubero',    N'S1 Tubero Specialist',              'construction',   'construction-plumbing',                'taytay',          '1-3',     'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
( 2, 's1-welder',    N'S1 Welder Specialist',              'construction',   'construction-welding',                 'taytay',          '10-plus', 'minimum',       400,  0,  20, 0, 0, 0, 0, 10,  10,  0,  0,  0),
( 3, 's1-both',      N'S1 Tubero at Welder',               'construction',   'construction-plumbing,construction-welding', 'taytay',    '1-3',     'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
-- S2 Electrician: town groups
( 4, 's2-taytay',    N'S2 Taytay Baguhan',                 'electrical',     'electrical-no-power',                  'taytay',          'under-1', 'average',        10, 20,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
( 5, 's2-multi',     N'S2 Taytay at Antipolo',             'electrical',     'electrical-no-power',                  'taytay,antipolo', '1-3',     'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
( 6, 's2-antipolo',  N'S2 Antipolo Star',                  'electrical',     'electrical-no-power',                  'antipolo',        '10-plus', 'average',       400,  0,  30, 0, 0, 0, 0, 12,  10,  0,  0,  0),
( 7, 's2-angono',    N'S2 Angono Kuryente',                'electrical',     'electrical-no-power',                  'angono',          '3-10',    'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
( 8, 's2-unserved',  N'S2 Binangonan Lang',                'electrical',     'electrical-no-power',                  'binangonan',      '10-plus', 'average',       400,  0,  30, 0, 0, 0, 0, 12,  10,  0,  0,  0),
-- S3 Carpentry: reviews
( 9, 's3-steady',    N'S3 Steady 4.8 (40 reviews)',        'carpentry',      'carpentry-furniture',                  'taytay',          '1-3',     'average',        90,  1,  32, 8, 0, 0, 0,  0,   0,  0,  0,  0),
(10, 's3-lucky',     N'S3 Lucky 5.0 (1 review)',           'carpentry',      'carpentry-furniture',                  'taytay',          '1-3',     'average',        90,  1,   1, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(11, 's3-new',       N'S3 Bago (no reviews)',              'carpentry',      'carpentry-furniture',                  'taytay',          '1-3',     'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(12, 's3-poor',      N'S3 Poor 2.0 (10 reviews)',          'carpentry',      'carpentry-furniture',                  'taytay',          '1-3',     'average',        90,  1,   0, 0, 0,10, 0,  0,   0,  0,  0,  0),
-- S4 Errands: responsiveness
(13, 's4-fast',      N'S4 Mabilis Sumagot (10 min)',       'errand_runner',  'errand-house-cleaning',                'taytay',          '1-3',     'average',        90,  1,   0, 0, 0, 0, 0, 10,  10,  0,  0,  0),
(14, 's4-slow',      N'S4 Matagal Sumagot (5 hrs)',        'errand_runner',  'errand-house-cleaning',                'taytay',          '1-3',     'average',        90,  1,   0, 0, 0, 0, 0, 10, 300,  0,  0,  0),
(15, 's4-new',       N'S4 Bago (no chats yet)',            'errand_runner',  'errand-house-cleaning',                'taytay',          '1-3',     'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(16, 's4-calls',     N'S4 Tawag Lang (calls only)',        'errand_runner',  'errand-house-cleaning',                'taytay',          '1-3',     'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  5,  0),
(17, 's4-fresh',     N'S4 Kaka-message (2 hrs ago)',       'errand_runner',  'errand-house-cleaning',                'taytay',          '1-3',     'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  3),
(18, 's4-ghost',     N'S4 Hindi Sumasagot (1 of 10)',      'errand_runner',  'errand-house-cleaning',                'taytay',          '1-3',     'average',        90,  1,   0, 0, 0, 0, 0,  1,  60,  9,  0,  0),
-- S5 Masonry: last active (all joined 120 days ago)
(19, 's5-today',     N'S5 Active Today',                   'masonry_tiling', 'masonry-tiles',                        'taytay',          '1-3',     'average',       120,  0,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(20, 's5-week',      N'S5 Active 1 Week Ago',              'masonry_tiling', 'masonry-tiles',                        'taytay',          '1-3',     'average',       120,  7,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(21, 's5-month',     N'S5 Active 1 Month Ago',             'masonry_tiling', 'masonry-tiles',                        'taytay',          '1-3',     'average',       120, 30,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(22, 's5-quarter',   N'S5 Active 3 Months Ago',            'masonry_tiling', 'masonry-tiles',                        'taytay',          '1-3',     'average',       120, 90,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
-- S6 Car Mechanic: tenure (all active today)
(23, 's6-year',      N'S6 Joined 1 Year Ago',              'car_mechanic',   'mechanic-engine',                      'taytay',          '1-3',     'average',       365,  0,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(24, 's6-quarter',   N'S6 Joined 3 Months Ago',            'car_mechanic',   'mechanic-engine',                      'taytay',          '1-3',     'average',        90,  0,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(25, 's6-new',       N'S6 Joined 3 Days Ago',              'car_mechanic',   'mechanic-engine',                      'taytay',          '1-3',     'average',         3,  0,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
-- S7 Roofing: budget fit (Roof leak repair guide: ₱1,000 lowest, ₱2,000 average, ₱3,500 above average)
(26, 's7-minimum',   N'S7 Lowest Price',                   'roofing',        'roofing-leak',                         'taytay',          '1-3',     'minimum',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(27, 's7-average',   N'S7 Average Price',                  'roofing',        'roofing-leak',                         'taytay',          '1-3',     'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(28, 's7-above',     N'S7 Above Average',                  'roofing',        'roofing-leak',                         'taytay',          '1-3',     'above-average',  90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(29, 's7-quote',     N'S7 Depends on the Job',             'roofing',        'roofing-leak',                         'taytay',          '1-3',     'quote',          90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(30, 's7-old-cheap', N'S7 Old App, Cheap Tier',            'roofing',        'roofing-leak',                         'taytay',          '1-3',     '500-1000',       90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(31, 's7-old-top',   N'S7 Old App, Top Tier',              'roofing',        'roofing-leak',                         'taytay',          '1-3',     '5000-up',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
-- S8 Solar: experience
(32, 's8-10plus',    N'S8 10+ Years',                      'solar_cleaning', 'solar-cleaning',                       'taytay',          '10-plus', 'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(33, 's8-3to10',     N'S8 3 to 10 Years',                  'solar_cleaning', 'solar-cleaning',                       'taytay',          '3-10',    'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(34, 's8-1to3',      N'S8 1 to 3 Years',                   'solar_cleaning', 'solar-cleaning',                       'taytay',          '1-3',     'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(35, 's8-under1',    N'S8 Under 1 Year',                   'solar_cleaning', 'solar-cleaning',                       'taytay',          'under-1', 'average',        90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
-- S9 Aircon: real-looking mix (Aircon cleaning guide: ₱755 lowest, ₱1,000 average, ₱1,500 above average)
(36, 's9-mangjun',   N'S9 Mang Jun Aircon',                'aircon',         'aircon-cleaning,aircon-repair',        'taytay',          '10-plus', 'average',       400,  0,  22, 3, 0, 0, 0,  8,  15,  0,  0,  0),
(37, 's9-kuyaben',   N'S9 Kuya Ben Cool',                  'aircon',         'aircon-cleaning,aircon-leak',          'taytay,antipolo', '3-10',    'minimum',       200,  2,   8, 3, 1, 0, 0,  6, 120,  2,  0,  0),
(38, 's9-rj',        N'S9 RJ Aircon (2 reviews)',          'aircon',         'aircon-cleaning',                      'taytay',          '1-3',     'above-average',  30,  0,   2, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(39, 's9-bago',      N'S9 Bagong Aircon Tech',             'aircon',         'aircon-cleaning',                      'taytay',          'under-1', 'average',         5,  0,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(40, 's9-tulog',     N'S9 Tulog Aircon (inactive)',        'aircon',         'aircon-cleaning,aircon-freon',         'taytay',          '10-plus', 'average',       500, 45,  21, 9, 0, 0, 0,  2,  90, 10,  0,  0),
(41, 's9-mahal',     N'S9 Mahal Aircon (top price)',       'aircon',         'aircon-cleaning,aircon-install',       'taytay',          '10-plus', 'above-average', 300,  0,  18, 2, 0, 0, 0,  8,  10,  0,  0,  0),
(42, 's9-quote',     N'S9 Quote Aircon',                   'aircon',         'aircon-cleaning',                      'angono,taytay',   '3-10',    'quote',         150,  3,   3, 2, 0, 0, 0,  4,  60,  1,  0,  0),
(43, 's9-antipolo',  N'S9 Antipolo Aircon Pro',            'aircon',         'aircon-cleaning',                      'antipolo',        '10-plus', 'average',       450,  0,  38, 2, 0, 0, 0, 12,   5,  0,  0,  0),
(44, 's9-angono',    N'S9 Angono Aircon',                  'aircon',         'aircon-cleaning',                      'angono',          '1-3',     'minimum',        60,  4,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(45, 's9-install',   N'S9 Install Lang',                   'aircon',         'aircon-install',                       'taytay',          '10-plus', 'average',       400,  0,  25, 0, 0, 0, 0,  8,  10,  0,  0,  0);

-- Every sample job must be an active, priced job in the config (004-config.sql), or the scenarios mean nothing.
IF EXISTS (SELECT 1 FROM @Pros p CROSS APPLY STRING_SPLIT(p.ServiceIds, ',') s
           WHERE NOT EXISTS (SELECT 1 FROM @Budgets b WHERE b.ServiceId = TRIM(s.value)))
    THROW 50002, 'A sample job is missing from dbo.Services or has no price guide: run 004-config.sql, then this script.', 1;

DECLARE @Ids TABLE (Num int NOT NULL PRIMARY KEY, Id varchar(128) NOT NULL, Mobile varchar(16) NOT NULL);
INSERT INTO @Ids
SELECT Num, @Tag + Code, CONCAT('+639002', RIGHT(CONCAT('000000', Num), 6)) FROM @Pros;

INSERT INTO dbo.Users (Uid, DisplayName, PhoneNumber, Email, Language, Role, IncomeGoalId, ProfilePhotoId, CreatedAt, UpdatedAt, LastActiveAt)
SELECT i.Id, p.Name, i.Mobile, NULL, 'taglish', 'professional', NULL, NULL,
       DATEADD(day, -p.JoinedDays, @now), DATEADD(day, -p.JoinedDays, @now), DATEADD(day, -p.ActiveDays, @now)
FROM @Pros p JOIN @Ids i ON i.Num = p.Num;

-- UpdatedAt = when they joined, so "last active" is LastActiveAt above (ranking takes the later of the two).
INSERT INTO dbo.Professionals (Id, DisplayName, MobileNumber, CategoryId, AreaId, ExperienceId, RateTierId, ProfilePhotoId, PublishedAt, UpdatedAt)
SELECT i.Id, p.Name, i.Mobile, p.CategoryId, 'rizal', p.ExperienceId, p.RateTierId, NULL,
       DATEADD(day, -p.JoinedDays, @now), DATEADD(day, -p.JoinedDays, @now)
FROM @Pros p JOIN @Ids i ON i.Num = p.Num;

INSERT INTO dbo.ProfessionalServices (ProfessionalId, ServiceId)
SELECT i.Id, TRIM(s.value) FROM @Pros p JOIN @Ids i ON i.Num = p.Num CROSS APPLY STRING_SPLIT(p.ServiceIds, ',') s;

INSERT INTO dbo.ProfessionalCities (ProfessionalId, CityId)
SELECT i.Id, TRIM(c.value) FROM @Pros p JOIN @Ids i ON i.Num = p.Num CROSS APPLY STRING_SPLIT(p.CityIds, ',') c;

-- One reference each (Seekers see only the count).
INSERT INTO dbo.ProfessionalReferences (ProfessionalId, Position, Name, MobileNumber)
SELECT i.Id, 0, N'Sample Reference', CONCAT('+639003', RIGHT(CONCAT('000000', i.Num), 6)) FROM @Ids i;

/* ---------- Sample Seekers: they wrote the reviews and started the chats ---------- */
DECLARE @N TABLE (n int NOT NULL PRIMARY KEY);
INSERT INTO @N SELECT TOP (60) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) FROM sys.all_objects;

DECLARE @Names TABLE (k int NOT NULL PRIMARY KEY, Name nvarchar(60) NOT NULL);
INSERT INTO @Names VALUES (0, N'Maricel D.'), (1, N'Joel R.'), (2, N'Ana P.'), (3, N'Carlo M.'), (4, N'Liza G.'),
                          (5, N'Mark T.'), (6, N'Grace S.'), (7, N'Ramon V.'), (8, N'Tess B.'), (9, N'Nonoy C.');

INSERT INTO dbo.Users (Uid, DisplayName, PhoneNumber, Email, Language, Role, IncomeGoalId, ProfilePhotoId, CreatedAt, UpdatedAt, LastActiveAt)
SELECT CONCAT(@Tag, 'seeker-', n.n), nm.Name, CONCAT('+639004', RIGHT(CONCAT('000000', n.n), 6)), NULL, 'taglish', 'seeker', NULL, NULL,
       DATEADD(day, -100, @now), DATEADD(day, -100, @now), NULL
FROM @N n JOIN @Names nm ON nm.k = n.n % 10;

/* ---------- Reviews: review k of a worker is by Seeker k; the first R5 are 5 stars, then R4 4 stars… ---------- */
INSERT INTO dbo.Reviews (ProfessionalId, SeekerId, ReviewerName, Rating, Comment, ServiceId, CreatedAt, UpdatedAt)
SELECT i.Id, CONCAT(@Tag, 'seeker-', n.n), nm.Name, r.Rating,
       CASE r.Rating WHEN 5 THEN N'Ang galing, mabilis at malinis gumawa!' WHEN 4 THEN N'Maayos ang trabaho.'
                     WHEN 3 THEN N'Okay lang.' ELSE N'Hindi natapos sa napag-usapang araw.' END,
       LEFT(p.ServiceIds, CHARINDEX(',', p.ServiceIds + ',') - 1),
       DATEADD(day, -(n.n % 40 + 2), @now), DATEADD(day, -(n.n % 40 + 2), @now)
FROM @Pros p
JOIN @Ids i ON i.Num = p.Num
JOIN @N n ON n.n <= p.R5 + p.R4 + p.R3 + p.R2 + p.R1
JOIN @Names nm ON nm.k = n.n % 10
CROSS APPLY (SELECT CASE WHEN n.n <= p.R5 THEN 5
                         WHEN n.n <= p.R5 + p.R4 THEN 4
                         WHEN n.n <= p.R5 + p.R4 + p.R3 THEN 3
                         WHEN n.n <= p.R5 + p.R4 + p.R3 + p.R2 THEN 2
                         ELSE 1 END AS Rating) r;

/* ---------- Chats: chat k of a worker is with Seeker k (answered first, then ignored, call-only, fresh) ---------- */
DECLARE @Chats TABLE
(
    ProId varchar(128) NOT NULL, SeekerNum int NOT NULL, Kind varchar(10) NOT NULL, ServiceId varchar(40) NOT NULL,
    StartedAt datetimeoffset(3) NOT NULL, RepliedAt datetimeoffset(3) NULL
);
INSERT INTO @Chats (ProId, SeekerNum, Kind, ServiceId, StartedAt, RepliedAt)
SELECT i.Id, n.n, k.Kind, LEFT(p.ServiceIds, CHARINDEX(',', p.ServiceIds + ',') - 1), s.StartedAt,
       CASE WHEN k.Kind = 'answered' THEN DATEADD(minute, p.ReplyMinutes, s.StartedAt) END
FROM @Pros p
JOIN @Ids i ON i.Num = p.Num
JOIN @N n ON n.n <= p.Answered + p.Ignored + p.CallOnly + p.Fresh
CROSS APPLY (SELECT CASE WHEN n.n <= p.Answered THEN 'answered'
                         WHEN n.n <= p.Answered + p.Ignored THEN 'ignored'
                         WHEN n.n <= p.Answered + p.Ignored + p.CallOnly THEN 'call'
                         ELSE 'fresh' END AS Kind) k
CROSS APPLY (SELECT CASE WHEN k.Kind = 'fresh' THEN DATEADD(hour, -2, @now)
                         ELSE DATEADD(hour, -(n.n % 19 + 2) * 24 - 3, @now) END AS StartedAt) s;

-- The chat carries the Seeker's answers: their town, "this week", and the job's "Tipid" budget ("755-1000").
INSERT INTO dbo.Chats (Id, ProfessionalId, SeekerId, ProfessionalName, SeekerName, ServiceId, CityId, UrgencyId, BudgetId,
                       LastMessageId, LastMessageSenderId, LastMessageKind, LastMessageText, LastMessageAt,
                       SeekerReadAt, ProfessionalReadAt, ProfessionalRepliedAt, CreatedAt)
SELECT c.ProId + '_' + CONCAT(@Tag, 'seeker-', c.SeekerNum), c.ProId, CONCAT(@Tag, 'seeker-', c.SeekerNum), p.Name, nm.Name,
       c.ServiceId, 'taytay', 'this-week', b.BudgetId,
       CONCAT(@Tag, 'm-', c.SeekerNum, '-', CASE WHEN c.Kind = 'answered' THEN 'b' ELSE 'a' END, '-', i.Num),
       CASE WHEN c.Kind = 'answered' THEN c.ProId ELSE CONCAT(@Tag, 'seeker-', c.SeekerNum) END,
       CASE WHEN c.Kind = 'call' THEN 'call' ELSE 'text' END,
       CASE c.Kind WHEN 'answered' THEN N'Opo, available po ako. Kailan po kayo free?' WHEN 'call' THEN NULL
                   ELSE N'Hi po, available po ba kayo this week?' END,
       COALESCE(c.RepliedAt, c.StartedAt),
       COALESCE(c.RepliedAt, c.StartedAt), c.RepliedAt, c.RepliedAt, c.StartedAt
FROM @Chats c
JOIN @Ids i ON i.Id = c.ProId
JOIN @Pros p ON p.Num = i.Num
JOIN @Names nm ON nm.k = c.SeekerNum % 10
JOIN @Budgets b ON b.ServiceId = c.ServiceId;

-- The Seeker's first message (a text, or a Call)…
INSERT INTO dbo.ChatMessages (Id, ChatId, SenderId, Kind, Text, At)
SELECT CONCAT(@Tag, 'm-', c.SeekerNum, '-a-', i.Num), c.ProId + '_' + CONCAT(@Tag, 'seeker-', c.SeekerNum), CONCAT(@Tag, 'seeker-', c.SeekerNum),
       CASE WHEN c.Kind = 'call' THEN 'call' ELSE 'text' END,
       CASE WHEN c.Kind = 'call' THEN NULL ELSE N'Hi po, available po ba kayo this week?' END,
       c.StartedAt
FROM @Chats c JOIN @Ids i ON i.Id = c.ProId;

-- …and the worker's reply, when they answered.
INSERT INTO dbo.ChatMessages (Id, ChatId, SenderId, Kind, Text, At)
SELECT CONCAT(@Tag, 'm-', c.SeekerNum, '-b-', i.Num), c.ProId + '_' + CONCAT(@Tag, 'seeker-', c.SeekerNum), c.ProId,
       'text', N'Opo, available po ako. Kailan po kayo free?', c.RepliedAt
FROM @Chats c JOIN @Ids i ON i.Id = c.ProId
WHERE c.Kind = 'answered';

COMMIT TRANSACTION;

DECLARE @Workers int = (SELECT COUNT(*) FROM @Pros);
DECLARE @ReviewCount int = (SELECT COUNT(*) FROM dbo.Reviews WHERE ProfessionalId LIKE @TagPattern);
DECLARE @ChatCount int = (SELECT COUNT(*) FROM dbo.Chats WHERE ProfessionalId LIKE @TagPattern);
PRINT CONCAT('Search ranking seed added: ', @Workers, ' workers, ', @ReviewCount, ' reviews, ', @ChatCount, ' chats.');
