/* =====================================================================================
   SideGigPh: SAMPLE data for trying the app (search, public profiles, star ranking, reviews).
   No chats and no photos.

   SEED TAG: every id this script makes starts with 'seed-s1-'. That tag belongs to THIS seed
   only, so it can be wiped on its own without touching real users or any other seed
   (a future seed should use its own tag, e.g. 'seed-s2-').

   How: run after 001-schema.sql, the same way. Safe to run again: it first deletes every row
   this seed made, then inserts them fresh.
   To roll back / wipe this seed: run 002-sample-data-remove.sql.

   - 31 sample Skilled Workers across all 10 trades and the 4 areas (most in Rizal and Metro Manila).
     Numbers use the unassigned +63 900 prefix, so a Call never reaches a real person.
   - 8 sample Seekers who wrote the reviews. Some workers have none, one has a single 5-star
     review (to see star smoothing), one has a 2-star one.
   - Optional: set @MyProfessionalUid below to your own Firebase uid (Firebase console >
     Authentication > Users) to give your own listing a few reviews to look at on the Skilled Worker side.
     Those reviews are written by this seed's sample Seekers, so the rollback removes them too
     and leaves your listing itself alone.

   These rows aren't Firebase accounts: nobody can sign in as them, so a chat with one never gets a reply.
   ===================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @MyProfessionalUid varchar(128) = NULL;  -- e.g. 'Xb3kq…' to get sample reviews on your own listing
DECLARE @now datetimeoffset(3) = SYSUTCDATETIME();

-- This seed's tag. Keep it in sync with 002-sample-data-remove.sql.
DECLARE @Tag varchar(20) = 'seed-s1-';
DECLARE @TagPattern varchar(21) = @Tag + '%';

BEGIN TRANSACTION;

/* ---------- Remove this seed's previous rows (same logic as the rollback script) ---------- */
DELETE FROM dbo.Reviews
WHERE SeekerId LIKE @TagPattern OR ProfessionalId LIKE @TagPattern
   -- rows from the older, untagged version of this seed
   OR SeekerId LIKE 'seed-seeker-[0-9]' OR ProfessionalId LIKE 'seed-pro-[0-9][0-9]';

DELETE FROM dbo.Professionals  -- services, towns and photos cascade
WHERE Id LIKE @TagPattern OR Id LIKE 'seed-pro-[0-9][0-9]';

DELETE FROM dbo.Users
WHERE Uid LIKE @TagPattern OR Uid LIKE 'seed-pro-[0-9][0-9]' OR Uid LIKE 'seed-seeker-[0-9]';

/* ---------- Skilled Workers ---------- */
DECLARE @Pros TABLE
(
    Num           int           NOT NULL PRIMARY KEY,
    Name          nvarchar(60)  NOT NULL,
    CategoryId    varchar(40)   NOT NULL,
    AreaId        varchar(40)   NOT NULL,
    ExperienceId  varchar(40)   NOT NULL,
    RateTierId    varchar(40)   NOT NULL,
    Language      varchar(10)   NOT NULL,
    ServiceIds    varchar(400)  NOT NULL,  -- comma-separated ReferenceData ids
    CityIds       varchar(400)  NOT NULL
);

INSERT INTO @Pros (Num, Name, CategoryId, AreaId, ExperienceId, RateTierId, Language, ServiceIds, CityIds) VALUES
-- Rizal
( 1, N'Mang Jun Aircon Services',     'aircon',         'rizal',        '10-plus',  '500-1000',  'taglish', 'aircon-cleaning,aircon-repair,aircon-install,aircon-freon',              'taytay,angono'),
( 2, N'Kuya Ben Cool Aircon',         'aircon',         'rizal',        '3-10',     'under-500', 'taglish', 'aircon-cleaning,aircon-leak,aircon-repair',                            'antipolo,taytay'),
( 3, N'RJ Aircon & Ref',              'aircon',         'rizal',        '1-3',      '1000-2000', 'english', 'aircon-install,aircon-repair,aircon-freon',                            'binangonan,angono'),
( 4, N'Tatay Ramon Tubero',           'construction',   'rizal',        '10-plus',  '500-1000',  'taglish', 'construction-plumbing',                                                'taytay,antipolo'),
( 5, N'AquaFix Plumbing',             'construction',   'rizal',        '3-10',     '1000-2000', 'english', 'construction-plumbing',                                                'binangonan,angono'),
( 6, N'Edwin Electrical Works',       'construction',   'rizal',        '3-10',     '500-1000',  'taglish', 'construction-electrical',                                                'taytay,angono,binangonan'),
( 7, N'Rodel Electrician',            'construction',   'rizal',        '1-3',      'under-500', 'taglish', 'construction-electrical',                                                'antipolo'),
( 8, N'Mang Carding Karpintero',      'construction',   'rizal',        '10-plus',  '1000-2000', 'taglish', 'construction-carpentry',                                                 'taytay,antipolo'),
( 9, N'Jomar Woodworks',              'construction',   'rizal',        '3-10',     '2000-5000', 'english', 'construction-carpentry',                                                 'binangonan'),
(10, N'Bubong Masters Rizal',         'construction',   'rizal',        '10-plus',  '2000-5000', 'taglish', 'construction-roofing',                                                 'antipolo,taytay,angono'),
(11, N'Nonoy Welding Shop',           'construction',   'rizal',        '3-10',     '1000-2000', 'taglish', 'construction-welding',                                                 'taytay,angono'),
(12, N'Mang Pido Tiles & Semento',    'construction',   'rizal',        '10-plus',  '2000-5000', 'taglish', 'construction-tiles,construction-masonry',                                'binangonan,angono'),
(13, N'Ate Liza PasaBuy',             'errand_runner',  'rizal',        '1-3',      'under-500', 'taglish', 'errand-grocery,errand-bills,errand-pasabuy,errand-parcel',             'taytay,antipolo'),
(14, N'SunClean Solar Services',      'solar_cleaning', 'rizal',        '1-3',      '1000-2000', 'english', 'solar-cleaning,solar-checkup,solar-inverter',                          'antipolo,binangonan'),
(15, N'Mang Tonyo Auto Repair',       'car_mechanic',   'rizal',        '10-plus',  '500-1000',  'taglish', 'mechanic-engine,mechanic-suspension,mechanic-electronics',              'angono,binangonan,taytay'),
-- Metro Manila
(16, N'QC Aircon Doctors',            'aircon',         'metro-manila', '3-10',     '500-1000',  'english', 'aircon-cleaning,aircon-repair,aircon-freon',                           'quezon-city,marikina,san-juan'),
(17, N'Pasig Tubero Express',         'construction',   'metro-manila', '10-plus',  '1000-2000', 'taglish', 'construction-plumbing',                                                'pasig,mandaluyong,makati'),
(22, N'Makati Roof & Gutter Pros',    'construction',   'metro-manila', '3-10',     '2000-5000', 'english', 'construction-roofing',                                                 'makati,taguig,pasay'),
(23, N'Caloocan Welding Works',       'construction',   'metro-manila', '10-plus',  '1000-2000', 'taglish', 'construction-welding',                                                 'caloocan,malabon,valenzuela,navotas'),
(24, N'Kuya Dodong Tiles',            'construction',   'metro-manila', '10-plus',  '2000-5000', 'taglish', 'construction-tiles,construction-masonry',                                'quezon-city,marikina'),
(25, N'Ate Joy Errands Manila',       'errand_runner',  'metro-manila', '1-3',      'under-500', 'taglish', 'errand-bills,errand-documents,errand-pasabuy,errand-grocery',          'manila,san-juan,mandaluyong'),
(26, N'BrightPanel Solar Care',       'solar_cleaning', 'metro-manila', '3-10',     '1000-2000', 'english', 'solar-cleaning,solar-checkup',                                         'paranaque,las-pinas,muntinlupa'),
(27, N'Mang Lito Karpintero QC',      'construction',   'metro-manila', '10-plus',  '500-1000',  'taglish', 'construction-carpentry',                                                 'quezon-city,caloocan'),
(28, N'Taguig Aircon Pros',           'aircon',         'metro-manila', '1-3',      'quote',     'english', 'aircon-cleaning,aircon-pms,aircon-relocate,aircon-install',            'taguig,pateros,pasig,makati'),
(31, N'Bagong Simula Mekaniko',       'car_mechanic',   'metro-manila', 'under-1',  'under-500', 'taglish', 'mechanic-engine,mechanic-suspension', 'pasig,marikina'),
-- Laguna
(18, N'Nuvali Home Aircon',           'aircon',         'laguna',       '3-10',     '1000-2000', 'english', 'aircon-cleaning,aircon-install,aircon-repair',                         'santa-rosa,calamba,cabuyao'),
(19, N'Sta. Rosa Electrical',         'construction',   'laguna',       '1-3',      '500-1000',  'taglish', 'construction-electrical',                                                'santa-rosa,binan,san-pedro'),
(29, N'Calamba Tubero 24/7',          'construction',   'laguna',       '3-10',     '1000-2000', 'taglish', 'construction-plumbing',                                                'calamba,los-banos,cabuyao'),
-- Cavite
(20, N'Bacoor Plumbing Masters',      'construction',   'cavite',       '3-10',     '500-1000',  'taglish', 'construction-plumbing',                                                'bacoor,imus,kawit'),
(21, N'Imus Auto Electrical',         'car_mechanic',   'cavite',       '1-3',      '500-1000',  'taglish', 'mechanic-electronics,mechanic-engine', 'imus,dasmarinas,general-trias'),
(30, N'Dasma Electrician',            'construction',   'cavite',       '3-10',     '2000-5000', 'english', 'construction-electrical',                                                'dasmarinas,silang,tagaytay,carmona');

DECLARE @ProIds TABLE (Num int NOT NULL PRIMARY KEY, Id varchar(128) NOT NULL, Mobile varchar(16) NOT NULL);
INSERT INTO @ProIds (Num, Id, Mobile)
SELECT Num, CONCAT(@Tag, 'pro-', RIGHT(CONCAT('0', Num), 2)), CONCAT('+6390000', RIGHT(CONCAT('0000', Num), 5))
FROM @Pros;

INSERT INTO dbo.Users (Uid, DisplayName, PhoneNumber, Email, Language, Role, IncomeGoalId, CreatedAt, UpdatedAt)
SELECT i.Id, p.Name, i.Mobile, NULL, p.Language, 'professional', '20000',
       DATEADD(day, -(p.Num * 2 + 10), @now), @now
FROM @Pros AS p JOIN @ProIds AS i ON i.Num = p.Num;

INSERT INTO dbo.Professionals (Id, DisplayName, MobileNumber, CategoryId, AreaId, ExperienceId, RateTierId, ProfilePhotoId, PublishedAt, UpdatedAt)
SELECT i.Id, p.Name, i.Mobile, p.CategoryId, p.AreaId, p.ExperienceId, p.RateTierId, NULL,
       DATEADD(day, -(p.Num * 2 + 5), @now), DATEADD(day, -p.Num, @now)
FROM @Pros AS p JOIN @ProIds AS i ON i.Num = p.Num;

INSERT INTO dbo.ProfessionalServices (ProfessionalId, ServiceId)
SELECT i.Id, TRIM(s.value)
FROM @Pros AS p JOIN @ProIds AS i ON i.Num = p.Num
CROSS APPLY STRING_SPLIT(p.ServiceIds, ',') AS s;

INSERT INTO dbo.ProfessionalCities (ProfessionalId, CityId)
SELECT i.Id, TRIM(c.value)
FROM @Pros AS p JOIN @ProIds AS i ON i.Num = p.Num
CROSS APPLY STRING_SPLIT(p.CityIds, ',') AS c;

/* ---------- Seekers (they wrote the reviews) ---------- */
DECLARE @Seekers TABLE (Num int NOT NULL PRIMARY KEY, Name nvarchar(60) NOT NULL, Language varchar(10) NOT NULL);
INSERT INTO @Seekers (Num, Name, Language) VALUES
(1, N'Maricel D.', 'taglish'),
(2, N'Joel R.',    'taglish'),
(3, N'Ana P.',     'english'),
(4, N'Carlo M.',   'taglish'),
(5, N'Liza G.',    'english'),
(6, N'Mark T.',    'taglish'),
(7, N'Grace S.',   'taglish'),
(8, N'Ramon V.',   'english');

INSERT INTO dbo.Users (Uid, DisplayName, PhoneNumber, Email, Language, Role, IncomeGoalId, CreatedAt, UpdatedAt)
SELECT CONCAT(@Tag, 'seeker-', Num), Name, CONCAT('+6390001', RIGHT(CONCAT('0000', Num), 5)), NULL, Language, 'seeker', NULL,
       DATEADD(day, -(60 + Num), @now), @now
FROM @Seekers;

/* ---------- Reviews ---------- */
DECLARE @Reviews TABLE
(
    ProNum     int            NOT NULL,
    SeekerNum  int            NOT NULL,
    Rating     tinyint        NOT NULL,
    ServiceId  varchar(40)    NULL,
    DaysAgo    int            NOT NULL,
    Comment    nvarchar(1000) NULL
);

INSERT INTO @Reviews (ProNum, SeekerNum, Rating, ServiceId, DaysAgo, Comment) VALUES
( 1, 1, 5, 'aircon-cleaning',       2, N'Mabilis dumating at malinis gumawa. Salamat po!'),
( 1, 2, 5, 'aircon-repair',         9, N'Lumamig ulit ang aircon namin. Sulit!'),
( 1, 3, 4, 'aircon-freon',         21, N'Ayos ang trabaho, medyo late lang ng konti.'),
( 1, 4, 5, 'aircon-install',       40, NULL),
( 2, 5, 4, 'aircon-cleaning',       5, N'Okay naman, mura pa.'),
( 2, 6, 3, 'aircon-leak',          14, N'Tumutulo pa rin ng konti after 2 days, pero bumalik naman siya para ayusin.'),
( 4, 1, 5, 'construction-plumbing',  3, N'Na-unclog agad ang lababo. Galing!'),
( 4, 7, 5, 'construction-plumbing', 18, N'Honest at hindi nananaga sa presyo.'),
( 4, 8, 4, 'construction-plumbing', 33, NULL),
( 5, 2, 4, 'construction-plumbing',  7, N'Maayos ang pagkakabit ng water pump.'),
( 6, 3, 5, 'construction-electrical',  1, N'Hindi na nagti-trip ang breaker. Salamat kuya!'),
( 6, 4, 4, 'construction-electrical', 12, NULL),
( 6, 5, 5, 'construction-electrical', 26, N'Malinis mag-rewire, may dalang sariling gamit.'),
( 6, 6, 5, 'construction-electrical', 45, NULL),
( 7, 7, 5, 'construction-electrical',  4, N'Mabilis lang, ayos!'),
( 8, 8, 5, 'construction-carpentry',  6, N'Ang ganda ng cabinet na ginawa. Matibay.'),
( 8, 1, 4, 'construction-carpentry', 20, NULL),
( 9, 2, 2, 'construction-carpentry', 10, N'Hindi natapos sa napag-usapang araw.'),
(10, 3, 5, 'construction-roofing',  8, N'Wala nang tulo kahit malakas ang ulan.'),
(10, 4, 5, 'construction-roofing', 30, NULL),
(11, 5, 4, 'construction-welding', 11, N'Maganda ang gate, medyo natagalan lang.'),
(12, 6, 5, 'construction-tiles',    9, N'Pantay at malinis ang tiles sa CR.'),
(13, 7, 5, 'errand-bills',          2, N'Nabayaran lahat ng bills on time. Laking tulong!'),
(13, 8, 5, 'errand-grocery',       15, NULL),
(14, 1, 4, 'solar-cleaning',       17, N'Malinis ang panels, tumaas ulit ang output.'),
(15, 2, 5, 'mechanic-engine',       3, N'Ang galing ni Mang Tonyo, umandar ulit ang kotse namin.'),
(15, 3, 5, 'mechanic-suspension',  19, NULL),
(15, 4, 4, 'mechanic-electronics', 36, NULL),
(16, 5, 5, 'aircon-cleaning',       4, N'On time at maayos kausap.'),
(16, 6, 4, 'aircon-freon',         22, NULL),
(17, 7, 4, 'construction-plumbing',  6, N'Naayos ang tulo sa ilalim ng lababo.'),
(17, 8, 5, 'construction-plumbing', 28, N'Pinalitan ang buong shower set, ang linis ng gawa.'),
(18, 1, 5, 'aircon-install',       13, N'Maayos ang installation ng split-type.'),
(20, 2, 4, 'construction-plumbing', 16, NULL),
(21, 3, 5, 'mechanic-electronics',  5, N'Naayos ang ilaw at battery ng kotse, babalikan ko ulit.'),
(21, 4, 5, 'mechanic-engine',      25, NULL),
(22, 5, 4, 'construction-roofing', 12, N'Nalinis ang alulod, wala nang bara.'),
(23, 6, 5, 'construction-welding',  7, N'Matibay ang grills, maganda pa ang design.'),
(24, 7, 5, 'construction-masonry', 24, N'Parang bago ang CR namin. Sulit ang bayad.'),
(25, 8, 5, 'errand-documents',      3, N'Nakuha agad ang documents sa city hall.'),
(26, 1, 4, 'solar-checkup',        20, NULL),
(27, 2, 5, 'construction-carpentry',  8, N'Naayos ang sirang upuan at mesa.'),
(28, 3, 4, 'aircon-pms',           11, N'Detalyado mag-check, may report pa.'),
(29, 4, 5, 'construction-plumbing',  9, N'Mabilis ang siphoning, walang kalat.'),
(30, 5, 5, 'construction-electrical', 14, N'Malinaw ang CCTV, tinuruan pa kami gamitin.');

INSERT INTO dbo.Reviews (ProfessionalId, SeekerId, ReviewerName, Rating, Comment, ServiceId, CreatedAt, UpdatedAt)
SELECT i.Id, CONCAT(@Tag, 'seeker-', r.SeekerNum), s.Name, r.Rating, r.Comment, r.ServiceId,
       DATEADD(day, -r.DaysAgo, @now), DATEADD(day, -r.DaysAgo, @now)
FROM @Reviews AS r
JOIN @ProIds AS i ON i.Num = r.ProNum
JOIN @Seekers AS s ON s.Num = r.SeekerNum;

/* ---------- Optional: sample reviews on your own listing ---------- */
IF @MyProfessionalUid IS NOT NULL
BEGIN
    INSERT INTO dbo.Reviews (ProfessionalId, SeekerId, ReviewerName, Rating, Comment, ServiceId, CreatedAt, UpdatedAt)
    SELECT @MyProfessionalUid, CONCAT(@Tag, 'seeker-', s.Num), s.Name, v.Rating, v.Comment, NULL,
           DATEADD(day, -v.DaysAgo, @now), DATEADD(day, -v.DaysAgo, @now)
    FROM (VALUES
            (1, 5, 1, N'Mabilis dumating at malinis gumawa. Salamat po!'),
            (2, 4, 6, N'Ayos ang trabaho, medyo late lang ng konti.'),
            (3, 5, 20, NULL)
         ) AS v (SeekerNum, Rating, DaysAgo, Comment)
    JOIN @Seekers AS s ON s.Num = v.SeekerNum;
END;

COMMIT TRANSACTION;

SELECT (SELECT COUNT(*) FROM dbo.Professionals WHERE Id LIKE @TagPattern)       AS SampleWorkers,
       (SELECT COUNT(*) FROM dbo.Users WHERE Uid LIKE @Tag + 'seeker-%')        AS SampleSeekers,
       (SELECT COUNT(*) FROM dbo.Reviews WHERE SeekerId LIKE @TagPattern)      AS SampleReviews;