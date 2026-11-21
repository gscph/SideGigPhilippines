/*
    Wipe one person's data from the SideGigPh database by their Firebase uid.

    The same rows the app's Delete account removes (UserStore.DeleteAccountAsync + VerificationStore.ForgetAsync),
    for an operator to run by hand in the Azure portal's Query editor or SSMS:
      - their worker listing (services, towns, work photos, old references)
      - every chat they are in, with its messages (for both people)
      - their job posts (with the posts' photos and every offer on them) and the offers they made on others' posts
      - reviews they wrote AND reviews about them, household ratings both ways
      - blocks both ways, their phones for notifications, their badge requests, their feedback votes
      - their Users row
    Kept without their uid: reports they made, "Others" notes, feedback suggestions they wrote.
    Kept as is: reports ABOUT them (the operator's safety record) and the VerificationAudit log.

    NOT done by SQL (do these after a real run, using the lists this script prints):
      1. Delete the photo blobs: storage account sidegigphbfb9, container "photos", blob {PhotoId}.jpg
      2. Delete the badge proofs still waiting: container "verification", the DocumentBlob names
      3. Delete the sign-in account: Firebase console → Authentication → Users → the uid
      (The app's search cache clears itself within 30 s.)

    HOW TO RUN: set @Uid. With @DryRun = 1 (the default) it shows what it would delete and rolls everything back.
    Set @DryRun = 0 to really delete. It is all one transaction: any error leaves the database unchanged.
    NEVER put this file in SideGigPh.AzureSQL/Sql/ — the deploy runs every .sql in that folder.
*/

DECLARE @Uid    varchar(128) = 'PUT-THE-FIREBASE-UID-HERE';
DECLARE @DryRun bit          = 1;

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF @Uid IS NULL OR LTRIM(RTRIM(@Uid)) = '' OR @Uid = 'PUT-THE-FIREBASE-UID-HERE'
BEGIN
    RAISERROR(N'Set @Uid to the Firebase uid first.', 16, 1);
    RETURN;
END;

/* ---------- What will go: one row per table ---------- */
SELECT Item, Rows FROM (VALUES
    (1,  'Users',                                    (SELECT COUNT(*) FROM dbo.Users                  WHERE Uid = @Uid)),
    (2,  'Professionals (listing)',                  (SELECT COUNT(*) FROM dbo.Professionals          WHERE Id = @Uid)),
    (3,  'ProfessionalServices',                     (SELECT COUNT(*) FROM dbo.ProfessionalServices   WHERE ProfessionalId = @Uid)),
    (4,  'ProfessionalCities',                       (SELECT COUNT(*) FROM dbo.ProfessionalCities     WHERE ProfessionalId = @Uid)),
    (5,  'ProfessionalWorkPhotos',                   (SELECT COUNT(*) FROM dbo.ProfessionalWorkPhotos WHERE ProfessionalId = @Uid)),
    (6,  'ProfessionalReferences',                   (SELECT COUNT(*) FROM dbo.ProfessionalReferences WHERE ProfessionalId = @Uid)),
    (7,  'Chats',                                    (SELECT COUNT(*) FROM dbo.Chats WHERE SeekerId = @Uid OR ProfessionalId = @Uid)),
    (8,  'ChatMessages (in those chats)',            (SELECT COUNT(*) FROM dbo.ChatMessages m JOIN dbo.Chats c ON c.Id = m.ChatId
                                                                                              WHERE c.SeekerId = @Uid OR c.ProfessionalId = @Uid)),
    (9,  'JobPosts (theirs)',                        (SELECT COUNT(*) FROM dbo.JobPosts WHERE SeekerUid = @Uid)),
    (10, 'JobPostPhotos (on their posts)',           (SELECT COUNT(*) FROM dbo.JobPostPhotos ph JOIN dbo.JobPosts p ON p.Id = ph.PostId WHERE p.SeekerUid = @Uid)),
    (11, 'JobOffers (on their posts)',               (SELECT COUNT(*) FROM dbo.JobOffers o JOIN dbo.JobPosts p ON p.Id = o.PostId WHERE p.SeekerUid = @Uid)),
    (12, 'JobOffers (they made)',                    (SELECT COUNT(*) FROM dbo.JobOffers WHERE ProUid = @Uid)),
    (13, 'Reviews (by or about them)',               (SELECT COUNT(*) FROM dbo.Reviews WHERE SeekerId = @Uid OR ProfessionalId = @Uid)),
    (14, 'HouseholdReviews (by or about them)',      (SELECT COUNT(*) FROM dbo.HouseholdReviews WHERE SeekerUid = @Uid OR ProUid = @Uid)),
    (15, 'Blocks (either way)',                      (SELECT COUNT(*) FROM dbo.Blocks WHERE BlockerUid = @Uid OR BlockedUid = @Uid)),
    (16, 'PushDevices',                              (SELECT COUNT(*) FROM dbo.PushDevices WHERE Uid = @Uid)),
    (17, 'WorkerVerifications (badges)',             (SELECT COUNT(*) FROM dbo.WorkerVerifications WHERE Uid = @Uid)),
    (18, 'FeedbackVotes',                            (SELECT COUNT(*) FROM dbo.FeedbackVotes WHERE VoterUid = @Uid)),
    (19, 'Reports they made (uid cleared)',          (SELECT COUNT(*) FROM dbo.Reports WHERE ReporterUid = @Uid)),
    (20, 'OtherJobNotes (uid cleared)',              (SELECT COUNT(*) FROM dbo.OtherJobNotes WHERE Uid = @Uid)),
    (21, 'FeedbackPosts they wrote (uid cleared)',   (SELECT COUNT(*) FROM dbo.FeedbackPosts WHERE AuthorUid = @Uid)),
    (22, 'Reports about them (KEPT)',                (SELECT COUNT(*) FROM dbo.Reports WHERE TargetUid = @Uid))
) AS t(Ord, Item, Rows)
ORDER BY Ord;

/* ---------- Files to delete from Blob Storage afterwards ---------- */
DECLARE @Photos TABLE (PhotoId varchar(64) PRIMARY KEY);
INSERT INTO @Photos (PhotoId)
SELECT PhotoId FROM dbo.ProfessionalWorkPhotos WHERE ProfessionalId = @Uid
UNION SELECT ProfilePhotoId FROM dbo.Professionals WHERE Id = @Uid AND ProfilePhotoId IS NOT NULL
UNION SELECT ProfilePhotoId FROM dbo.Users WHERE Uid = @Uid AND ProfilePhotoId IS NOT NULL
UNION SELECT ph.PhotoId FROM dbo.JobPostPhotos ph JOIN dbo.JobPosts p ON p.Id = ph.PostId WHERE p.SeekerUid = @Uid;

SELECT 'photos' AS Container, RTRIM(PhotoId) + '.jpg' AS BlobName FROM @Photos
UNION ALL
SELECT 'verification', DocumentBlob FROM dbo.WorkerVerifications WHERE Uid = @Uid AND DocumentBlob IS NOT NULL;

/* ---------- The wipe ---------- */
BEGIN TRY
    BEGIN TRANSACTION;

    -- Listing: services, towns and work photos go by cascade; references purged by name, like the app.
    DELETE FROM dbo.ProfessionalReferences WHERE ProfessionalId = @Uid;
    DELETE FROM dbo.Professionals          WHERE Id = @Uid;

    -- Chats (messages go by cascade), for both people.
    DELETE FROM dbo.Chats WHERE SeekerId = @Uid OR ProfessionalId = @Uid;

    -- Reviews and household ratings, by them and about them.
    DELETE FROM dbo.Reviews          WHERE SeekerId = @Uid OR ProfessionalId = @Uid;
    DELETE FROM dbo.HouseholdReviews WHERE SeekerUid = @Uid OR ProUid = @Uid;

    -- Safety: blocks both ways go; reports they made lose their name.
    DELETE FROM dbo.Blocks  WHERE BlockerUid = @Uid OR BlockedUid = @Uid;
    UPDATE dbo.Reports SET ReporterUid = NULL WHERE ReporterUid = @Uid;

    DELETE FROM dbo.PushDevices WHERE Uid = @Uid;
    UPDATE dbo.OtherJobNotes SET Uid = NULL WHERE Uid = @Uid;

    -- Feedback board: their votes go and the counts are redone; their suggestions stay, unnamed.
    DECLARE @Voted TABLE (PostId bigint PRIMARY KEY);
    INSERT INTO @Voted (PostId) SELECT PostId FROM dbo.FeedbackVotes WHERE VoterUid = @Uid;
    DELETE FROM dbo.FeedbackVotes WHERE VoterUid = @Uid;
    UPDATE p SET Votes = (SELECT COUNT(*) FROM dbo.FeedbackVotes v WHERE v.PostId = p.Id)
    FROM dbo.FeedbackPosts p WHERE p.Id IN (SELECT PostId FROM @Voted);
    UPDATE dbo.FeedbackPosts SET AuthorUid = NULL WHERE AuthorUid = @Uid;

    -- Job posts (photos and offers on them go by cascade) and the offers they made on others' posts.
    DELETE FROM dbo.JobPosts  WHERE SeekerUid = @Uid;
    DELETE FROM dbo.JobOffers WHERE ProUid = @Uid;

    -- Badge requests (the audit log stays).
    DELETE FROM dbo.WorkerVerifications WHERE Uid = @Uid;

    DELETE FROM dbo.Users WHERE Uid = @Uid;

    IF @DryRun = 1
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'DRY RUN: nothing was deleted. Set @DryRun = 0 to wipe ' + @Uid + N'.';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'Wiped ' + @Uid + N'. Now delete the blobs listed above and the Firebase account.';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
