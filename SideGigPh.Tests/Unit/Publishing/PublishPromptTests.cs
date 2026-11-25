using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// The last setup step before publishing is a number confirmed with an SMS code, and once every step is done the
/// app opens the Publish page by itself, once. If the prompt never fires, a ready worker may never go live; if it
/// fires twice, Back from Publish would bounce them straight back.
/// </summary>
public class PublishPromptTests
{
    [Fact]
    public async Task AccountWithoutNumber_IsNotVerifiedOrReady()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft(), TestData.Worker(phone: null));
        app.Identity.Methods = AuthMethods.Phone | AuthMethods.Google;

        app.Pro.HasVerifiedPhone.Should().BeFalse();
        app.Pro.IsReadyToPublish.Should().BeFalse("the number step isn't done");
        app.Pro.TakePublishPrompt().Should().BeFalse();
    }

    [Fact]
    public async Task NoSignInInTheApp_CountsTheNumberStepAsDone()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft(), TestData.Worker(phone: null));

        app.Pro.HasVerifiedPhone.Should().BeTrue();
    }

    [Fact]
    public async Task LinkingANumber_IsPickedUpAndRaisesChanged()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft(), TestData.Worker(phone: null));
        app.Identity.Methods = AuthMethods.Google;
        var changed = 0;
        app.Pro.Changed += () => changed++;

        app.Identity.User = TestData.Worker();
        await app.Pro.RefreshAccountAsync();

        app.Pro.HasVerifiedPhone.Should().BeTrue();
        app.Pro.IsReadyToPublish.Should().BeTrue();
        changed.Should().Be(1, "the checklist ticks the number step");
    }

    [Fact]
    public async Task RefreshAccount_WithNoChange_DoesNotRaiseChanged()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Identity.Methods = AuthMethods.Phone;
        var changed = 0;
        app.Pro.Changed += () => changed++;

        await app.Pro.RefreshAccountAsync();

        changed.Should().Be(0);
    }

    [Fact]
    public async Task ReadyListing_PromptsOnceOnly()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Identity.Methods = AuthMethods.Phone;

        app.Pro.TakePublishPrompt().Should().BeTrue();
        app.Pro.TakePublishPrompt().Should().BeFalse("Back from Publish must not bounce the worker back");
    }

    [Fact]
    public async Task MissingProof_DoesNotPrompt()
    {
        var draft = TestData.ReadyDraft();
        draft.ProfilePhotoId = null; // the own photo is the one proof publishing needs
        var app = await FakeApp.ProAsync(draft);

        app.Pro.IsReadyToPublish.Should().BeFalse();
        app.Pro.TakePublishPrompt().Should().BeFalse();
    }

    [Fact]
    public async Task PublishedListing_DoesNotPrompt()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        await app.Pro.PublishAsync();

        app.Pro.TakePublishPrompt().Should().BeFalse();
    }

    [Fact]
    public async Task StartOver_PromptsAgainForTheNextListing()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());
        app.Pro.TakePublishPrompt().Should().BeTrue();

        app.Pro.Forget();
        (await app.Pro.EnsureLoadedAsync()).Should().BeTrue();

        app.Pro.TakePublishPrompt().Should().BeTrue();
    }
}
