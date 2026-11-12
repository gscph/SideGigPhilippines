/* =====================================================================================
   SideGigPh: Azure SQL schema. Creates every table, key, check and index the API uses.

   How: Azure portal > SQL database > Query editor (or SSMS / Azure Data Studio), paste this
   WHOLE file, Run. Safe to run again: it only creates what's missing. Then run 002-seed.sql
   for sample data.

   This script is the schema's source of truth. SideGigPh.AzureSQL/SideGigPhDb.cs maps to it
   (EF Core, no migrations): a change here needs the same change there, and the reverse.

   Ids:
     - People: the Firebase Auth uid (Firebase stays the sign-in provider).
     - Categories, services, areas, towns, price tiers, experience, urgency: the app's
       ReferenceData ids ("aircon", "aircon-cleaning", "rizal", "taytay", "500-1000"…). They are
       stable, so they're stored as plain text with no lookup tables.
     - Photos: the SHA-256 hex of the stored JPEG (the blob is photos/{id}.jpg in Blob Storage).
   Times are UTC datetimeoffset.

   Anti-fraud rules (CLAUDE.md), enforced here as well as in the API:
     1. One phone, one listing: UQ_Professionals_MobileNumber.
     2. One photo, one listing: PK_ProfessionalWorkPhotos (global), UX_Professionals_ProfilePhoto and
        UX_Users_ProfilePhoto (an account's one profile photo, for Seekers and Skilled Workers alike).
   ===================================================================================== */

SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* ---------- Users: the signed-in person, private to them ---------- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Uid           varchar(128)      NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        DisplayName   nvarchar(80)      NULL,
        PhoneNumber   varchar(16)       NULL,     -- E.164, the account's verified number
        Email         nvarchar(254)     NULL,
        Language      varchar(10)       NOT NULL CONSTRAINT DF_Users_Language DEFAULT ('english')
                                                 CONSTRAINT CK_Users_Language CHECK (Language IN ('english', 'taglish')),
        Role          varchar(12)       NULL     CONSTRAINT CK_Users_Role CHECK (Role IN ('seeker', 'professional')),
        IncomeGoalId  varchar(40)       NULL,     -- private monthly goal, never shown on the listing
        ProfilePhotoId char(64)         NULL     -- the account's one profile photo (photos/{id}.jpg); replaced, never added to
                                                 CONSTRAINT CK_Users_ProfilePhotoId CHECK (ProfilePhotoId NOT LIKE '%[^0-9a-f]%'),
        CreatedAt     datetimeoffset(3) NOT NULL,
        UpdatedAt     datetimeoffset(3) NOT NULL
    );
END;

-- Added after the first release: databases made before it get the column here.
IF COL_LENGTH(N'dbo.Users', N'ProfilePhotoId') IS NULL
    EXEC (N'ALTER TABLE dbo.Users ADD ProfilePhotoId char(64) NULL
              CONSTRAINT CK_Users_ProfilePhotoId CHECK (ProfilePhotoId NOT LIKE ''%[^0-9a-f]%'');');

-- Anti-fraud rule 2: two accounts can't share a profile photo. (EXEC: the column may be new in this run.)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_ProfilePhoto')
    EXEC (N'CREATE UNIQUE INDEX UX_Users_ProfilePhoto ON dbo.Users (ProfilePhotoId) WHERE ProfilePhotoId IS NOT NULL;');

/* ---------- Professionals: the published Skilled Worker listing, id = the worker's uid ---------- */
IF OBJECT_ID(N'dbo.Professionals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Professionals
    (
        Id              varchar(128)      NOT NULL CONSTRAINT PK_Professionals PRIMARY KEY,
        DisplayName     nvarchar(60)      NOT NULL,
        MobileNumber    varchar(16)       NOT NULL
                        CONSTRAINT CK_Professionals_MobileNumber CHECK (MobileNumber LIKE '+639[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
        CategoryId      varchar(40)       NOT NULL,  -- one trade per worker
        AreaId          varchar(40)       NOT NULL,  -- one area per worker: metro-manila, rizal, laguna, cavite
        ExperienceId    varchar(40)       NULL,
        RateTierId      varchar(40)       NULL,      -- a price tier, or "quote"
        ProfilePhotoId  char(64)          NULL
                        CONSTRAINT CK_Professionals_ProfilePhotoId CHECK (ProfilePhotoId NOT LIKE '%[^0-9a-f]%'),
        PublishedAt     datetimeoffset(3) NOT NULL,  -- first went live; kept when the worker edits
        UpdatedAt       datetimeoffset(3) NOT NULL,
        CONSTRAINT UQ_Professionals_MobileNumber UNIQUE (MobileNumber)  -- anti-fraud rule 1
    );

    -- Search: one trade in one area. Towns inside the area are ranked on the phone.
    CREATE INDEX IX_Professionals_Category_Area ON dbo.Professionals (CategoryId, AreaId);

    -- Anti-fraud rule 2 for the profile photo.
    CREATE UNIQUE INDEX UX_Professionals_ProfilePhoto ON dbo.Professionals (ProfilePhotoId) WHERE ProfilePhotoId IS NOT NULL;
END;

/* ---------- The jobs a worker offers ---------- */
IF OBJECT_ID(N'dbo.ProfessionalServices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProfessionalServices
    (
        ProfessionalId  varchar(128) NOT NULL
                        CONSTRAINT FK_ProfessionalServices_Professionals REFERENCES dbo.Professionals (Id) ON DELETE CASCADE,
        ServiceId       varchar(40)  NOT NULL,
        CONSTRAINT PK_ProfessionalServices PRIMARY KEY (ProfessionalId, ServiceId)
    );

    CREATE INDEX IX_ProfessionalServices_Service ON dbo.ProfessionalServices (ServiceId);
END;

/* ---------- The towns a worker goes to (all in the worker's area) ---------- */
IF OBJECT_ID(N'dbo.ProfessionalCities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProfessionalCities
    (
        ProfessionalId  varchar(128) NOT NULL
                        CONSTRAINT FK_ProfessionalCities_Professionals REFERENCES dbo.Professionals (Id) ON DELETE CASCADE,
        CityId          varchar(40)  NOT NULL,
        CONSTRAINT PK_ProfessionalCities PRIMARY KEY (ProfessionalId, CityId)
    );

    CREATE INDEX IX_ProfessionalCities_City ON dbo.ProfessionalCities (CityId);
END;

/* ---------- Work photos (the API keeps up to 6 per listing, the check allows 12; the bytes are in Blob Storage) ---------- */
IF OBJECT_ID(N'dbo.ProfessionalWorkPhotos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProfessionalWorkPhotos
    (
        PhotoId         char(64)     NOT NULL
                        CONSTRAINT PK_ProfessionalWorkPhotos PRIMARY KEY NONCLUSTERED  -- anti-fraud rule 2
                        CONSTRAINT CK_ProfessionalWorkPhotos_PhotoId CHECK (PhotoId NOT LIKE '%[^0-9a-f]%'),
        ProfessionalId  varchar(128) NOT NULL
                        CONSTRAINT FK_ProfessionalWorkPhotos_Professionals REFERENCES dbo.Professionals (Id) ON DELETE CASCADE,
        Position        smallint     NOT NULL  -- 0 = newest
                        CONSTRAINT CK_ProfessionalWorkPhotos_Position CHECK (Position BETWEEN 0 AND 11)
    );

    CREATE UNIQUE CLUSTERED INDEX CX_ProfessionalWorkPhotos_Professional ON dbo.ProfessionalWorkPhotos (ProfessionalId, Position);
END;

/* ---------- References: people who can vouch for a worker (1 to 3 per listing) ----------
   Private: never sent to Seekers. A listing needs at least one to be published, plus a
   profile photo and at least 2 work photos (checked by the API). */
IF OBJECT_ID(N'dbo.ProfessionalReferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProfessionalReferences
    (
        ProfessionalId  varchar(128)  NOT NULL
                        CONSTRAINT FK_ProfessionalReferences_Professionals REFERENCES dbo.Professionals (Id) ON DELETE CASCADE,
        Position        smallint      NOT NULL  -- 0 = first added
                        CONSTRAINT CK_ProfessionalReferences_Position CHECK (Position BETWEEN 0 AND 2),
        Name            nvarchar(60)  NOT NULL,
        MobileNumber    varchar(16)   NOT NULL
                        CONSTRAINT CK_ProfessionalReferences_MobileNumber CHECK (MobileNumber LIKE '+639[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
        CONSTRAINT PK_ProfessionalReferences PRIMARY KEY (ProfessionalId, Position)
    );
END;

/* ---------- Reviews: one per Seeker per worker; writing again replaces it ----------
   No foreign key to Professionals on purpose: a worker who removes and republishes their
   listing must not wipe the reviews about them. */
IF OBJECT_ID(N'dbo.Reviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reviews
    (
        ProfessionalId  varchar(128)      NOT NULL,
        SeekerId        varchar(128)      NOT NULL,
        ReviewerName    nvarchar(60)      NOT NULL,  -- e.g. "Maricel D."
        Rating          tinyint           NOT NULL CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5),
        Comment         nvarchar(1000)    NULL,
        ServiceId       varchar(40)       NULL,
        CreatedAt       datetimeoffset(3) NOT NULL,
        UpdatedAt       datetimeoffset(3) NOT NULL,
        CONSTRAINT PK_Reviews PRIMARY KEY (ProfessionalId, SeekerId)  -- also serves a worker's reviews and star averages
    );

    CREATE INDEX IX_Reviews_Seeker ON dbo.Reviews (SeekerId, CreatedAt DESC);
END;

/* ---------- Chats: one per Seeker per worker, id = {professionalId}_{seekerId} ----------
   The last message is copied onto the chat (in the same transaction as the message), so an
   Inbox is one indexed read. */
IF OBJECT_ID(N'dbo.Chats', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Chats
    (
        Id                   varchar(257)      NOT NULL CONSTRAINT PK_Chats PRIMARY KEY,
        ProfessionalId       varchar(128)      NOT NULL,
        SeekerId             varchar(128)      NOT NULL,
        ProfessionalName     nvarchar(60)      NOT NULL,
        SeekerName           nvarchar(60)      NOT NULL,
        ServiceId            varchar(40)       NULL,  -- the job, from the Seeker's answers
        CityId               varchar(40)       NULL,
        UrgencyId            varchar(40)       NULL,
        BudgetId             varchar(40)       NULL,
        LastMessageId        varchar(64)       NOT NULL,
        LastMessageSenderId  varchar(128)      NOT NULL,
        LastMessageKind      varchar(4)        NOT NULL CONSTRAINT CK_Chats_LastMessageKind CHECK (LastMessageKind IN ('text', 'call')),
        LastMessageText      nvarchar(1000)    NULL,
        LastMessageAt        datetimeoffset(3) NOT NULL,
        SeekerReadAt         datetimeoffset(3) NULL,  -- null = never opened
        ProfessionalReadAt   datetimeoffset(3) NULL,
        CreatedAt            datetimeoffset(3) NOT NULL,
        CONSTRAINT UQ_Chats_Professional_Seeker UNIQUE (ProfessionalId, SeekerId),
        CONSTRAINT CK_Chats_Id CHECK (Id = ProfessionalId + '_' + SeekerId)
    );

    -- Each side's Inbox, newest first.
    CREATE INDEX IX_Chats_Seeker ON dbo.Chats (SeekerId, LastMessageAt DESC);
    CREATE INDEX IX_Chats_Professional ON dbo.Chats (ProfessionalId, LastMessageAt DESC);
END;

/* ---------- Chat messages ---------- */
IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages
    (
        Id        varchar(64)       NOT NULL CONSTRAINT PK_ChatMessages PRIMARY KEY NONCLUSTERED,  -- made by the phone, so a retry is harmless
        ChatId    varchar(257)      NOT NULL
                  CONSTRAINT FK_ChatMessages_Chats REFERENCES dbo.Chats (Id) ON DELETE CASCADE,
        SenderId  varchar(128)      NOT NULL,
        Kind      varchar(4)        NOT NULL CONSTRAINT CK_ChatMessages_Kind CHECK (Kind IN ('text', 'call')),
        Text      nvarchar(1000)    NULL,     -- null for a call
        At        datetimeoffset(3) NOT NULL, -- set by the server
        CONSTRAINT CK_ChatMessages_Text CHECK ((Kind = 'call' AND Text IS NULL) OR (Kind = 'text' AND Text IS NOT NULL))
    );

    -- A chat's messages in order.
    CREATE CLUSTERED INDEX CX_ChatMessages_Chat_At ON dbo.ChatMessages (ChatId, At);
END;

COMMIT TRANSACTION;
PRINT 'SideGigPh schema is ready.';
