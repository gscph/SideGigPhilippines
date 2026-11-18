using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Photo uploads through the real API (re-encoded, hashed, owned by one account) and the account's own data: the
/// saved profile, and Start over removing everything the person made.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class PhotosAndAccountTests(ApiTestHost api)
{
    [IntegrationFact]
    public async Task UploadedPhoto_IsStoredAsJpegUnderItsHash_AndShownFromItsUrl()
    {
        var phone = new TestPhone(api, "Tess");
        var photos = phone.Get<IPhotoStore>();

        var id = await photos.SaveAsync(TestImages.Jpeg());

        id.Should().MatchRegex("^[0-9a-f]{64}$");
        api.Blobs.Blobs.Should().ContainKey($"{id}.jpg");
        api.Blobs.Blobs[$"{id}.jpg"].Metadata["owner"].Should().Be(phone.Uid);
        (await photos.GetUrlAsync(id)).Should().Be($"{InMemoryBlobContainer.ContainerUri}/{id}.jpg");
    }

    [IntegrationFact]
    public async Task SamePhotoAgain_BySameAccount_IsFine_ByAnotherAccount_IsTaken()
    {
        var photo = TestImages.Jpeg();
        var owner = new TestPhone(api, "Ugo");
        var id = await owner.Get<IPhotoStore>().SaveAsync(photo);

        (await owner.Get<IPhotoStore>().SaveAsync(photo)).Should().Be(id);

        var thief = new TestPhone(api, "Vic");
        var steal = () => thief.Get<IPhotoStore>().SaveAsync(photo);
        (await steal.Should().ThrowAsync<PhotoStoreException>()).Which.Taken.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task WorkerAddsPhotoUsedByAnotherAccount_SeesTheTakenNotice()
    {
        var photo = TestImages.Jpeg();
        await new TestPhone(api, "Wally").Get<IPhotoStore>().SaveAsync(photo);
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Xena");
        worker.Picker.Next = new PhotoPick(PhotoPickStatus.Picked, [photo]);

        var result = await worker.Pro.AddWorkPhotosAsync();

        result.Problem.Should().Be(PhotoProblem.Taken);
    }

    [IntegrationFact]
    public async Task NotAPhoto_IsRefused()
    {
        using var response = await api.Client(api.Tokens.For("someone")).PostAsync("photos", new ByteArrayContent("hello"u8.ToArray()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [IntegrationFact]
    public async Task DeletingSomeoneElsesPhoto_IsForbidden_OwnPhoto_IsDeleted()
    {
        var owner = new TestPhone(api, "Yna");
        var id = await owner.Get<IPhotoStore>().SaveAsync(TestImages.Jpeg());

        using (var response = await api.Client(api.Tokens.For("intruder")).DeleteAsync($"photos/{id}"))
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        await owner.Get<IPhotoStore>().DeleteAsync(id);
        api.Blobs.Blobs.Should().NotContainKey($"{id}.jpg");
    }

    [IntegrationFact]
    public async Task Profile_IsSavedAndReadBack()
    {
        var phone = new TestPhone(api, "Zoe", state: new OnboardingState { Language = AppLanguage.Taglish, Role = UserRole.Seeker });

        await phone.Account.SaveProfileAsync();
        var profile = await phone.Get<IUserRepository>().GetAsync(phone.Uid);

        profile!.DisplayName.Should().Be("Zoe");
        profile.Language.Should().Be(AppLanguage.Taglish);
        profile.Role.Should().Be(UserRole.Seeker);
        profile.PhoneNumber.Should().Be(phone.PhoneNumber);
    }

    [IntegrationFact]
    public async Task StartOver_RemovesTheListingPhotosChatsAndProfile()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Abe");
        await worker.Pro.PublishAsync();
        await worker.Account.SaveProfileAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Bea");
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;
        await seeker.Seeker.SendAsync(listing, "Hi");
        var photoIds = worker.Pro.Draft.WorkPhotoIds.Append(worker.Pro.Draft.ProfilePhotoId!).ToList();

        await worker.Account.StartOverAsync();

        var viewer = new TestPhone(api, "Viewer");
        (await viewer.Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid)).Should().BeNull("the listing is out of search");
        (await worker.Get<IUserRepository>().GetAsync(worker.Uid)).Should().BeNull();
        photoIds.Should().OnlyContain(id => !api.Blobs.Blobs.ContainsKey($"{id}.jpg"));
        await seeker.Seeker.RefreshActivityAsync();
        seeker.Seeker.Threads.Should().BeEmpty("the chat went with the worker");
        worker.Store.Cleared.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task AfterAReinstall_PublishedWorker_GetsTheLiveListingBack_AndLandsOnProHome()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Cid");
        await worker.Pro.PublishAsync();
        var published = worker.Pro.Draft;

        // The same account on a fresh install: nothing on the phone but the language.
        var state = new OnboardingState { Language = AppLanguage.English };
        var reinstalled = new TestPhone(api, "Cid", state: state, uid: worker.Uid);

        (await reinstalled.Get<ListingRestore>().RestoreAsync(state)).Should().BeTrue();

        OnboardingSteps.LaunchPath(state).Should().Be("/professional");
        state.Professional.ServiceIds.Should().BeEquivalentTo(published.ServiceIds);
        state.Professional.WorkPhotoIds.Should().Equal(published.WorkPhotoIds);
        state.Professional.ProfilePhotoId.Should().Be(published.ProfilePhotoId);
        state.Professional.HasProof.Should().BeTrue();
    }
}
