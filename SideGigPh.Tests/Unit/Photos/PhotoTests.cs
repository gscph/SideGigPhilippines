using SideGigPh.Maui.Models;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Photos;

/// <summary>
/// Photos are part of the proof a worker needs to publish (own photo + 2 work photos), and the avatar customers see.
/// Every way picking or uploading can go wrong must end in a clear notice (no permission, upload failed, photo
/// belongs to someone else), never a crash or a silently missing photo.
/// </summary>
public class PhotoTests
{
    private static ProfessionalDraft DraftWithPhotos(int workPhotos)
    {
        var draft = TestData.ReadyDraft();
        draft.WorkPhotoIds = [.. Enumerable.Range(10, workPhotos).Select(TestData.PhotoId)];
        return draft;
    }

    // --- Worker's own photo -------------------------------------------------

    [Fact]
    public async Task ChangeProfilePhoto_Picked_ReplacesAndDeletesTheOldOne()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Picker.Picks(50);

        var result = await app.Pro.ChangeProfilePhotoAsync();

        result.Should().Be(new PhotoAddResult(1, 0));
        app.Pro.Draft.ProfilePhotoId.Should().Be(TestData.PhotoId(50));
        app.Pro.ProfilePhotoUrl.Should().Be(FakePhotoStore.Url(TestData.PhotoId(50)));
        app.Photos.Deleted.Should().Equal(TestData.PhotoId(1));
    }

    [Fact]
    public async Task ChangeProfilePhoto_OnLiveListing_RepublishesWithTheNewPhoto()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        var app = await FakeApp.ProAsync(draft);
        app.Picker.Picks(50);

        await app.Pro.ChangeProfilePhotoAsync();

        app.Profiles.Published.Should().ContainSingle()
            .Which.PhotoUrl.Should().Be(FakePhotoStore.Url(TestData.PhotoId(50)));
    }

    [Fact]
    public async Task Selfie_Taken_BecomesTheirPhoto()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Picker.Picks(50);

        var result = await app.Pro.SetProfilePhotoAsync(await app.Picker.TakeSelfieAsync());

        result.Should().Be(new PhotoAddResult(1, 0));
        app.Picker.Selfies.Should().Be(1);
        app.Picker.LastMaxCount.Should().BeNull("the camera opened, not the gallery");
        app.Pro.Draft.ProfilePhotoId.Should().Be(TestData.PhotoId(50));
    }

    [Fact]
    public async Task Selfie_CameraDenied_SaysSo_AndKeepsTheOldPhoto()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Picker.Next = PhotoPick.CameraDenied;

        var result = await app.Pro.SetProfilePhotoAsync(await app.Picker.TakeSelfieAsync());

        result.Problem.Should().Be(PhotoProblem.NoCamera);
        app.Pro.Draft.ProfilePhotoId.Should().Be(TestData.PhotoId(1));
    }

    [Theory]
    [InlineData(PhotoPickStatus.Cancelled, PhotoProblem.None)]
    [InlineData(PhotoPickStatus.PermissionDenied, PhotoProblem.NoPermission)]
    [InlineData(PhotoPickStatus.Failed, PhotoProblem.CouldNotOpen)]
    public async Task ChangeProfilePhoto_NothingPicked_SaysWhyAndKeepsTheOldPhoto(PhotoPickStatus status, PhotoProblem expected)
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Picker.Next = new PhotoPick(status, []);

        var result = await app.Pro.ChangeProfilePhotoAsync();

        result.Problem.Should().Be(expected);
        app.Pro.Draft.ProfilePhotoId.Should().Be(TestData.PhotoId(1));
    }

    [Theory]
    [InlineData(false, PhotoProblem.UploadFailed)]
    [InlineData(true, PhotoProblem.Taken)] // anti-fraud rule 2: another account has this photo
    public async Task ChangeProfilePhoto_UploadRefused_SaysWhyAndKeepsTheOldPhoto(bool taken, PhotoProblem expected)
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Picker.Picks(50);
        app.Photos.Refuse = new PhotoStoreException("no", taken: taken);

        var result = await app.Pro.ChangeProfilePhotoAsync();

        result.Problem.Should().Be(expected);
        result.Failed.Should().BeTrue();
        app.Pro.Draft.ProfilePhotoId.Should().Be(TestData.PhotoId(1));
    }

    // --- Work photos ----------------------------------------------------------

    [Fact]
    public async Task AddWorkPhotos_AddsNewestFirst_AndAsksOnlyForTheRoomLeft()
    {
        var app = await FakeApp.ProAsync(DraftWithPhotos(2));
        app.Picker.Picks(60, 61);

        var result = await app.Pro.AddWorkPhotosAsync();

        result.Should().Be(new PhotoAddResult(2, 0));
        app.Picker.LastMaxCount.Should().Be(4, "a listing has at most 6 work photos");
        app.Pro.Draft.WorkPhotoIds.Take(2).Should().Equal(TestData.PhotoId(61), TestData.PhotoId(60));
        app.Pro.WorkPhotos.Should().HaveCount(4);
        app.Store.Saves.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddWorkPhotos_SamePhotoAgain_IsSkippedAndCounted()
    {
        var app = await FakeApp.ProAsync(DraftWithPhotos(2));
        app.Picker.Picks(10); // PhotoId(10) is already in the gallery

        var result = await app.Pro.AddWorkPhotosAsync();

        result.Should().Be(new PhotoAddResult(0, 1));
        app.Pro.Draft.WorkPhotoIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddWorkPhotos_GalleryFull_DoesNotOpenThePicker()
    {
        var app = await FakeApp.ProAsync(DraftWithPhotos(6));

        var result = await app.Pro.AddWorkPhotosAsync();

        result.Should().Be(PhotoAddResult.Nothing);
        app.Picker.LastMaxCount.Should().BeNull();
    }

    [Fact]
    public async Task AddWorkPhotos_Cancelled_ChangesNothing()
    {
        var app = await FakeApp.ProAsync(DraftWithPhotos(2));

        var result = await app.Pro.AddWorkPhotosAsync();

        result.Should().Be(new PhotoAddResult(0, 0));
        app.Store.Saves.Should().Be(0);
    }

    [Fact]
    public async Task AddWorkPhotos_UploadFails_ReportsTheProblemAndAddsNothing()
    {
        var app = await FakeApp.ProAsync(DraftWithPhotos(2));
        app.Picker.Picks(60, 61);
        app.Photos.Refuse = new PhotoStoreException("offline");

        var result = await app.Pro.AddWorkPhotosAsync();

        result.Problem.Should().Be(PhotoProblem.UploadFailed);
        result.Added.Should().Be(0);
        app.Pro.Draft.WorkPhotoIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveWorkPhoto_BeforePublishing_RemovesAndDeletesIt()
    {
        var app = await FakeApp.ProAsync(DraftWithPhotos(2));

        await app.Pro.RemoveWorkPhotoAsync(TestData.PhotoId(10));

        app.Pro.Draft.WorkPhotoIds.Should().Equal(TestData.PhotoId(11));
        app.Photos.Deleted.Should().Equal(TestData.PhotoId(10));
    }

    [Fact]
    public async Task RemoveWorkPhoto_AlsoTheProfilePhoto_IsNotDeletedFromStorage()
    {
        // The same picture can be the worker's own photo and a work photo: removing one must not break the other.
        var draft = DraftWithPhotos(3);
        draft.ProfilePhotoId = TestData.PhotoId(10);
        var app = await FakeApp.ProAsync(draft);

        await app.Pro.RemoveWorkPhotoAsync(TestData.PhotoId(10));

        app.Photos.Deleted.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveWorkPhoto_NotInTheGallery_DoesNothing()
    {
        var app = await FakeApp.ProAsync(DraftWithPhotos(2));

        await app.Pro.RemoveWorkPhotoAsync(TestData.PhotoId(99));

        app.Store.Saves.Should().Be(0);
    }

    [Fact]
    public async Task RemoveWorkPhoto_BelowTwoOnPublishedListing_IsNotAllowed()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        var app = await FakeApp.ProAsync(draft);

        await app.Pro.RemoveWorkPhotoAsync(TestData.PhotoId(2));

        app.Pro.CanRemoveWorkPhoto.Should().BeFalse();
        app.Pro.Draft.WorkPhotoIds.Should().HaveCount(2);
    }

    // --- Seeker's own photo -------------------------------------------------

    [Fact]
    public async Task SeekerChangeProfilePhoto_ReplacesAndDeletesTheOldOne()
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.ProfilePhotoId = TestData.PhotoId(5);
        var app = await FakeApp.SeekerAsync(answers);
        app.Seeker.ProfilePhotoUrl.Should().Be(FakePhotoStore.Url(TestData.PhotoId(5)));
        app.Picker.Picks(70);

        var result = await app.Seeker.ChangeProfilePhotoAsync();

        result.Should().Be(new PhotoAddResult(1, 0));
        app.Seeker.Answers.ProfilePhotoId.Should().Be(TestData.PhotoId(70));
        app.Seeker.ProfilePhotoUrl.Should().Be(FakePhotoStore.Url(TestData.PhotoId(70)));
        app.Photos.Deleted.Should().Equal(TestData.PhotoId(5));
    }

    [Fact]
    public async Task SeekerChangeProfilePhoto_SamePhotoAgain_IsNotDeleted()
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.ProfilePhotoId = TestData.PhotoId(5);
        var app = await FakeApp.SeekerAsync(answers);
        app.Picker.Picks(5);

        await app.Seeker.ChangeProfilePhotoAsync();

        app.Photos.Deleted.Should().BeEmpty();
    }

    [Fact]
    public async Task SeekerChangeProfilePhoto_FirstPhoto_DeletesNothing()
    {
        var app = await FakeApp.SeekerAsync();
        app.Picker.Picks(70);

        await app.Seeker.ChangeProfilePhotoAsync();

        app.Photos.Deleted.Should().BeEmpty();
        app.Seeker.Answers.ProfilePhotoId.Should().Be(TestData.PhotoId(70));
    }

    [Fact]
    public async Task SeekerChangeProfilePhoto_PermissionDenied_SaysSo()
    {
        var app = await FakeApp.SeekerAsync();
        app.Picker.Next = PhotoPick.PermissionDenied;

        (await app.Seeker.ChangeProfilePhotoAsync()).Problem.Should().Be(PhotoProblem.NoPermission);
    }

    [Fact]
    public async Task SeekerChangeProfilePhoto_TakenByAnotherAccount_SaysSo()
    {
        var app = await FakeApp.SeekerAsync();
        app.Picker.Picks(70);
        app.Photos.Refuse = new PhotoStoreException("taken", taken: true);

        (await app.Seeker.ChangeProfilePhotoAsync()).Problem.Should().Be(PhotoProblem.Taken);
        app.Seeker.Answers.ProfilePhotoId.Should().BeNull();
    }
}
