/* =====================================================================================
   SideGigPh: ROLLBACK for 002-sample-data.sql.

   Removes only the rows that seed made: ids tagged 'seed-s1-' (plus the older untagged
   'seed-pro-NN' / 'seed-seeker-N' ids from the first version of that seed).
   Real users, their listings and any other seed are never touched.

   Also removes:
   - reviews real Seekers left on sample Skilled Workers (the worker is gone, so the review would dangle)
   - the optional sample reviews on your own listing (they were written by sample Seekers);
     your listing itself stays.

   All-or-nothing: if anything fails (e.g. another table still points at a sample user),
   the whole thing rolls back and nothing is deleted.
   Safe to run more than once; a second run deletes 0 rows.
   ===================================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Must match @Tag in 002-sample-data.sql.
DECLARE @TagPattern varchar(21) = 'seed-s1-%';

DECLARE @Reviews int, @Workers int, @Users int;

BEGIN TRANSACTION;

DELETE FROM dbo.Reviews
WHERE SeekerId LIKE @TagPattern OR ProfessionalId LIKE @TagPattern
   OR SeekerId LIKE 'seed-seeker-[0-9]' OR ProfessionalId LIKE 'seed-pro-[0-9][0-9]';
SET @Reviews = @@ROWCOUNT;

DELETE FROM dbo.Professionals  -- services, towns and photos cascade
WHERE Id LIKE @TagPattern OR Id LIKE 'seed-pro-[0-9][0-9]';
SET @Workers = @@ROWCOUNT;

DELETE FROM dbo.Users
WHERE Uid LIKE @TagPattern OR Uid LIKE 'seed-pro-[0-9][0-9]' OR Uid LIKE 'seed-seeker-[0-9]';
SET @Users = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT @Reviews AS ReviewsRemoved, @Workers AS WorkersRemoved, @Users AS UsersRemoved;