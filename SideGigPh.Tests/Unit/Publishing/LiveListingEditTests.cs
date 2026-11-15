using SideGigPh.Maui.Models;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// A live listing edited on the phone must reach customers. Edit listing used to save only on the phone (customers
/// kept the old price and towns), and a photo or reference change whose re-publish failed on weak 4G was never tried
/// again. Now an edit marks the listing out of date until a re-publish goes through.
/// </summary>
public class LiveListingEditTests
{
    [Fact]
    public async Task EditListing_OnALiveListing_RepublishesTheNewAnswers()
    {
        var app = await FakeApp.ProAsync(Live());
        app.Pro.Draft.CityIds.Clear();
        app.Pro.Draft.CityIds.Add("angono");

        var result = await app.Pro.ListingEditedAsync();

        result.Should().Be(PublishResult.Published);
        app.Profiles.Published.Should().ContainSingle().Which.Listing.CityIds.Should().Equal("angono");
        app.Store.State.Professional.ListingOutOfDate.Should().BeFalse();
    }

    [Fact]
    public async Task EditListing_BeforePublishing_SendsNothing()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());

        (await app.Pro.ListingEditedAsync()).Should().BeNull();

        app.Profiles.Published.Should().BeEmpty("the setup's Publish sends it");
        app.Store.State.Professional.ListingOutOfDate.Should().BeFalse();
    }

    [Fact]
    public async Task EditListing_WhenTheSignalDrops_StaysMarkedAndTheNextRefreshSendsIt()
    {
        var app = await FakeApp.ProAsync(Live());
        app.Profiles.PublishThrows = new HttpRequestException("Socket closed");

        var result = await app.Pro.ListingEditedAsync();

        result.Should().BeNull("nobody answered");
        app.Store.State.Professional.ListingOutOfDate.Should().BeTrue("saved, so it survives closing the app");

        app.Profiles.PublishThrows = null;
        await app.Pro.RefreshActivityAsync();

        app.Profiles.Published.Should().HaveCount(2);
        app.Store.State.Professional.ListingOutOfDate.Should().BeFalse();
    }

    [Fact]
    public async Task PhotoChange_WhenTheRepublishFails_IsKeptOnThePhoneWithoutThrowing()
    {
        var app = await FakeApp.ProAsync(Live());
        app.Profiles.PublishThrows = new HttpRequestException("Socket closed");
        app.Picker.Picks(50);

        await app.Pro.ChangeProfilePhotoAsync();

        app.Pro.Draft.ProfilePhotoId.Should().Be(TestData.PhotoId(50));
        app.Store.State.Professional.ListingOutOfDate.Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_WithNothingOutOfDate_DoesNotRepublish()
    {
        var app = await FakeApp.ProAsync(Live());

        await app.Pro.RefreshActivityAsync();

        app.Profiles.Published.Should().BeEmpty();
    }

    private static ProfessionalDraft Live()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        return draft;
    }
}
