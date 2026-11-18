/* =====================================================================================
   SideGigPh schema change 023: a Skilled Worker's price is one number, and the old price scales are gone.

   What it changes and why: a worker now saves one average price per service (pesos), only a guide for customers,
   instead of a price level, an old price tier or a from-to range in the text column RateTierId. The minimum-wage
   floor is gone too (it only applies to a full working day, not to a price per service).
   - dbo.Professionals.RatePesos int NOT NULL, CHECK 1 to 1,000,000 (WorkerPrice.MaxPesos), filled from RateTierId:
     a range 'rate-800-1500' -> its middle rounded to 50; an old tier -> its typical price; a level (minimum /
     average / above-average) -> that point of the cheapest guide among the worker's jobs; anything else ('quote',
     empty) -> the average guide price of their jobs, else 1000.
   - dbo.Professionals.RateTierId is dropped (nothing reads it any more).
   - dbo.ConfigOptions loses the lists 'price-tiers' and 'minimum-wage' (004-config.sql no longer lists them).

   How: run AFTER every earlier schema script. Safe to run again: each step checks first.
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

/* ---------- The new column ---------- */
IF OBJECT_ID(N'dbo.Professionals', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Professionals', N'RatePesos') IS NULL
    EXEC (N'ALTER TABLE dbo.Professionals ADD RatePesos int NULL;');

/* ---------- Filled from the old text column ---------- */
IF COL_LENGTH(N'dbo.Professionals', N'RateTierId') IS NOT NULL
    EXEC (N'
        UPDATE p SET RatePesos =
            CASE
                WHEN p.RateTierId LIKE ''rate-%-%''
                     AND TRY_CONVERT(int, PARSENAME(REPLACE(SUBSTRING(p.RateTierId, 6, 40), ''-'', ''.''), 2)) > 0
                     AND TRY_CONVERT(int, PARSENAME(REPLACE(SUBSTRING(p.RateTierId, 6, 40), ''-'', ''.''), 1)) > 0
                    THEN ROUND((TRY_CONVERT(int, PARSENAME(REPLACE(SUBSTRING(p.RateTierId, 6, 40), ''-'', ''.''), 2))
                              + TRY_CONVERT(int, PARSENAME(REPLACE(SUBSTRING(p.RateTierId, 6, 40), ''-'', ''.''), 1))) / 100.0, 0) * 50
                WHEN p.RateTierId = ''under-500'' THEN 400
                WHEN p.RateTierId = ''500-1000'' THEN 750
                WHEN p.RateTierId = ''1000-2000'' THEN 1500
                WHEN p.RateTierId = ''2000-5000'' THEN 3500
                WHEN p.RateTierId = ''5000-up'' THEN 5000
                WHEN p.RateTierId = ''minimum'' THEN guide.PriceMin
                WHEN p.RateTierId = ''average'' THEN guide.PriceAverage
                WHEN p.RateTierId = ''above-average'' THEN guide.PriceHigh
            END
        FROM dbo.Professionals AS p
        OUTER APPLY (SELECT TOP (1) s.PriceMin, s.PriceAverage, s.PriceHigh
                     FROM dbo.ProfessionalServices AS ps
                     JOIN dbo.Services AS s ON s.Id = ps.ServiceId
                     WHERE ps.ProfessionalId = p.Id AND s.PriceMin IS NOT NULL
                     ORDER BY s.PriceMin) AS guide
        WHERE p.RatePesos IS NULL;');

/* ---------- Anything left: the average guide price of their jobs, else 1000 ---------- */
IF COL_LENGTH(N'dbo.Professionals', N'RatePesos') IS NOT NULL
    EXEC (N'
        UPDATE p SET RatePesos = COALESCE(
            (SELECT ROUND(AVG(CAST(s.PriceAverage AS decimal(12, 2))) / 50, 0) * 50
             FROM dbo.ProfessionalServices AS ps
             JOIN dbo.Services AS s ON s.Id = ps.ServiceId
             WHERE ps.ProfessionalId = p.Id AND s.PriceAverage IS NOT NULL), 1000)
        FROM dbo.Professionals AS p
        WHERE p.RatePesos IS NULL OR p.RatePesos <= 0 OR p.RatePesos > 1000000;');

/* ---------- Required, and within range ---------- */
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Professionals') AND name = N'RatePesos' AND is_nullable = 1)
    EXEC (N'ALTER TABLE dbo.Professionals ALTER COLUMN RatePesos int NOT NULL;');

IF OBJECT_ID(N'dbo.CK_Professionals_RatePesos', N'C') IS NULL
    EXEC (N'ALTER TABLE dbo.Professionals ADD CONSTRAINT CK_Professionals_RatePesos CHECK (RatePesos > 0 AND RatePesos <= 1000000);');

/* ---------- The old text column goes ---------- */
IF COL_LENGTH(N'dbo.Professionals', N'RateTierId') IS NOT NULL
    EXEC (N'ALTER TABLE dbo.Professionals DROP COLUMN RateTierId;');

/* ---------- The old price scales go from the config ---------- */
IF OBJECT_ID(N'dbo.ConfigOptions', N'U') IS NOT NULL
    EXEC (N'DELETE dbo.ConfigOptions WHERE ListId IN (''price-tiers'', ''minimum-wage'');');

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaChanges WHERE Script = '023-schema-worker-price')
    INSERT dbo.SchemaChanges (Script) VALUES ('023-schema-worker-price');

COMMIT TRANSACTION;
