using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// After a reinstall (or on a new phone) the account comes back from the server, phone → API → database and back: a
/// household's setup (<c>Users.SeekerFinishedAt</c>, home town and barangay, <c>035-schema-user-restore.sql</c>), both
/// sides of one account, and a fresh phone's first profile save never wiping what the server kept. Every write here
/// loses its first answer (<see cref="DroppingConnection.LoseFirstAnswers"/>), so each is proven safe twice.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class ReinstallRestoreTests(ApiTestHost api)
{
    // The same account on a fresh install: nothing on the phone but the language.
    private TestPhone Reinstalled(TestPhone before, out OnboardingState state)
    {
        state = new OnboardingState { Language = AppLanguage.English };
        return new TestPhone(api, "Again", state: state, uid: before.Uid);
    }

    private static string Photo(char last) => new string('b', 63) + last;

    private async Task<UserDto> SavedProfileAsync(string uid)
    {
        using var response = await api.Client(api.Tokens.For(uid)).GetAsync("users/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync(ApiJson.Default.UserDto))!;
    }

    private async Task<UserDto> PutProfileAsync(string uid, UserDto body)
    {
        using var response = await api.Client(api.Tokens.For(uid)).PutAsJsonAsync("users/me", body, ApiJson.Default.UserDto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync(ApiJson.Default.UserDto))!;
    }

    private static UserDto Body(string? role = null, string? photo = null, DateTimeOffset? finished = null, string? town = null, string? barangay = null) =>
        new("ignored", "Name", null, null, Wire.English, role, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, photo, finished, town, barangay);

    [IntegrationFact]
    public async Task Household_AfterAReinstall_LandsOnSeekerHome_WithItsTownAndBarangay()
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.BarangayId = ReferenceData.BarangaysOf("taytay")[0].Id;
        var household = await TestPhone.SeekerAsync(api, "Lorna", answers);
        await household.Account.SaveProfileAsync(); // the interview's finish saves it

        var phone = Reinstalled(household, out var state);
        (await phone.Get<AccountRestore>().RestoreAsync(state)).Should().Be(AccountRestoreResult.Restored);

        OnboardingSteps.LaunchPath(state).Should().Be(SeekerRoutes.Home, "no role question, no interview again");
        state.Role.Should().Be(UserRole.Seeker);
        state.SeekerFinishedAt.Should().NotBeNull();
        state.Seeker.CityId.Should().Be("taytay");
        state.Seeker.BarangayId.Should().Be(answers.BarangayId);
        (await phone.Seeker.EnsureLoadedAsync()).Should().BeTrue("Seeker home loads");
    }

    [IntegrationFact]
    public async Task BothSides_AfterAReinstall_ComeBack_OnTheSideLastUsed()
    {
        var person = await TestPhone.WorkerReadyToPublishAsync(api, "Dado");
        (await person.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        var before = person.Store.State;
        before.Seeker = TestData.AirconCleaningInTaytay();
        before.SeekerFinishedAt = DateTimeOffset.Now;
        (await person.Account.SwitchSideAsync(UserRole.Seeker)).Should().Be(SeekerRoutes.Home);
        await person.Account.SaveProfileAsync(); // the switch saves in the background: wait for it here

        var phone = Reinstalled(person, out var state);
        (await phone.Get<AccountRestore>().RestoreAsync(state)).Should().Be(AccountRestoreResult.Restored);

        state.Role.Should().Be(UserRole.Seeker, "the side they last used");
        state.HasFinished(UserRole.Seeker).Should().BeTrue();
        state.HasFinished(UserRole.Professional).Should().BeTrue();
        OnboardingSteps.LaunchPath(state).Should().Be(SeekerRoutes.Home);
        (await phone.Account.SwitchSideAsync(UserRole.Professional)).Should().Be(ProRoutes.Home, "the live listing came back too");
    }

    [IntegrationFact]
    public async Task FreshPhonesFirstSave_KeepsTheSideHouseholdAndPhotoTheServerHas()
    {
        var answers = TestData.AirconCleaningInTaytay();
        var household = await TestPhone.SeekerAsync(api, "Nena", answers);
        household.Store.State.Seeker.ProfilePhotoId = Photo('1');
        await household.Account.SaveProfileAsync();
        var saved = await SavedProfileAsync(household.Uid);

        // A fresh install that signs in and saves before it knows anything (the language is all it has).
        var fresh = new TestPhone(api, "Nena", verifiedPhone: false, state: new OnboardingState { Language = AppLanguage.Taglish }, uid: household.Uid);
        await fresh.Account.SaveProfileAsync();

        var after = await SavedProfileAsync(household.Uid);
        after.Role.Should().Be(Wire.Seeker);
        after.ProfilePhotoId.Should().Be(Photo('1'));
        after.SeekerFinishedAt.Should().Be(saved.SeekerFinishedAt);
        after.HomeTownId.Should().Be("taytay");
        after.Language.Should().Be(Wire.Taglish, "the language just picked on the phone wins");
    }

    [IntegrationFact]
    public async Task HouseholdFinishDate_TheFirstStays()
    {
        var uid = $"finish-{Guid.NewGuid():N}";
        var first = new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

        await PutProfileAsync(uid, Body(Wire.Seeker, finished: first));
        var again = await PutProfileAsync(uid, Body(Wire.Seeker, finished: first.AddDays(5)));

        again.SeekerFinishedAt.Should().Be(first, "finishing again on a new phone doesn't move the first date");
        (await PutProfileAsync(uid, Body(Wire.Professional))).SeekerFinishedAt.Should().Be(first, "never cleared");
    }

    [IntegrationFact]
    public async Task HomeTown_OnlyAServedTown_AndOnlyThatTownsBarangay()
    {
        var uid = $"home-{Guid.NewGuid():N}";
        var taytayBarangay = ReferenceData.BarangaysOf("taytay")[0].Id;

        (await PutProfileAsync(uid, Body(town: "taytay", barangay: taytayBarangay))).HomeBarangayId.Should().Be(taytayBarangay);

        var unserved = await PutProfileAsync(uid, Body(town: "makati", barangay: "makati-poblacion"));
        unserved.HomeTownId.Should().Be("taytay", "a town we don't serve is ignored");
        unserved.HomeBarangayId.Should().Be(taytayBarangay);

        var otherTowns = await PutProfileAsync(uid, Body(town: "angono", barangay: taytayBarangay));
        otherTowns.HomeTownId.Should().Be("angono");
        otherTowns.HomeBarangayId.Should().BeNull("Taytay's barangay isn't Angono's");

        (await PutProfileAsync(uid, Body())).HomeTownId.Should().Be("angono", "a save without a town keeps it");
    }
}
