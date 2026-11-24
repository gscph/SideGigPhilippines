using MobileAppTheme.Auth;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// A live listing edited on the phone must reach customers. Edit listing used to save only on the phone (customers
/// kept the old price and towns), and a photo change whose re-publish failed on weak 4G was never tried
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
        app.Profiles.Published.Should().ContainSingle().Which.CityIds.Should().Equal("angono");
        app.Store.State.Professional.ListingOutOfDate.Should().BeFalse();
    }

    [Fact]
    public async Task EditListing_LeftAfterANewTrade_IsShownAsUnfinished_WithTheJobsQuestionNext()
    {
        // A new trade clears the jobs; leaving Edit listing there used to keep the old listing live with no word to
        // the worker, and every later photo change silently stayed off it too.
        var app = await FakeApp.ProAsync(Live());
        app.Pro.Draft.PickTrade("plumbing");

        (await app.Pro.ListingEditedAsync()).Should().BeNull("an unfinished listing isn't sent");

        app.Pro.HasUnfinishedEdit.Should().BeTrue();
        app.Pro.UnfinishedEditPath.Should().Be(OnboardingSteps.PathOf(ProStep.Services));
        app.Profiles.Published.Should().BeEmpty("customers keep the old listing until the edit is finished");
    }

    [Fact]
    public async Task UnfinishedEdit_IsNotShown_ForACompleteListing_OrBeforePublishing()
    {
        (await FakeApp.ProAsync(Live())).Pro.HasUnfinishedEdit.Should().BeFalse();

        var draft = TestData.ReadyDraft();
        draft.ServiceIds.Clear();
        (await FakeApp.ProAsync(draft)).Pro.HasUnfinishedEdit.Should().BeFalse("the setup handles a listing not live yet");
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

        await app.Pro.SetProfilePhotoAsync(await app.Picker.TakeSelfieAsync());

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

    [Fact]
    public async Task NameChanged_OnALiveListing_RepublishesWithTheNewName()
    {
        // Me → Change name: a name typed wrong at sign-up reaches customers once fixed.
        var app = await FakeApp.ProAsync(Live());
        await app.Pro.EnsureDraftLoadedAsync();
        app.Identity.User = new AuthUser { Uid = TestData.WorkerUid, DisplayName = "Juan P. Dela Cruz", PhoneNumber = TestData.WorkerNumber };

        await app.Pro.RefreshAccountAsync();
        await WaitForAsync(() => app.Profiles.Published.Count == 1);

        app.Profiles.Published.Single().DisplayName.Should().Be("Juan P. Dela Cruz");
    }

    [Fact]
    public async Task NameChanged_BeforePublishing_SendsNothing()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        await app.Pro.EnsureDraftLoadedAsync();
        app.Identity.User = new AuthUser { Uid = TestData.WorkerUid, DisplayName = "Juan P. Dela Cruz", PhoneNumber = TestData.WorkerNumber };

        await app.Pro.RefreshAccountAsync();

        app.Pro.Draft.DisplayName.Should().Be("Juan P. Dela Cruz", "the setup's Publish sends it");
        app.Profiles.Published.Should().BeEmpty();
    }

    // The re-publish after a name change isn't awaited by the page change; with fakes it finishes almost at once.
    private static async Task WaitForAsync(Func<bool> done)
    {
        for (var i = 0; i < 50 && !done(); i++)
        {
            await Task.Delay(10);
        }
    }

    private static ProfessionalDraft Live()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        return draft;
    }
}
