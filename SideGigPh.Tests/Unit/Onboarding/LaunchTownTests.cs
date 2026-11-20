using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Tests.Unit.Onboarding;

/// <summary>
/// The narrower launch: one town first (the server's default town, Angono in the built-in copy) and barangays as
/// optional server data. The default town starts picked wherever a town is picked but never locks the others; a
/// barangay never blocks anything and never sticks to the wrong town or a barangay we no longer serve.
/// These read the built-in config (<see cref="ReferenceData.BuiltInConfig"/>), as every unit test does.
/// </summary>
public class LaunchTownTests
{
    [Fact]
    public void TheBuiltInCopy_StartsWithAngono_AndKnowsEachTownsBarangays()
    {
        ReferenceData.DefaultTownId.Should().Be("angono");
        ReferenceData.BarangaysOf("angono").Should().HaveCount(10);
        ReferenceData.BarangaysOf("taytay").Should().HaveCount(5);
        ReferenceData.BarangaysOf("antipolo").Should().HaveCount(16);
        ReferenceData.BarangaysOf("makati").Should().BeEmpty("a town we don't serve has no list");
        ReferenceData.BarangaysOf(null).Should().BeEmpty();
        ReferenceData.FindBarangay("angono", "angono-santo-nino")!.Label.English.Should().Be("Santo Niño");
        ReferenceData.FindBarangay("taytay", "angono-san-isidro").Should().BeNull("a barangay belongs to one town");
    }

    [Fact]
    public void BarangayIds_AreStableCodes_WithNoAccents()
    {
        var ids = ReferenceData.BuiltInConfig.BarangaysByTown!.SelectMany(town => town.Value.Select(b => (Town: town.Key, b.Id))).ToList();

        ids.Select(b => b.Id).Should().OnlyHaveUniqueItems();
        ids.Should().OnlyContain(b => b.Id.StartsWith(b.Town + "-") && System.Text.RegularExpressions.Regex.IsMatch(b.Id, "^[a-z0-9-]+$"));
    }

    // --- Skilled Worker: the towns question ------------------------------------

    [Fact]
    public void AWorkerWithNoTowns_StartsWithTheDefaultTownTicked_AndCanTickOthers()
    {
        var draft = new ProfessionalDraft();

        draft.PreselectDefaultTown();
        draft.ToggleTown("taytay");

        draft.CityIds.Should().Equal("angono", "taytay");
        draft.AreaId.Should().Be(ReferenceData.ServiceAreaId);
    }

    [Fact]
    public void AWorkerWhoPickedTowns_KeepsThem()
    {
        var draft = new ProfessionalDraft { CityIds = ["antipolo"] };

        draft.PreselectDefaultTown();

        draft.CityIds.Should().Equal("antipolo");
    }

    [Fact]
    public void AWorkerCanUntickTheDefaultTown()
    {
        var draft = new ProfessionalDraft();
        draft.PreselectDefaultTown();

        draft.ToggleTown("angono");
        draft.ToggleTown("taytay");

        draft.CityIds.Should().Equal("taytay");
    }

    // --- Household: the town question, the post form, search ------------------

    [Fact]
    public void AHouseholdWithNoTown_SeesTheDefaultTownPicked_ElseTheirOwn()
    {
        new SeekerAnswers().TownShown().Should().Be("angono");
        new SeekerAnswers { CityId = "taytay" }.TownShown().Should().Be("taytay");
    }

    [Fact]
    public void PickingATown_KeepsOnlyABarangayOfThatTown()
    {
        var answers = new SeekerAnswers();
        answers.PickBarangay("angono", "angono-san-isidro");

        answers.PickTown("angono");
        answers.BarangayId.Should().Be("angono-san-isidro", "the default town was confirmed with its barangay");

        answers.PickTown("taytay");
        answers.BarangayId.Should().BeNull("San Isidro of Angono is not in Taytay");
        answers.AreaId.Should().Be(ReferenceData.ServiceAreaId);
    }

    [Theory]
    [InlineData("angono-bagumbayan", "angono-bagumbayan")]
    [InlineData(null, null)]
    [InlineData("taytay-muzon", null)]
    [InlineData("angono-old-barangay", null)]
    public void PickingABarangay_KeepsOnlyOneWeServeInTheTown(string? picked, string? kept)
    {
        var answers = new SeekerAnswers { CityId = "angono", BarangayId = "angono-kalayaan" };

        answers.PickBarangay("angono", picked);

        answers.BarangayId.Should().Be(kept);
    }

    [Fact]
    public void APostInTheirHomeTown_RemembersItsBarangay_ForTheNextPost()
    {
        var answers = new SeekerAnswers { CityId = "angono" };
        var post = new JobPostDraft
        {
            CategoryId = "construction", ServiceId = "construction-plumbing", CityId = "angono", BarangayId = "angono-san-roque", BudgetPesos = 800,
        };

        answers.RememberPost(post);
        answers.BarangayId.Should().Be("angono-san-roque");

        answers.RememberPost(new JobPostDraft { CategoryId = "construction", CityId = "taytay", BarangayId = "taytay-muzon", BudgetPesos = 800 });
        answers.BarangayId.Should().Be("angono-san-roque", "a post in another town says nothing about where they live");
    }

    [Fact]
    public void SavedBarangay_RetiredOrOfAnotherTown_IsDropped_WithoutAskingAgain()
    {
        var retired = new OnboardingState { Seeker = { CityId = "angono", BarangayId = "angono-retired" }, SeekerFinishedAt = DateTimeOffset.UnixEpoch };
        var otherTown = new OnboardingState { Seeker = { CityId = "taytay", BarangayId = "angono-san-isidro" } };
        var kept = new OnboardingState { Seeker = { CityId = "taytay", BarangayId = "taytay-dolores" } };

        retired.FitToCatalog().Seeker.BarangayId.Should().BeNull();
        retired.Seeker.CityId.Should().Be("angono", "only the barangay goes; the town is still served");
        retired.Seeker.FirstUnanswered().Should().NotBe(SeekerStep.City, "a barangay is optional, so nothing is asked again");
        otherTown.FitToCatalog().Seeker.BarangayId.Should().BeNull();
        kept.FitToCatalog().Seeker.BarangayId.Should().Be("taytay-dolores");
    }

    [Fact]
    public void APostShowsTheTownAndBarangay_ElseJustTheTown()
    {
        var text = new AppText();

        text.Place("angono", "angono-san-isidro").Should().Be("Angono · Brgy. San Isidro");
        text.Place("angono", null).Should().Be("Angono");
        text.Place("angono", "angono-retired").Should().Be("Angono", "a retired barangay is not shown");
        text.Place("makati", null).Should().Be("makati", "a town we no longer serve shows its id");
    }

    // --- The wire ------------------------------------------------------------------

    [Fact]
    public void ThePostSendsItsBarangay_OnlyWhenItIsInTheTown()
    {
        var draft = new JobPostDraft { CategoryId = "construction", ServiceId = "construction-plumbing", CityId = "angono", BarangayId = "angono-san-pedro" };

        draft.ToRequest().BarangayId.Should().Be("angono-san-pedro");
        draft.CityId = "taytay";
        draft.ToRequest().BarangayId.Should().BeNull();
    }

    [Fact]
    public void ServerConfig_GivesTheDefaultTown_AndBarangaysByTown_InOrder()
    {
        var dto = ReferenceDataDto(
            "taytay",
            [
                new BarangayDto("taytay-muzon", "taytay", "Muzon", "Muzon"),
                new BarangayDto("angono-kalayaan", "angono", "Kalayaan", "Kalayaan"),
                new BarangayDto("taytay-dolores", "taytay", "Dolores", "Dolores"),
            ]);

        var config = dto.ToModel();

        config.DefaultTownId.Should().Be("taytay");
        config.BarangaysByTown!["taytay"].Select(b => b.Id).Should().Equal("taytay-muzon", "taytay-dolores");
        config.BarangaysByTown["angono"].Single().Label.Should().Be(new LocalizedText("Kalayaan", "Kalayaan"));
        ReferenceDataDto(null, null).ToModel().BarangaysByTown.Should().BeNull("a server before barangays sends none");
    }

    private static ConfigDto ReferenceDataDto(string? defaultTown, List<BarangayDto>? barangays) => new(
        [new CategoryDto("aircon", "Aircon", "Aircon", null, null, null, [new ServiceDto("aircon-other", "Others", "Wala sa nabanggit", null)])],
        [new OptionDto("angono", "Angono", "Angono", null, 0), new OptionDto("taytay", "Taytay", "Taytay", null, 0)],
        [], [], [], defaultTown, barangays);
}
