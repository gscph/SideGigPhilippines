/* =====================================================================================
   SideGigPh: SAMPLE data for testing SEARCH RANKING (docs/search-ranking-test-plan.md).

   Each trade tests one thing. Its workers are identical except for that one thing, so the order you
   see is caused by it alone:
     S1 Plumber          the job is a hard filter
     S2 Electrician      the town: your town first, "Other towns nearby" after, unserved towns hidden
     S3 Carpentry        reviews: stars (smoothed) + how many
     S4 Welder           responsiveness: reply rate + reply speed
     S5 Masonry          activity: last opened the app
     S6 Car Mechanic     tenure: how long on the app
     S7 Roofing          budget fit
     S8 Solar            experience
     S9 Aircon           everything together, with paging (4 per page) and the nearby group
   The ordinary worker (unless a scenario changes it): Taytay, ₱500–1,000 per job, 1–3 years,
   no reviews, no chats, joined 90 days ago, last active yesterday.
   Names start with their scenario ("S4 …") so you can see at a glance which test a card belongs to.

   SEED TAG: every id starts 'seed-s2-' (the older sample data used 'seed-s1-'). Only those rows are
   touched; real users and their listings never are.
   Numbers use the unassigned +63 900 2xx xxxx range, so a Call never reaches a real person.
   These aren't Firebase accounts: nobody can sign in as them, and a chat with one never gets a reply.

   Needs: 001-schema.sql, 005-schema-ranking-signals.sql and 004-config.sql already run.
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
DELETE FROM dbo.Professionals WHERE Id LIKE @TagPattern;                                     -- jobs, towns, photos cascade
DELETE FROM dbo.Users WHERE Uid LIKE @TagPattern;

IF @RemoveOnly = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT 'Search ranking seed removed.';
    RETURN;
END;

/* ---------- The workers ----------
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
--  #  code             name                                  trade             jobs                                    towns              exp        rate         joined active  5  4  3  2  1  ans  min  ign call fresh
-- S1 Plumber: only workers who list the job
( 1, 's1-leak',      N'S1 Tagas Specialist',               'plumbing',       'plumbing-leak',                        'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
( 2, 's1-clog',      N'S1 Barado Specialist',              'plumbing',       'plumbing-clog',                        'taytay',          '10-plus', 'under-500',  400, 0,  20, 0, 0, 0, 0, 10,  10,  0,  0,  0),
( 3, 's1-both',      N'S1 Tagas at Barado',                'plumbing',       'plumbing-leak,plumbing-clog',          'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
-- S2 Electrician: town groups
( 4, 's2-taytay',    N'S2 Taytay Baguhan',                 'electrical',     'electrical-no-power',                  'taytay',          'under-1', '500-1000',   10, 20,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
( 5, 's2-multi',     N'S2 Taytay at Antipolo',             'electrical',     'electrical-no-power',                  'taytay,antipolo', '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
( 6, 's2-antipolo',  N'S2 Antipolo Star',                  'electrical',     'electrical-no-power',                  'antipolo',        '10-plus', '500-1000',  400,  0,  30, 0, 0, 0, 0, 12,  10,  0,  0,  0),
( 7, 's2-angono',    N'S2 Angono Kuryente',                'electrical',     'electrical-no-power',                  'angono',          '3-10',    '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
( 8, 's2-unserved',  N'S2 Binangonan Lang',                'electrical',     'electrical-no-power',                  'binangonan',      '10-plus', '500-1000',  400,  0,  30, 0, 0, 0, 0, 12,  10,  0,  0,  0),
-- S3 Carpentry: reviews
( 9, 's3-steady',    N'S3 Steady 4.8 (40 reviews)',        'carpentry',      'carpentry-furniture',                  'taytay',          '1-3',     '500-1000',   90,  1,  32, 8, 0, 0, 0,  0,   0,  0,  0,  0),
(10, 's3-lucky',     N'S3 Lucky 5.0 (1 review)',           'carpentry',      'carpentry-furniture',                  'taytay',          '1-3',     '500-1000',   90,  1,   1, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(11, 's3-new',       N'S3 Bago (no reviews)',              'carpentry',      'carpentry-furniture',                  'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(12, 's3-poor',      N'S3 Poor 2.0 (10 reviews)',          'carpentry',      'carpentry-furniture',                  'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0,10, 0,  0,   0,  0,  0,  0),
-- S4 Welder: responsiveness
(13, 's4-fast',      N'S4 Mabilis Sumagot (10 min)',       'welder',         'welder-gate',                          'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0, 10,  10,  0,  0,  0),
(14, 's4-slow',      N'S4 Matagal Sumagot (5 hrs)',        'welder',         'welder-gate',                          'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0, 10, 300,  0,  0,  0),
(15, 's4-new',       N'S4 Bago (no chats yet)',            'welder',         'welder-gate',                          'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(16, 's4-calls',     N'S4 Tawag Lang (calls only)',        'welder',         'welder-gate',                          'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  5,  0),
(17, 's4-fresh',     N'S4 Kaka-message (2 hrs ago)',       'welder',         'welder-gate',                          'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  3),
(18, 's4-ghost',     N'S4 Hindi Sumasagot (1 of 10)',      'welder',         'welder-gate',                          'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0,  1,  60,  9,  0,  0),
-- S5 Masonry: last active (all joined 120 days ago)
(19, 's5-today',     N'S5 Active Today',                   'masonry_tiling', 'masonry-tiles',                        'taytay',          '1-3',     '500-1000',  120,  0,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(20, 's5-week',      N'S5 Active 1 Week Ago',              'masonry_tiling', 'masonry-tiles',                        'taytay',          '1-3',     '500-1000',  120,  7,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(21, 's5-month',     N'S5 Active 1 Month Ago',             'masonry_tiling', 'masonry-tiles',                        'taytay',          '1-3',     '500-1000',  120, 30,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(22, 's5-quarter',   N'S5 Active 3 Months Ago',            'masonry_tiling', 'masonry-tiles',                        'taytay',          '1-3',     '500-1000',  120, 90,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
-- S6 Car Mechanic: tenure (all active today)
(23, 's6-year',      N'S6 Joined 1 Year Ago',              'car_mechanic',   'mechanic-engine',                      'taytay',          '1-3',     '500-1000',  365,  0,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(24, 's6-quarter',   N'S6 Joined 3 Months Ago',            'car_mechanic',   'mechanic-engine',                      'taytay',          '1-3',     '500-1000',   90,  0,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(25, 's6-new',       N'S6 Joined 3 Days Ago',              'car_mechanic',   'mechanic-engine',                      'taytay',          '1-3',     '500-1000',    3,  0,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
-- S7 Roofing: budget fit
(26, 's7-under500',  N'S7 Under 500',                      'roofing',        'roofing-leak',                         'taytay',          '1-3',     'under-500',  90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(27, 's7-500',       N'S7 500 to 1000',                    'roofing',        'roofing-leak',                         'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(28, 's7-1000',      N'S7 1000 to 2000',                   'roofing',        'roofing-leak',                         'taytay',          '1-3',     '1000-2000',  90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(29, 's7-quote',     N'S7 Depends on the Job',             'roofing',        'roofing-leak',                         'taytay',          '1-3',     'quote',      90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(30, 's7-2000',      N'S7 2000 to 5000',                   'roofing',        'roofing-leak',                         'taytay',          '1-3',     '2000-5000',  90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(31, 's7-5000',      N'S7 5000 and Up',                    'roofing',        'roofing-leak',                         'taytay',          '1-3',     '5000-up',    90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
-- S8 Solar: experience
(32, 's8-10plus',    N'S8 10+ Years',                      'solar_cleaning', 'solar-cleaning',                       'taytay',          '10-plus', '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(33, 's8-3to10',     N'S8 3 to 10 Years',                  'solar_cleaning', 'solar-cleaning',                       'taytay',          '3-10',    '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(34, 's8-1to3',      N'S8 1 to 3 Years',                   'solar_cleaning', 'solar-cleaning',                       'taytay',          '1-3',     '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(35, 's8-under1',    N'S8 Under 1 Year',                   'solar_cleaning', 'solar-cleaning',                       'taytay',          'under-1', '500-1000',   90,  1,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
-- S9 Aircon: real-looking mix
(36, 's9-mangjun',   N'S9 Mang Jun Aircon',                'aircon',         'aircon-cleaning,aircon-repair',        'taytay',          '10-plus', '500-1000',  400,  0,  22, 3, 0, 0, 0,  8,  15,  0,  0,  0),
(37, 's9-kuyaben',   N'S9 Kuya Ben Cool',                  'aircon',         'aircon-cleaning,aircon-leak',          'taytay,antipolo', '3-10',    'under-500', 200,  2,   8, 3, 1, 0, 0,  6, 120,  2,  0,  0),
(38, 's9-rj',        N'S9 RJ Aircon (2 reviews)',          'aircon',         'aircon-cleaning',                      'taytay',          '1-3',     '1000-2000',  30,  0,   2, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(39, 's9-bago',      N'S9 Bagong Aircon Tech',             'aircon',         'aircon-cleaning',                      'taytay',          'under-1', '500-1000',    5,  0,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(40, 's9-tulog',     N'S9 Tulog Aircon (inactive)',        'aircon',         'aircon-cleaning,aircon-freon',         'taytay',          '10-plus', '500-1000',  500, 45,  21, 9, 0, 0, 0,  2,  90, 10,  0,  0),
(41, 's9-mahal',     N'S9 Mahal Aircon (₱5,000+)',         'aircon',         'aircon-cleaning,aircon-install',       'taytay',          '10-plus', '5000-up',   300,  0,  18, 2, 0, 0, 0,  8,  10,  0,  0,  0),
(42, 's9-quote',     N'S9 Quote Aircon',                   'aircon',         'aircon-cleaning',                      'angono,taytay',   '3-10',    'quote',     150,  3,   3, 2, 0, 0, 0,  4,  60,  1,  0,  0),
(43, 's9-antipolo',  N'S9 Antipolo Aircon Pro',            'aircon',         'aircon-cleaning',                      'antipolo',        '10-plus', '500-1000',  450,  0,  38, 2, 0, 0, 0, 12,   5,  0,  0,  0),
(44, 's9-angono',    N'S9 Angono Aircon',                  'aircon',         'aircon-cleaning',                      'angono',          '1-3',     'under-500',  60,  4,   0, 0, 0, 0, 0,  0,   0,  0,  0,  0),
(45, 's9-install',   N'S9 Install Lang',                   'aircon',         'aircon-install',                       'taytay',          '10-plus', '500-1000',  400,  0,  25, 0, 0, 0, 0,  8,  10,  0,  0,  0);

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

INSERT INTO dbo.Chats (Id, ProfessionalId, SeekerId, ProfessionalName, SeekerName, ServiceId, CityId, UrgencyId, BudgetId,
                       LastMessageId, LastMessageSenderId, LastMessageKind, LastMessageText, LastMessageAt,
                       SeekerReadAt, ProfessionalReadAt, ProfessionalRepliedAt, CreatedAt)
SELECT c.ProId + '_' + CONCAT(@Tag, 'seeker-', c.SeekerNum), c.ProId, CONCAT(@Tag, 'seeker-', c.SeekerNum), p.Name, nm.Name,
       c.ServiceId, 'taytay', 'this-week', '500-1000',
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
JOIN @Names nm ON nm.k = c.SeekerNum % 10;

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
