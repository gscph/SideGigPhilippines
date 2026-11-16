/* =====================================================================================
   SideGigPh schema change 013: geo (a worker's base location and how far they travel).

   What it adds and why (matching by distance instead of by town only; the town lists stay as they are):
   - dbo.Professionals.Latitude / Longitude: the worker's base point (WGS 84). PRIVATE: never sent to another
     person; households only ever see a town or a rounded distance. NULL = not known yet (matched by town only).
   - dbo.Professionals.RadiusKm: how far the worker travels from that point, 1 to 50 km (default 10).
   - dbo.Professionals.Location: the same point as geography (SRID 4326), computed from Latitude / Longitude and
     stored, so distance queries read it: Location.STDistance(@point) <= RadiusKm * 1000 (STDistance is in metres).
   - SIX_Professionals_Location: the spatial index those queries use.
   - Backfill: a listing already live gets the middle of the centres of the towns it serves (Angono, Antipolo,
     Taytay), so every worker has a point from day one; the app then asks them for their own.

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

/* ---------- The worker's base point, and the backfill from town centres ---------- */
IF OBJECT_ID(N'dbo.Professionals', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Professionals', N'Latitude') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.Professionals ADD
              Latitude  float NULL CONSTRAINT CK_Professionals_Latitude  CHECK (Latitude  BETWEEN -90 AND 90),
              Longitude float NULL CONSTRAINT CK_Professionals_Longitude CHECK (Longitude BETWEEN -180 AND 180),
              CONSTRAINT CK_Professionals_LatLng CHECK ((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL));');

    -- Town centres (the same three as TownCentres in SideGigPh.AzureSQL/GeoRules.cs).
    EXEC (N'UPDATE p
               SET Latitude = c.Latitude, Longitude = c.Longitude
              FROM dbo.Professionals AS p
              JOIN (SELECT pc.ProfessionalId, AVG(t.Latitude) AS Latitude, AVG(t.Longitude) AS Longitude
                      FROM dbo.ProfessionalCities AS pc
                      JOIN (VALUES (''angono'',   CAST(14.5266 AS float), CAST(121.1536 AS float)),
                                   (''antipolo'', CAST(14.5865 AS float), CAST(121.1763 AS float)),
                                   (''taytay'',   CAST(14.5574 AS float), CAST(121.1324 AS float))) AS t (CityId, Latitude, Longitude)
                        ON t.CityId = pc.CityId
                     GROUP BY pc.ProfessionalId) AS c
                ON c.ProfessionalId = p.Id
             WHERE p.Latitude IS NULL;');
END;

/* ---------- How far the worker travels ---------- */
IF OBJECT_ID(N'dbo.Professionals', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Professionals', N'RadiusKm') IS NULL
    EXEC (N'ALTER TABLE dbo.Professionals ADD RadiusKm smallint NOT NULL
              CONSTRAINT DF_Professionals_RadiusKm DEFAULT (10)
              CONSTRAINT CK_Professionals_RadiusKm CHECK (RadiusKm BETWEEN 1 AND 50);');

/* ---------- The point as geography (SRID 4326), kept by the database ---------- */
IF OBJECT_ID(N'dbo.Professionals', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Professionals', N'Location') IS NULL
    EXEC (N'ALTER TABLE dbo.Professionals ADD Location AS
              (CASE WHEN Latitude IS NOT NULL AND Longitude IS NOT NULL THEN geography::Point(Latitude, Longitude, 4326) END) PERSISTED;');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'SIX_Professionals_Location' AND object_id = OBJECT_ID(N'dbo.Professionals'))
    EXEC (N'CREATE SPATIAL INDEX SIX_Professionals_Location ON dbo.Professionals (Location) USING GEOGRAPHY_AUTO_GRID;');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '013-schema-geo')
    INSERT dbo.SchemaChanges (Script) VALUES ('013-schema-geo');

COMMIT TRANSACTION;
