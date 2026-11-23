/* =====================================================================================
   SideGigPh: the app's config, the answer lists that are fine to be a few hours stale:
     - the trades (dbo.Categories) and their jobs (dbo.Services),
     - towns, the launch's default town, experience, urgency and income goals (dbo.ConfigOptions),
     - the barangays we serve in each town (dbo.Barangays; needs 021-schema-barangays.sql first).
   REQUIRED data, not a sample: the app reads it all in one GET config when it opens.

   How: run after 001-schema.sql, the same way (portal Query editor, the WHOLE file). Safe to run
   again: it updates labels, icons and order to what is written here, adds new rows, and RETIRES
   (IsActive = 0) any row that is in the database but not in this file. A trade or job an operator changed in the
   operator site (Catalog page, Categories/Services.EditedAt set, 029-schema-catalog-names.sql) is left as the
   operator saved it: copy their change here if you want this file to match.
   Caching: each API instance keeps the config 12 hours and each phone keeps its copy 12 hours, so
   a change here can take up to a day to reach everyone. Restart the Function App to skip the server half.

   Rules (CLAUDE.md):
     - Ids are stable: saved answers, listings, chats and reviews point at them. Change only labels.
       To stop offering a trade, keep its rows here with IsActive = 0 (see home_massage).
     - Each trade's jobs are ordered most-requested first and end with "Others / Iba pa"
       (the -other job). What people type after picking it goes to dbo.OtherJobNotes.
     - Every active job has a price guide (PriceMin <= PriceAverage <= PriceHigh, pesos, labor only): only a guide
       shown to workers setting their price and households setting a budget. Needs 008-schema-service-prices.sql first.
     - The app's code depends on some ids: experience '10-plus', urgencies 'today' / 'this-week' / 'canvassing'.
       Don't retire those.
     - English first, then everyday Taglish (local words where people use them: Tubero, Karpintero…).
   The app ships a copy of this (SideGigPh.Maui/Services/ReferenceData.cs, BuiltInConfig) only for its very first
   launch offline. Labels, order, prices and new trades here need no app change; just never remove an id the copy has.
   ===================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Categories TABLE
(
    Id            varchar(40)    NOT NULL PRIMARY KEY,
    LabelEnglish  nvarchar(60)   NOT NULL,
    LabelTaglish  nvarchar(60)   NOT NULL,
    Icon          nvarchar(16)   NULL,
    HintEnglish   nvarchar(120)  NULL,
    HintTaglish   nvarchar(120)  NULL,
    SortOrder     int            NOT NULL,
    IsActive      bit            NOT NULL
);

INSERT INTO @Categories (Id, LabelEnglish, LabelTaglish, Icon, HintEnglish, HintTaglish, SortOrder, IsActive) VALUES
('aircon',         N'Aircon Technician',      N'Aircon Technician',       N'❄️', N'Cleaning, repair, installation',        N'Linis, repair, pakabit',                        1, 1),
('construction',   N'Construction',           N'Construction',            N'🏗️', N'Carpenter, electrician, plumber, mason, welder', N'Karpintero, electrician, tubero, mason, welder', 2, 1),
('errand_runner',  N'Errands',                N'Errands',                 N'🛵', N'House cleaning, grocery, bills, gardening', N'Linis ng bahay, pa-grocery, bills, halaman', 3, 1),
('driver',         N'Driver (On-Call)',       N'Driver (On-Call)',        N'🚘', N'Family, out of town, airport, motorcycle', N'Family driver, out of town, airport, motor',  4, 1),
('car_mechanic',   N'Car Mechanic',           N'Mekaniko',                N'🚗', N'Suspension, engine, electronics',       N'Suspension, makina, electronics',               5, 1),
('pest_control',   N'Pest Control',           N'Pest Control',            N'🪳', N'Termites, cockroaches, rats, mosquitoes',  N'Anay, ipis, daga, lamok, surot',             6, 1),
-- Retired: moved under Construction as its jobs (019, 020 and 022-schema-construction-trades.sql moved the data).
('electrical',     N'Electrician',            N'Electrician',             N'⚡', N'Power, breaker, outlets, lights',       N'Kuryente, breaker, saksakan, ilaw',            93, 0),
('carpentry',      N'Carpentry',              N'Karpintero',              N'🔨', N'Furniture, doors, cabinets',            N'Custom furniture, pinto, cabinet',             94, 0),
('masonry_tiling', N'Masonry',                N'Mason',                   N'🧱', N'Tiles, cement work, walls',             N'Tiles, semento, palitada, pader',              95, 0),
('plumbing',       N'Plumber',                N'Tubero',                  N'🚰', N'Clogs, leaks, faucets, CR',             N'Barado, tagas, gripo, CR',                     97, 0),
('roofing',        N'Roofing & Gutter',       N'Roofing & Gutter',        N'🏠', N'Roof leaks, gutters, waterproofing',    N'Tulo sa bubong, yero, alulod, waterproofing',  96, 0),
('welder',         N'Welder',                 N'Welder',                  N'🔥', N'Gates, grills, railings',               N'Gate, grills, rehas, pa-weld',                 98, 0),
-- Retired: no longer offered (Driver (On-Call) took its place). Old listings and reviews keep the id.
('it',             N'IT',                     N'IT',                      N'💻', N'Computers, software, networks, AI',     N'Computer, software, network, AI',              91, 0),
-- Retired: no longer offered (Pest Control took its place). Old listings and reviews keep the id.
('solar_cleaning', N'Solar Panel Technician', N'Solar Panel Technician',  N'☀️', N'Panel cleaning and check-up',           N'Linis at check-up ng solar panel',             92, 0),
-- Retired: no longer offered. Old listings and reviews keep the id.
('home_massage',   N'Home Service Massage',   N'Home Service Massage',    N'💆', NULL,                                     NULL,                                            99, 0);

DECLARE @Services TABLE
(
    Id            varchar(40)    NOT NULL PRIMARY KEY,
    CategoryId    varchar(40)    NOT NULL,
    LabelEnglish  nvarchar(100)  NOT NULL,
    LabelTaglish  nvarchar(100)  NOT NULL,
    Icon          nvarchar(16)   NULL,
    SortOrder     int            NOT NULL,
    IsActive      bit            NOT NULL,
    PriceMin      int            NULL,
    PriceAverage  int            NULL,
    PriceHigh     int            NULL
);

-- The last three numbers: what the job usually costs in pesos (labor only): low, average and high. Only a guide:
-- Skilled Workers see it when they type their own average price; Seekers pick a budget range built from it.
-- Sources (Nov 2026): published Metro Manila / Rizal service rates; Rizal labor runs a little under Manila's.
INSERT INTO @Services (Id, CategoryId, LabelEnglish, LabelTaglish, Icon, SortOrder, IsActive, PriceMin, PriceAverage, PriceHigh) VALUES
('aircon-cleaning',          'aircon',         N'Aircon cleaning',                           N'Linis aircon',                               N'🧽', 1, 1,   755,  1000,  1500),
('aircon-repair',            'aircon',         N'Aircon repair',                             N'Sirang aircon',                              N'🌡️', 2, 1,   755,  1500,  2500),
('aircon-leak',              'aircon',         N'Aircon leak',                               N'Tumutulong aircon',                          N'💧', 3, 1,   755,  1000,  1500),
('aircon-freon',             'aircon',         N'Freon recharge',                            N'Pa-freon',                                   N'🧪', 4, 1,  1200,  2000,  3000),
('aircon-install',           'aircon',         N'Aircon install',                            N'Pakabit aircon',                             N'🔧', 5, 1,  2500,  3500,  5000),
('aircon-relocate',          'aircon',         N'Aircon transfer',                           N'Palipat aircon',                             N'🚚', 6, 1,  1500,  2500,  4000),
('aircon-pms',               'aircon',         N'Aircon PMS',                                N'Aircon PMS',                                 N'📋', 7, 1,   755,  1200,  1800),
('aircon-other',             'aircon',         N'Others',                                    N'Iba pa',                                     N'➕', 99, 1,   755,  1200,  2000),

('construction-foreman',     'construction',   N'Foreman',                                   N'Maestro',                                    N'👷', 1, 1,  1200,  1800,  2500),
('construction-carpentry',   'construction',   N'Carpentry',                                 N'Karpintero',                                 N'🔨', 2, 1,   755,  1200,  2000),
('construction-electrical',  'construction',   N'Electrical',                                N'Electrician',                                N'⚡', 3, 1,   755,  1200,  2500),
('construction-plumbing',    'construction',   N'Plumbing',                                  N'Tubero',                                     N'🚰', 4, 1,   755,  1200,  2000),
('construction-masonry',     'construction',   N'Masonry',                                   N'Mason',                                      N'🧱', 5, 1,   755,  1200,  2000),
('construction-tiles',       'construction',   N'Tile setting',                              N'Tiles',                                      N'🔲', 6, 1,  1000,  2000,  3500),
('construction-welding',     'construction',   N'Welding',                                   N'Welder',                                     N'🔥', 7, 1,   755,  1200,  2000),
('construction-roofing',     'construction',   N'Roofing',                                   N'Bubong',                                     N'🏠', 8, 1,  1000,  2000,  3500),
('construction-steelman',    'construction',   N'Steelman',                                  N'Steelman',                                   N'🏗️', 9, 1,   755,  1100,  1500),
('construction-helper',      'construction',   N'Helper',                                    N'Helper',                                     N'🧰', 10, 1,   755,   850,  1000),
('construction-other',       'construction',   N'Others',                                    N'Iba pa',                                     N'➕', 99, 1,   755,  1200,  2000),

('plumbing-clog',            'plumbing',       N'Clogged sink, drain or CR',                 N'Barado ang lababo, drain o CR',              N'🪠', 1, 0,   755,  1000,  1800),
('plumbing-leak',            'plumbing',       N'Leaking pipe or faucet',                    N'May tagas ang tubo o gripo',                 N'💧', 2, 0,   755,  1000,  1500),
('plumbing-toilet',          'plumbing',       N'Toilet / CR repair',                        N'Sirang inidoro / CR',                        N'🚽', 3, 0,   755,  1200,  2000),
('plumbing-replace',         'plumbing',       N'Replace faucet or pipe',                    N'Palit ng gripo o tubo',                      N'🔧', 4, 0,   755,  1000,  1500),
('plumbing-install',         'plumbing',       N'Install sink, toilet or shower',            N'Pakabit ng lababo, inidoro o shower',        N'🚿', 5, 0,  1000,  1800,  3000),
('plumbing-water-heater',    'plumbing',       N'Water heater install or repair',            N'Pakabit o repair ng water heater',           N'♨️', 6, 0,  1000,  1800,  3000),
('plumbing-pump',            'plumbing',       N'Water pump, tank or poso',                  N'Water pump, tangke o poso',                  N'🛢️', 7, 0,  1000,  2000,  3500),
('plumbing-septic',          'plumbing',       N'Septic tank siphoning',                     N'Pa-siphon ng poso negro',                    N'🚛', 8, 0,  3500,  5000,  7000),
('plumbing-other',           'plumbing',       N'Others',                                    N'Wala sa nabanggit',                          N'➕', 99, 0,   755,  1200,  2000),

('electrical-no-power',      'electrical',     N'No power / breaker keeps tripping',         N'Walang kuryente / nagta-trip ang breaker',   N'🔌', 1, 0,   755,  1000,  1500),
('electrical-outlet',        'electrical',     N'Outlet or switch repair',                   N'Sirang outlet o switch',                     N'🔧', 2, 0,   755,   900,  1300),
('electrical-lights',        'electrical',     N'Lights or ceiling fan',                     N'Pakabit ng ilaw o ceiling fan',              N'💡', 3, 0,   755,  1000,  1500),
('electrical-wiring',        'electrical',     N'Wiring or rewiring',                        N'Wiring o rewiring',                          N'🧵', 4, 0,  1500,  3000,  5000),
('electrical-breaker-panel', 'electrical',     N'Breaker or panel box',                      N'Palit ng breaker o panel box',               N'🗃️', 5, 0,  1000,  1800,  3000),
('electrical-meter',         'electrical',     N'Meralco meter / new line',                  N'Pakabit ng metro / bagong linya (Meralco)',  N'📟', 6, 0,  1500,  2500,  4000),
('electrical-cctv',          'electrical',     N'CCTV installation',                         N'Pakabit ng CCTV',                            N'📹', 7, 0,  1500,  2500,  4000),
('electrical-other',         'electrical',     N'Others',                                    N'Wala sa nabanggit',                          N'➕', 99, 0,   755,  1200,  2000),

('roofing-leak',             'roofing',        N'Roof leak repair',                          N'Tulo sa bubong',                             N'🌧️', 1, 0,  1000,  2000,  3500),
('roofing-gutter',           'roofing',        N'Gutter cleaning or repair',                 N'Linis o repair ng alulod',                   N'🍂', 2, 0,   755,  1500,  2500),
('roofing-sheet',            'roofing',        N'Roof sheet replacement',                    N'Palit ng yero',                              N'🔩', 3, 0,  1500,  3000,  5000),
('roofing-waterproofing',    'roofing',        N'Waterproofing (roof deck, walls)',          N'Waterproofing ng roof deck o pader',         N'🛡️', 4, 0,  2000,  4000,  7000),
('roofing-paint',            'roofing',        N'Roof painting',                             N'Pintura ng bubong',                          N'🎨', 5, 0,  2000,  3500,  6000),
('roofing-other',            'roofing',        N'Others',                                    N'Wala sa nabanggit',                          N'➕', 99, 0,  1000,  2000,  3500),

('carpentry-furniture',      'carpentry',      N'Furniture repair',                          N'Repair ng furniture',                        N'🪑', 1, 0,   755,  1200,  2000),
('carpentry-door-window',    'carpentry',      N'Door or window repair',                     N'Repair ng pinto o bintana',                  N'🚪', 2, 0,   755,  1200,  2000),
('carpentry-cabinet',        'carpentry',      N'Custom cabinets or shelves',                N'Pagawa ng cabinet o estante',                N'🗄️', 3, 0,  2500,  5000,  8000),
('carpentry-ceiling-roof',   'carpentry',      N'Ceiling repair',                            N'Repair ng kisame',                           N'🏠', 4, 0,  1000,  2000,  3500),
('carpentry-partition',      'carpentry',      N'Partition or drywall',                      N'Partition o drywall',                        N'🧱', 5, 0,  2000,  4000,  7000),
('carpentry-other',          'carpentry',      N'Others',                                    N'Wala sa nabanggit',                          N'➕', 99, 0,  1000,  1800,  3000),

('welder-repair',            'welder',         N'Welding repair',                            N'Pa-weld ng sira',                            N'🔥', 1, 0,   755,  1200,  2000),
('welder-gate',              'welder',         N'Gate (new or repair)',                      N'Pagawa o repair ng gate',                    N'🚧', 2, 0,  2000,  4000,  7000),
('welder-grills',            'welder',         N'Window grills',                             N'Grills sa bintana',                          N'🪟', 3, 0,  1500,  3000,  5000),
('welder-railings',          'welder',         N'Stair or balcony railings',                 N'Rehas sa hagdan o balcony',                  N'🪜', 4, 0,  2000,  4000,  7000),
('welder-frame',             'welder',         N'Steel frame or trusses',                    N'Bakal na frame o trusses',                   N'🏗️', 5, 0,  3000,  6000, 10000),
('welder-other',             'welder',         N'Others',                                    N'Wala sa nabanggit',                          N'➕', 99, 0,  1000,  2000,  3500),

('masonry-tiles',            'masonry_tiling', N'Tile installation or replacement',          N'Pakabit o palit ng tiles',                   N'🔲', 1, 0,  1500,  3000,  5000),
('masonry-cement',           'masonry_tiling', N'Cement work / plastering',                  N'Semento / palitada',                         N'🪣', 2, 0,  1000,  2000,  3500),
('masonry-crack',            'masonry_tiling', N'Crack repair (wall or floor)',              N'Ayos ng bitak sa pader o sahig',             N'🔨', 3, 0,   755,  1500,  2500),
('masonry-wall',             'masonry_tiling', N'Wall or fence (hollow blocks)',             N'Pader o bakod (hollow blocks)',              N'🧱', 4, 0,  2000,  4000,  7000),
('masonry-cr-renovation',    'masonry_tiling', N'CR / bathroom renovation',                  N'Renovate ng CR / banyo',                     N'🚿', 5, 0,  5000, 10000, 18000),
('masonry-other',            'masonry_tiling', N'Others',                                    N'Wala sa nabanggit',                          N'➕', 99, 0,  1000,  2000,  3500),

('mechanic-suspension',      'car_mechanic',   N'Suspension',                                N'Suspension',                                 N'🛞', 1, 1,  1000,  2000,  3500),
('mechanic-engine',          'car_mechanic',   N'Engine',                                    N'Makina',                                     N'⚙️', 2, 1,  1500,  3000,  5000),
('mechanic-electronics',     'car_mechanic',   N'Electronics',                               N'Electronics',                                N'🔋', 3, 1,  1000,  2000,  3500),
('mechanic-other',           'car_mechanic',   N'Others',                                    N'Iba pa',                                     N'➕', 99, 1,  1000,  2000,  3500),

('errand-house-cleaning',    'errand_runner',  N'House cleaning',                            N'Linis bahay',                                N'🧹', 1, 1,   755,  1000,  1500),
('errand-grocery',           'errand_runner',  N'Grocery run',                               N'Pa-grocery',                                 N'🛒', 2, 1,   755,   900,  1200),
('errand-bills',             'errand_runner',  N'Bills payment',                             N'Bills payment',                              N'🧾', 3, 1,   755,   850,  1000),
('errand-pasabuy',           'errand_runner',  N'PasaBuy',                                   N'PasaBuy',                                    N'🛍️', 4, 1,   755,   900,  1200),
('errand-documents',         'errand_runner',  N'Document delivery',                         N'Pahatid papeles',                            N'📄', 5, 1,   755,   900,  1200),
('errand-parcel',            'errand_runner',  N'Parcel pickup',                             N'Pa-pickup parcel',                           N'📦', 6, 1,   755,   850,  1000),
('errand-car-cleaning',      'errand_runner',  N'Car cleaning',                              N'Linis kotse',                                N'🚗', 7, 1,   755,   900,  1200),
('errand-gardening',         'errand_runner',  N'Gardening',                                 N'Paghahalaman',                               N'🌱', 8, 1,   755,  1000,  1500),
('errand-other',             'errand_runner',  N'Others',                                    N'Iba pa',                                     N'➕', 99, 1,   755,   900,  1200),

('driver-family',            'driver',         N'Family driver for the day',                 N'Family driver (per araw)',                   N'🚗', 1, 1,  1000,  1500,  2500),
('driver-out-of-town',       'driver',         N'Out-of-town trip (Tagaytay, Baguio…)',      N'Out of town (Tagaytay, Baguio…)',            N'🛣️', 2, 1,  1500,  2500,  4000),
('driver-airport',           'driver',         N'Airport transfer (NAIA)',                   N'Hatid-sundo sa airport (NAIA)',              N'✈️', 3, 1,   800,  1200,  2000),
('driver-motorcycle',        'driver',         N'Motorcycle rider (hatid-sundo)',            N'Rider ng motor (hatid-sundo)',               N'🏍️', 4, 1,   300,   600,  1000),
('driver-other',             'driver',         N'Others',                                    N'Wala sa nabanggit',                          N'➕', 99, 1,   800,  1500,  2500),

('pest-termites',            'pest_control',   N'Termites',                                  N'Anay',                                       N'🐜', 1, 1,  2500,  5000,  9000),
('pest-cockroaches',         'pest_control',   N'Cockroaches',                               N'Ipis',                                       N'🪳', 2, 1,  1200,  2000,  3500),
('pest-rats',                'pest_control',   N'Rats and mice',                             N'Daga',                                       N'🐀', 3, 1,  1500,  2500,  4000),
('pest-mosquitoes',          'pest_control',   N'Mosquito misting',                          N'Pa-misting sa lamok',                        N'🦟', 4, 1,  1200,  2000,  3500),
('pest-bedbugs',             'pest_control',   N'Bed bugs',                                  N'Surot',                                      N'🛏️', 5, 1,  2000,  3500,  6000),
('pest-ants',                'pest_control',   N'Ants',                                      N'Langgam',                                    N'🐜', 6, 1,  1000,  1800,  3000),
('pest-general',             'pest_control',   N'Whole-house treatment',                     N'Spray sa buong bahay',                       N'🧴', 7, 1,  1500,  2500,  4500),
('pest-other',               'pest_control',   N'Others',                                    N'Wala sa nabanggit',                          N'➕', 99, 1,  1200,  2000,  3500),

-- Retired with it.
('it-software',              'it',             N'Software',                                  N'Software',                                   N'💾', 1, 0,   755,  1200,  2000),
('it-hardware',              'it',             N'Hardware',                                  N'Hardware',                                   N'🖥️', 2, 0,   755,  1500,  2500),
('it-cybersecurity',         'it',             N'Cybersecurity',                             N'Cybersecurity',                              N'🔒', 3, 0,  1000,  2000,  3500),
('it-ai',                    'it',             N'AI',                                        N'AI',                                         N'🤖', 4, 0,  1500,  3000,  5000),
('it-enterprise',            'it',             N'Enterprise Solutions',                      N'Enterprise Solutions',                       N'🏢', 5, 0,  3000,  6000, 10000),
('it-other',                 'it',             N'Others',                                    N'Iba pa',                                     N'➕', 99, 0,   755,  1500,  2500),

-- Retired with solar_cleaning.
('solar-cleaning',           'solar_cleaning', N'Panel cleaning',                            N'Linis panel',                                N'🧽', 1, 0,  1000,  2000,  3500),
('solar-checkup',            'solar_cleaning', N'Solar check-up',                            N'Solar check-up',                             N'📋', 2, 0,  1000,  1800,  3000),
('solar-inverter',           'solar_cleaning', N'Inverter check',                            N'Inverter check',                             N'🔋', 3, 0,  1000,  2000,  3500),
('solar-other',              'solar_cleaning', N'Others',                                    N'Iba pa',                                     N'➕', 99, 0,  1000,  2000,  3500),

-- Retired with home_massage.
('massage-full-body',        'home_massage',   N'Full body massage',                         N'Full body massage',                          N'💆', 1, 0,  NULL,  NULL,  NULL),
('massage-hilot',            'home_massage',   N'Hilot',                                     N'Hilot',                                      N'🤲', 2, 0,  NULL,  NULL,  NULL),
('massage-ventosa',          'home_massage',   N'Ventosa',                                   N'Ventosa',                                    N'🫙', 3, 0,  NULL,  NULL,  NULL),
('massage-foot',             'home_massage',   N'Foot reflexology',                          N'Foot reflexology',                           N'🦶', 4, 0,  NULL,  NULL,  NULL),
('massage-other',            'home_massage',   N'Others',                                    N'Wala sa nabanggit',                          N'➕', 99, 0,  NULL,  NULL,  NULL);

DECLARE @Options TABLE
(
    ListId        varchar(40)    NOT NULL,
    Id            varchar(40)    NOT NULL,
    LabelEnglish  nvarchar(100)  NOT NULL,
    LabelTaglish  nvarchar(100)  NOT NULL,
    Icon          nvarchar(16)   NULL,
    Amount        int            NOT NULL,
    SortOrder     int            NOT NULL,
    IsActive      bit            NOT NULL,
    PRIMARY KEY (ListId, Id)
);

INSERT INTO @Options (ListId, Id, LabelEnglish, LabelTaglish, Icon, Amount, SortOrder, IsActive) VALUES
-- Towns we serve (A–Z), all in the app's one area. Place names are the same in both languages.
('towns',        'angono',     N'Angono',                  N'Angono',                  NULL,     0, 1, 1),
('towns',        'antipolo',   N'Antipolo',                N'Antipolo',                NULL,     0, 2, 1),
('towns',        'taytay',     N'Taytay',                  N'Taytay',                  NULL,     0, 3, 1),
-- The launch's default town: pre-selected wherever a person picks a town (the others stay selectable). One row; its
-- Id is the town ('towns' above). To change it, change the Id: the old row is retired by the MERGE below.
('default-town', 'angono',     N'Angono',                  N'Angono',                  NULL,     0, 1, 1),
('experience',   'under-1',    N'Less than 1 year',        N'Wala pang 1 taon',        N'🌱',    0, 1, 1),
('experience',   '1-3',        N'1–3 years',               N'1–3 taon',                N'🧰',    0, 2, 1),
('experience',   '3-10',       N'3–10 years',              N'3–10 taon',               N'🛠️',    0, 3, 1),
('experience',   '10-plus',    N'10+ years',               N'Mahigit 10 taon',         N'🏆',    0, 4, 1),
('urgencies',    'today',      N'Today, it''s urgent',     N'Ngayon na, urgent',       N'⚡',    0, 1, 1),
('urgencies',    'this-week',  N'This week',               N'Ngayong linggo',          N'📅',    0, 2, 1),
('urgencies',    'canvassing', N'Just looking around',     N'Tumitingin-tingin lang',  N'🔍',    0, 3, 1),
-- Monthly earnings goals (private to the worker). Amount = pesos per month.
('income-goals', '5000',       N'₱5,000',                  N'₱5,000',                  NULL,  5000, 1, 1),
('income-goals', '10000',      N'₱10,000',                 N'₱10,000',                 NULL,  10000, 2, 1),
('income-goals', '20000',      N'₱20,000',                 N'₱20,000',                 NULL,  20000, 3, 1),
('income-goals', '30000-up',   N'₱30,000 or more',         N'₱30,000 pataas',          NULL,  30000, 4, 1);

-- The barangays we serve in each town ('towns' above), A–Z. Ids are stable, town first, no accents ('angono-santo-nino');
-- labels are the barangay's own name (the app adds "Brgy."), the same in both languages. To add one, add a row; to stop
-- serving one, set IsActive = 0 (never delete or reuse an id: posts point at them). Picking one is always optional.
-- Sources (Nov 2026): PSA PSGC / the towns' official lists: Angono 10, Taytay 5, Antipolo 16.
DECLARE @Barangays TABLE
(
    Id            varchar(40)    NOT NULL PRIMARY KEY,
    CityId        varchar(40)    NOT NULL,
    LabelEnglish  nvarchar(60)   NOT NULL,
    LabelTaglish  nvarchar(60)   NOT NULL,
    SortOrder     int            NOT NULL,
    IsActive      bit            NOT NULL
);

INSERT INTO @Barangays (Id, CityId, LabelEnglish, LabelTaglish, SortOrder, IsActive) VALUES
('angono-bagumbayan',        'angono',   N'Bagumbayan',        N'Bagumbayan',         1, 1),
('angono-kalayaan',          'angono',   N'Kalayaan',          N'Kalayaan',           2, 1),
('angono-mahabang-parang',   'angono',   N'Mahabang Parang',   N'Mahabang Parang',    3, 1),
('angono-poblacion-ibaba',   'angono',   N'Poblacion Ibaba',   N'Poblacion Ibaba',    4, 1),
('angono-poblacion-itaas',   'angono',   N'Poblacion Itaas',   N'Poblacion Itaas',    5, 1),
('angono-san-isidro',        'angono',   N'San Isidro',        N'San Isidro',         6, 1),
('angono-san-pedro',         'angono',   N'San Pedro',         N'San Pedro',          7, 1),
('angono-san-roque',         'angono',   N'San Roque',         N'San Roque',          8, 1),
('angono-san-vicente',       'angono',   N'San Vicente',       N'San Vicente',        9, 1),
('angono-santo-nino',        'angono',   N'Santo Niño',        N'Santo Niño',        10, 1),

('antipolo-bagong-nayon',    'antipolo', N'Bagong Nayon',      N'Bagong Nayon',       1, 1),
('antipolo-beverly-hills',   'antipolo', N'Beverly Hills',     N'Beverly Hills',      2, 1),
('antipolo-calawis',         'antipolo', N'Calawis',           N'Calawis',            3, 1),
('antipolo-cupang',          'antipolo', N'Cupang',            N'Cupang',             4, 1),
('antipolo-dalig',           'antipolo', N'Dalig',             N'Dalig',              5, 1),
('antipolo-dela-paz',        'antipolo', N'Dela Paz',          N'Dela Paz',           6, 1),
('antipolo-inarawan',        'antipolo', N'Inarawan',          N'Inarawan',           7, 1),
('antipolo-mambugan',        'antipolo', N'Mambugan',          N'Mambugan',           8, 1),
('antipolo-mayamot',         'antipolo', N'Mayamot',           N'Mayamot',            9, 1),
('antipolo-muntingdilaw',    'antipolo', N'Muntingdilaw',      N'Muntingdilaw',      10, 1),
('antipolo-san-isidro',      'antipolo', N'San Isidro',        N'San Isidro',        11, 1),
('antipolo-san-jose',        'antipolo', N'San Jose',          N'San Jose',          12, 1),
('antipolo-san-juan',        'antipolo', N'San Juan',          N'San Juan',          13, 1),
('antipolo-san-luis',        'antipolo', N'San Luis',          N'San Luis',          14, 1),
('antipolo-san-roque',       'antipolo', N'San Roque',         N'San Roque',         15, 1),
('antipolo-santa-cruz',      'antipolo', N'Santa Cruz',        N'Santa Cruz',        16, 1),

('taytay-dolores',           'taytay',   N'Dolores',           N'Dolores',            1, 1),
('taytay-muzon',             'taytay',   N'Muzon',             N'Muzon',              2, 1),
('taytay-san-isidro',        'taytay',   N'San Isidro',        N'San Isidro',         3, 1),
('taytay-san-juan',          'taytay',   N'San Juan',          N'San Juan',           4, 1),
('taytay-santa-ana',         'taytay',   N'Santa Ana',         N'Santa Ana',          5, 1);

BEGIN TRANSACTION;

MERGE dbo.ConfigOptions AS target
USING @Options AS source ON target.ListId = source.ListId AND target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    LabelEnglish = source.LabelEnglish, LabelTaglish = source.LabelTaglish, Icon = source.Icon,
    Amount = source.Amount, SortOrder = source.SortOrder, IsActive = source.IsActive
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ListId, Id, LabelEnglish, LabelTaglish, Icon, Amount, SortOrder, IsActive)
    VALUES (source.ListId, source.Id, source.LabelEnglish, source.LabelTaglish, source.Icon, source.Amount, source.SortOrder, source.IsActive)
WHEN NOT MATCHED BY SOURCE THEN UPDATE SET IsActive = 0;

MERGE dbo.Categories AS target
USING @Categories AS source ON target.Id = source.Id
WHEN MATCHED AND target.EditedAt IS NULL THEN UPDATE SET
    LabelEnglish = source.LabelEnglish, LabelTaglish = source.LabelTaglish, Icon = source.Icon,
    HintEnglish = source.HintEnglish, HintTaglish = source.HintTaglish, SortOrder = source.SortOrder, IsActive = source.IsActive
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, LabelEnglish, LabelTaglish, Icon, HintEnglish, HintTaglish, SortOrder, IsActive)
    VALUES (source.Id, source.LabelEnglish, source.LabelTaglish, source.Icon, source.HintEnglish, source.HintTaglish, source.SortOrder, source.IsActive)
WHEN NOT MATCHED BY SOURCE AND target.EditedAt IS NULL THEN UPDATE SET IsActive = 0;

MERGE dbo.Services AS target
USING @Services AS source ON target.Id = source.Id
WHEN MATCHED AND target.EditedAt IS NULL THEN UPDATE SET
    CategoryId = source.CategoryId, LabelEnglish = source.LabelEnglish, LabelTaglish = source.LabelTaglish,
    Icon = source.Icon, SortOrder = source.SortOrder, IsActive = source.IsActive,
    PriceMin = source.PriceMin, PriceAverage = source.PriceAverage, PriceHigh = source.PriceHigh
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, CategoryId, LabelEnglish, LabelTaglish, Icon, SortOrder, IsActive, PriceMin, PriceAverage, PriceHigh)
    VALUES (source.Id, source.CategoryId, source.LabelEnglish, source.LabelTaglish, source.Icon, source.SortOrder, source.IsActive,
            source.PriceMin, source.PriceAverage, source.PriceHigh)
WHEN NOT MATCHED BY SOURCE AND target.EditedAt IS NULL THEN UPDATE SET IsActive = 0;

MERGE dbo.Barangays AS target
USING @Barangays AS source ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    CityId = source.CityId, LabelEnglish = source.LabelEnglish, LabelTaglish = source.LabelTaglish,
    SortOrder = source.SortOrder, IsActive = source.IsActive
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, CityId, LabelEnglish, LabelTaglish, SortOrder, IsActive)
    VALUES (source.Id, source.CityId, source.LabelEnglish, source.LabelTaglish, source.SortOrder, source.IsActive)
WHEN NOT MATCHED BY SOURCE THEN UPDATE SET IsActive = 0;

COMMIT TRANSACTION;
PRINT 'SideGigPh config is ready.';
