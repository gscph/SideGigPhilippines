using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Search;

namespace SideGigPh.Tests.TestSupport;

/// <summary>
/// The app's services (both workspaces and the account) wired to fakes, like <c>MauiProgram</c> wires them to the
/// real adapters. A test sets up the saved answers and the signed-in user, then uses <see cref="Pro"/>,
/// <see cref="Seeker"/> or <see cref="Account"/> exactly as the pages do, and checks the fakes afterwards.
/// </summary>
public sealed class FakeApp
{
    public FakeApp(OnboardingState state, AuthUser? user, AppLanguage language = AppLanguage.English)
    {
        Store = new FakeOnboardingStore(state);
        Identity = new FakeIdentity(user);
        Text.Use(language);
        Pro = new ProWorkspace(Store, Photos, Picker, Chats, Reviews, Profiles, Identity, Safety);
        Seeker = new SeekerWorkspace(Store, new ProfessionalSearch(Profiles), Profiles, Chats, Reviews, Phone, Identity, Picker, Photos, Safety, Text);
        Account = new LocalAccount(Store, Identity, Users, Photos, Profiles, Chats, Reviews, Pro, Seeker, Text, NullLogger<LocalAccount>.Instance);
    }

    public FakeOnboardingStore Store { get; }

    public FakeIdentity Identity { get; }

    public AppText Text { get; } = new();

    public FakeProfiles Profiles { get; } = new();

    public FakeChats Chats { get; } = new();

    public FakeReviews Reviews { get; } = new();

    public FakePhotoStore Photos { get; } = new();

    public FakePhotoPicker Picker { get; } = new();

    public FakeUsers Users { get; } = new();

    public FakeContactLauncher Phone { get; } = new();

    public FakeSafety Safety { get; } = new();

    public ProWorkspace Pro { get; }

    public SeekerWorkspace Seeker { get; }

    public LocalAccount Account { get; }

    /// <summary>A Skilled Worker who finished the interview, with Pro home already opened.</summary>
    public static async Task<FakeApp> ProAsync(ProfessionalDraft draft, AuthUser? user = null)
    {
        var app = new FakeApp(TestData.FinishedPro(draft), user ?? TestData.Worker());
        (await app.Pro.EnsureLoadedAsync()).Should().BeTrue("the worker finished the interview");
        return app;
    }

    /// <summary>A Seeker who finished the interview with <paramref name="answers"/>, with Seeker home already opened.</summary>
    public static async Task<FakeApp> SeekerAsync(SeekerAnswers? answers = null, AuthUser? user = null, Action<FakeApp>? before = null)
    {
        var app = new FakeApp(TestData.FinishedSeeker(answers ?? TestData.AirconCleaningInTaytay()), user ?? TestData.SeekerUser());
        before?.Invoke(app);
        (await app.Seeker.EnsureLoadedAsync()).Should().BeTrue("the Seeker finished the interview");
        return app;
    }
}
