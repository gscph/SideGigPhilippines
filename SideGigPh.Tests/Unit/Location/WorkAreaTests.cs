using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Location;

/// <summary>
/// "Use my location": only a precise point in the Philippines is used (a rough one would put a worker kilometres off,
/// and the server refuses one abroad), and the worker's base and travel distance reach the server with the listing,
/// as does a post's place. Without a point nothing changes: the server uses the towns' centres.
/// </summary>
public class WorkAreaTests
{
    private static readonly GeoPoint InTaytay = new(14.5600, 121.1300);

    [Fact]
    public void APreciseFixInThePhilippines_IsUsed() =>
        WorkArea.Read(new LocationFix(LocationFixStatus.Found, InTaytay, 25)).Should().Be(LocationOutcome.Set);

    [Fact]
    public void AFixWithoutAnAccuracy_IsUsed() =>
        WorkArea.Read(new LocationFix(LocationFixStatus.Found, InTaytay)).Should().Be(LocationOutcome.Set);

    [Fact]
    public void ARoughFix_IsNotUsed()
    {
        // A phone allowed only "approximate location" answers within a few km.
        WorkArea.Read(new LocationFix(LocationFixStatus.Found, InTaytay, 3000)).Should().Be(LocationOutcome.Rough);
        WorkArea.Read(new LocationFix(LocationFixStatus.Found, InTaytay, WorkArea.RoughAfterMeters)).Should().Be(LocationOutcome.Set);
    }

    [Theory]
    [InlineData(0, 0)] // a GPS glitch
    [InlineData(35.68, 139.69)] // Tokyo
    public void AFixOutsideThePhilippines_IsNotUsed(double latitude, double longitude) =>
        WorkArea.Read(new LocationFix(LocationFixStatus.Found, new GeoPoint(latitude, longitude), 10)).Should().Be(LocationOutcome.Abroad);

    [Fact]
    public void EveryFailure_HasItsOwnOutcome()
    {
        WorkArea.Read(LocationFix.Denied).Should().Be(LocationOutcome.Denied);
        WorkArea.Read(LocationFix.Off).Should().Be(LocationOutcome.Off);
        WorkArea.Read(LocationFix.Failed).Should().Be(LocationOutcome.Failed);
        WorkArea.Read(new LocationFix(LocationFixStatus.Found)).Should().Be(LocationOutcome.Failed);
    }

    [Fact]
    public void TheRadiusChoices_StayInsideWhatTheServerTakes()
    {
        WorkArea.RadiusChoices.Should().OnlyContain(km => km >= WorkArea.MinRadiusKm && km <= WorkArea.MaxRadiusKm);
        WorkArea.RadiusChoices.Should().Contain(WorkArea.DefaultRadiusKm);
        WorkArea.ClampRadius(0).Should().Be(WorkArea.MinRadiusKm);
        WorkArea.ClampRadius(500).Should().Be(WorkArea.MaxRadiusKm);
    }

    [Fact]
    public async Task SettingTheWorkArea_OnALiveListing_SendsTheBaseAndDistance()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        var app = await FakeApp.ProAsync(draft);

        await app.Pro.SetWorkAreaAsync(InTaytay, 20);

        var sent = app.Profiles.Published.Should().ContainSingle().Which;
        sent.BasePoint.Should().Be(InTaytay);
        sent.RadiusKm.Should().Be(20);
        app.Store.State.Professional.BasePoint.Should().Be(InTaytay, "it is kept on the phone for the next publish");
        app.Store.State.Professional.ListingOutOfDate.Should().BeFalse();
    }

    [Fact]
    public async Task ChangingOnlyTheDistance_KeepsTheBase()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        var app = await FakeApp.ProAsync(draft);
        await app.Pro.SetWorkAreaAsync(InTaytay, 20);

        await app.Pro.SetWorkAreaAsync(null, 5);

        var sent = app.Profiles.Published.Last();
        sent.BasePoint.Should().Be(InTaytay);
        sent.RadiusKm.Should().Be(5);
    }

    [Fact]
    public void TheListingRequest_CarriesTheBaseAndDistance_OrLeavesThemOut()
    {
        var draft = TestData.ReadyDraft();
        draft.ToListing(TestData.WorkerUid).ToPublishRequest()
            .Should().Match<SideGigPh.Contracts.PublishListingRequest>(r => r.Latitude == null && r.Longitude == null && r.RadiusKm == null,
                "without them the server keeps what it has");

        draft.BaseLatitude = InTaytay.Latitude;
        draft.BaseLongitude = InTaytay.Longitude;
        draft.RadiusKm = 30;
        var request = draft.ToListing(TestData.WorkerUid).ToPublishRequest();

        request.Latitude.Should().Be(InTaytay.Latitude);
        request.Longitude.Should().Be(InTaytay.Longitude);
        request.RadiusKm.Should().Be(30);
    }

    [Fact]
    public void APostsRequest_CarriesItsPlace_OnlyWhenThePhoneGaveOne()
    {
        var draft = new JobPostDraft { CategoryId = "plumbing", ServiceId = "plumbing-leak", CityId = "taytay", BudgetPesos = 800 };
        draft.ToRequest().Latitude.Should().BeNull("the post then sits at the town's centre");

        draft.Point = InTaytay;
        var request = draft.ToRequest();

        request.Latitude.Should().Be(InTaytay.Latitude);
        request.Longitude.Should().Be(InTaytay.Longitude);
    }
}
