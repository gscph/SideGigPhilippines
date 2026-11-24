using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Tests.Integration.Support;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The anti-fraud rules and publish checks, enforced by the server and the database even when an app is out of date
/// or tampered with: one phone = one listing, one photo = one account, a verified phone is required, and an app from
/// before references were dropped still publishes.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class PublishingRulesTests(ApiTestHost api)
{
    [IntegrationFact]
    public async Task Republish_ReplacesTheListing_InsteadOfAddingASecondOne()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Nilo");
        await worker.Pro.PublishAsync();

        worker.Picker.Next = new PhotoPick(PhotoPickStatus.Picked, [TestImages.Jpeg()]);
        (await worker.Pro.AddWorkPhotosAsync()).Added.Should().Be(1); // a live listing republishes itself

        var viewer = new TestPhone(api, "Viewer").Get<IProfessionalProfileRepository>();
        (await viewer.GetByIdAsync(worker.Uid))!.WorkPhotoUrls.Should().HaveCount(3);
    }

    [IntegrationFact]
    public async Task SecondAccountWithTheSameNumber_IsPhoneTaken()
    {
        // Anti-fraud rule 1, checked by the server: the app can't know another account has this number.
        var first = await TestPhone.WorkerReadyToPublishAsync(api, "Olive");
        await first.Pro.PublishAsync();
        var second = await TestPhone.WorkerReadyToPublishAsync(api, "Oscar");
        second.Identity.User = new AuthUser { Uid = second.Uid, DisplayName = "Oscar", PhoneNumber = first.PhoneNumber };

        // The sign-in provider gives a number to one account at a time, so the first account no longer holds it:
        // its listing (which would ring Oscar) comes down and Oscar's goes up. Still one listing per number.
        (await second.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        (await second.Get<IProfessionalProfileRepository>().GetByIdAsync(first.Uid)).Should().BeNull();
        (await second.Get<IProfessionalProfileRepository>().GetByIdAsync(second.Uid))!.MobileNumber.Should().Be(first.PhoneNumber);
    }

    [IntegrationFact]
    public async Task NumberOnAStaleSeekerAccount_MovesToTheAccountHoldingItNow()
    {
        // Seen live: a person's old customer account kept their number and the new worker account was refused with
        // no way out. The account whose token carries the verified number now holds it; the old one loses it.
        var seeker = new TestPhone(api, "Sally", state: TestData.FinishedSeeker(TestData.AirconCleaningInTaytay()));
        await seeker.Account.SaveProfileAsync();
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Sam");
        worker.Identity.User = new AuthUser { Uid = worker.Uid, DisplayName = "Sam", PhoneNumber = seeker.PhoneNumber };

        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        worker.Pro.IsPublished.Should().BeTrue();
        using var old = await api.Client(api.Tokens.For(seeker.Uid)).GetAsync("users/me");
        (await old.Content.ReadFromJsonAsync(ApiJson.Default.UserDto))!.PhoneNumber.Should().BeNull("one number, one account");
    }

    [IntegrationFact]
    public async Task ProfileClaimingAnotherNumber_SavesTheTokensVerifiedNumber()
    {
        // Otherwise a Seeker profile could claim a worker's number and block their listing.
        var profile = new UserDto("ignored", "Name", "+639170000001", null, Wire.English, Wire.Seeker, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);

        using var withPhone = await api.Client(api.Tokens.For("claims-a", "+639170000002")).PutAsJsonAsync("users/me", profile, ApiJson.Default.UserDto);
        using var withoutPhone = await api.Client(api.Tokens.For("claims-b")).PutAsJsonAsync("users/me", profile, ApiJson.Default.UserDto);

        (await withPhone.Content.ReadFromJsonAsync(ApiJson.Default.UserDto))!.PhoneNumber.Should().Be("+639170000002");
        (await withoutPhone.Content.ReadFromJsonAsync(ApiJson.Default.UserDto))!.PhoneNumber.Should().BeNull();
    }

    [IntegrationFact]
    public async Task OldAppPublishWithReferences_IsPublished_AndTheReferencesAreNotStored()
    {
        // An app from before references were dropped still sends a "references" list (even one with the worker's own
        // number, which used to be refused): the listing goes live and nothing about those people is kept.
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Pia");
        var body = JsonSerializer.Serialize(worker.Pro.Listing.ToPublishRequest(), ApiJson.Default.PublishListingRequest)[..^1]
            + $",\"references\":[{{\"name\":\"Me\",\"mobileNumber\":\"{worker.PhoneNumber}\"}}]}}";
        var client = api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber));

        using var response = await client.PutAsync("professionals/me", new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await api.ReferenceRowsAsync(worker.Uid)).Should().Be(0);
    }

    [IntegrationFact]
    public async Task OldAppReadingItsReferences_GetsAnEmptyList()
    {
        // An app from before references were dropped reads them to restore a live listing on a new phone and fails
        // on a 404, so the route stays and answers an empty list, even for a worker whose old rows are still stored.
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Lea");
        await worker.Pro.PublishAsync();
        await api.AddOldReferenceAsync(worker.Uid, TestPhone.UniqueNumber());

        using var response = await api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber)).GetAsync("professionals/me/references");
        using var anonymous = await api.Client().GetAsync("professionals/me/references");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("[]", "stored references are never read back");
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [IntegrationFact]
    public async Task AccountWithoutVerifiedPhone_CannotPublish()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Quin");
        worker.Identity.User = new AuthUser { Uid = worker.Uid, DisplayName = "Quin" };

        var publish = () => worker.Get<IProfessionalProfileRepository>().PublishAsync(worker.Pro.Listing);

        (await publish.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [IntegrationFact]
    public async Task ListingWithMissingField_IsRefusedWithTheFieldName()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Rico");
        var request = new PublishListingRequest("Rico", "aircon", "rizal", [], ["taytay"], null, null, null, []);

        using var response = await api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber)).PutAsJsonAsync("professionals/me", request, ApiJson.Default.PublishListingRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be("invalid-serviceIds");
    }

    // Work photos are optional: a worker goes live with none, phone -> API -> database and back.
    [IntegrationFact]
    public async Task ListingWithoutWorkPhotos_IsPublished()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Tonyo");
        foreach (var id in worker.Pro.Draft.WorkPhotoIds.ToList())
        {
            await worker.Pro.RemoveWorkPhotoAsync(id);
        }

        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);

        var listing = await new TestPhone(api, "Viewer").Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid);
        listing!.WorkPhotoUrls.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task LiveListing_RemovingEveryWorkPhoto_StaysLive()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Udong");
        await worker.Pro.PublishAsync();

        foreach (var id in worker.Pro.Draft.WorkPhotoIds.ToList())
        {
            await worker.Pro.RemoveWorkPhotoAsync(id); // a live listing republishes itself
        }

        worker.Pro.Draft.ListingOutOfDate.Should().BeFalse("the server took the listing with no work photos");
        var listing = await new TestPhone(api, "Viewer").Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid);
        listing!.WorkPhotoUrls.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task WorkPhotoIdThatIsNotAPhoto_IsRefusedWithTheFieldName()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Vic");
        var request = new PublishListingRequest("Vic", "aircon", "rizal", ["aircon-cleaning"], ["taytay"], null, 800,
            worker.Pro.Draft.ProfilePhotoId, ["not-a-photo"]);

        using var response = await api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber)).PutAsJsonAsync("professionals/me", request, ApiJson.Default.PublishListingRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be("invalid-workPhotoIds");
    }

    [IntegrationFact]
    public async Task ProfilePhotoThatIsNotTheirSelfie_IsRefused_AndTheAppSendsThemToTakeOne()
    {
        // A modified app puts a gallery (work) photo where the selfie goes: refused, the listing stays unpublished.
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Gil");
        var request = new PublishListingRequest("Gil", "aircon", "rizal", ["aircon-cleaning"], ["taytay"], null, 800,
            worker.Pro.Draft.WorkPhotoIds[0], [worker.Pro.Draft.WorkPhotoIds[1]]);

        using var response = await api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber)).PutAsJsonAsync("professionals/me", request, ApiJson.Default.PublishListingRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.SelfieRequired);

        worker.Pro.Draft.ProfilePhotoId = worker.Pro.Draft.WorkPhotoIds[0];
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.SelfieNeeded);
        worker.Pro.Draft.HasProfilePhoto.Should().BeFalse("the selfie step must ask for a new selfie, not offer Next back to the refusal");
    }

    [IntegrationFact]
    public async Task ASelfieThatBreaksTheRules_IsNeverSent()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Hal");
        var blobs = api.Blobs.Blobs.Count;
        using var png = new ImageMagick.MagickImage(ImageMagick.MagickColors.SeaGreen, 100, 100) { Format = ImageMagick.MagickFormat.Png };

        var result = await worker.Pro.SetProfilePhotoAsync(new PhotoPick(PhotoPickStatus.Picked, [png.ToByteArray()]));

        result.Problem.Should().Be(PhotoProblem.Rejected);
        api.Blobs.Blobs.Count.Should().Be(blobs, "the phone checked it and sent nothing");
    }

    [IntegrationFact]
    public async Task WorkPhotoOfAnotherListing_IsPhotoInUse()
    {
        // The database's unique key: even if a photo id is copied by hand, one photo belongs to one listing.
        var first = await TestPhone.WorkerReadyToPublishAsync(api, "Sara");
        await first.Pro.PublishAsync();
        var second = await TestPhone.WorkerReadyToPublishAsync(api, "Sam");
        var stolen = first.Pro.Draft.WorkPhotoIds[0];
        var request = new PublishListingRequest("Sam", "aircon", "rizal", ["aircon-cleaning"], ["taytay"], null, 800,
            second.Pro.Draft.ProfilePhotoId, [stolen, second.Pro.Draft.WorkPhotoIds[0]]);

        using var response = await api.Client(api.Tokens.For(second.Uid, second.PhoneNumber)).PutAsJsonAsync("professionals/me", request, ApiJson.Default.PublishListingRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.PhotoInUse);
    }
}
