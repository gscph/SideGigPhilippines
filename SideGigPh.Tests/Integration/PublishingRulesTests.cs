using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The anti-fraud rules and publish checks, enforced by the server and the database even when an app is out of date
/// or tampered with: one phone = one listing, one photo = one account, references can't be the worker's own number,
/// and a verified phone is required.
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

        await worker.Pro.AddReferenceAsync("Mang Jose", TestPhone.UniqueNumber()); // a live listing republishes itself

        var listing = await new TestPhone(api, "Viewer").Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid);
        listing!.ReferenceCount.Should().Be(2);
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
    public async Task ReferenceWithOwnNumber_SentByAnOldApp_IsBadReference()
    {
        // An older app without the on-phone check sends it anyway: the server answers invalid-references,
        // and today's app shows the "change your reference" message instead of a generic error.
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Pia");
        var listing = worker.Pro.Listing;

        var result = await worker.Get<IProfessionalProfileRepository>().PublishAsync(listing, [new WorkReference("Me", worker.PhoneNumber!)]);

        result.Should().Be(PublishResult.BadReference);
    }

    [IntegrationFact]
    public async Task AccountWithoutVerifiedPhone_CannotPublish()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Quin");
        worker.Identity.User = new AuthUser { Uid = worker.Uid, DisplayName = "Quin" };

        var publish = () => worker.Get<IProfessionalProfileRepository>().PublishAsync(worker.Pro.Listing, worker.Pro.Draft.References);

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

    [IntegrationFact]
    public async Task WorkPhotoOfAnotherListing_IsPhotoInUse()
    {
        // Anti-fraud rule 2 at the database: even if a photo id is sent by hand, one photo belongs to one listing.
        var first = await TestPhone.WorkerReadyToPublishAsync(api, "Sara");
        await first.Pro.PublishAsync();
        var second = await TestPhone.WorkerReadyToPublishAsync(api, "Sam");
        var stolen = first.Pro.Draft.WorkPhotoIds[0];
        var request = new PublishListingRequest("Sam", "aircon", "rizal", ["aircon-cleaning"], ["taytay"], null, null,
            second.Pro.Draft.ProfilePhotoId, [stolen, second.Pro.Draft.WorkPhotoIds[0]], [new ReferenceDto("Ref", TestPhone.UniqueNumber())]);

        using var response = await api.Client(api.Tokens.For(second.Uid, second.PhoneNumber)).PutAsJsonAsync("professionals/me", request, ApiJson.Default.PublishListingRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.PhotoInUse);
    }
}
