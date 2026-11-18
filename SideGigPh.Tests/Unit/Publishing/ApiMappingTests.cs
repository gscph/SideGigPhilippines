using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// The app turns its listing into the JSON the API takes (<c>ApiMapping</c>), and the API's answers back into
/// listings. A slip here (a photo id lost, the area missing) makes the server refuse every publish.
/// </summary>
public class ApiMappingTests
{
    [Fact]
    public void ToPublishRequest_ReadsPhotoIdsFromBlobUrls()
    {
        var draft = TestData.ReadyDraft();
        var listing = draft.ToListing(
            TestData.WorkerUid,
            $"{FakePhotoStore.BaseUrl}{TestData.PhotoId(1)}.jpg",
            [$"{FakePhotoStore.BaseUrl}{TestData.PhotoId(2)}.jpg", $"{FakePhotoStore.BaseUrl}{TestData.PhotoId(3)}.jpg"]);

        var request = listing.ToPublishRequest();

        request.ProfilePhotoId.Should().Be(TestData.PhotoId(1));
        request.WorkPhotoIds.Should().Equal(TestData.PhotoId(2), TestData.PhotoId(3));
    }

    [Fact]
    public void ToPublishRequest_LeavesOutPhotosThatAreNotStoredYet()
    {
        // An old on-device photo (data URL) has no id on the server: sending it would fail the whole publish.
        var listing = TestData.ReadyDraft().ToListing(TestData.WorkerUid, "data:image/jpeg;base64,AAAA", ["data:image/jpeg;base64,BBBB"]);

        var request = listing.ToPublishRequest();

        request.ProfilePhotoId.Should().BeNull();
        request.WorkPhotoIds.Should().BeEmpty();
    }

    [Fact]
    public void ToPublishRequest_SetsAreaFromTown()
    {
        var draft = TestData.ReadyDraft();

        var request = draft.ToListing(TestData.WorkerUid).ToPublishRequest();

        request.AreaId.Should().Be(ReferenceData.ServiceAreaId);
        request.CategoryId.Should().Be("aircon");
    }

    [Fact]
    public void ToPublishRequest_SendsNoReferences()
    {
        // References were dropped: the request has no such field, so no name or number of a third person is sent.
        var json = System.Text.Json.JsonSerializer.Serialize(
            TestData.ReadyDraft().ToListing(TestData.WorkerUid).ToPublishRequest(), ApiJson.Default.PublishListingRequest);

        json.Should().NotContain("reference", "the app no longer collects or sends references");
    }

    [Fact]
    public void ListingDto_ToModel_KeepsStarsAndPhotos()
    {
        var dto = new ListingDto(
            "pro-1", "Maria", "+639171234567", "plumbing", "rizal", ["plumbing-clog"], ["antipolo"], "1-3", "under-500",
            new PhotoDto(TestData.PhotoId(1), "https://x/p.jpg"),
            [new PhotoDto(TestData.PhotoId(2), "https://x/w.jpg")],
            new ReviewSummaryDto(4.5, 12));

        var listing = dto.ToModel();

        listing.CategoryIds.Should().Equal("plumbing");
        listing.PhotoUrl.Should().Be("https://x/p.jpg");
        listing.WorkPhotoUrls.Should().Equal("https://x/w.jpg");
        listing.Reviews.Should().Be(new ReviewSummary(4.5, 12));
    }

    [Fact]
    public void UserProfile_RoundTripsThroughTheWire()
    {
        var profile = new UserProfile("uid", "Juan", "+639171234567", null, AppLanguage.Taglish, UserRole.Professional,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, TestData.PhotoId(1));

        var dto = profile.ToDto();

        dto.ToModel().Should().Be(profile);
        dto.IncomeGoalId.Should().BeNull("the earnings goal is no longer asked");
    }

    [Fact]
    public void CallMessage_IsSentWithoutText()
    {
        var chat = new Chat("p_s", "p", "s", "Pro", "Seeker", null, null, null, null,
            new ChatMessage("m0", "p_s", "s", ChatMessageKind.Text, "hi", DateTimeOffset.UnixEpoch), null, null, DateTimeOffset.UnixEpoch);
        var call = new ChatMessage("m1", "p_s", "s", ChatMessageKind.Call, "ignored", DateTimeOffset.UnixEpoch);

        var request = chat.ToSendRequest(call);

        request.Kind.Should().Be(Wire.Call);
        request.Text.Should().BeNull();
    }
}
