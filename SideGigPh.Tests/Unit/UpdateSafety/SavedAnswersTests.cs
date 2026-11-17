using System.Text.Json;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Local;

namespace SideGigPh.Tests.Unit.UpdateSafety;

/// <summary>
/// Every user's interview answers sit in <c>onboarding.json</c> on their phone. After an update the new app must
/// still read the file the old app wrote; otherwise people lose their answers or the app gets stuck at launch.
/// The JSON below is what older versions wrote: never edit it to make a test pass, add a new case instead.
/// </summary>
public class SavedAnswersTests
{
    private static OnboardingState Load(string json) =>
        JsonSerializer.Deserialize(json, OnboardingJsonContext.Default.OnboardingState)!.DropUnservedTowns();

    // Written when the app served four areas (Metro Manila, Rizal, Laguna, Cavite).
    private const string FourAreaVersionJson = """
        {
          "Language": "Taglish",
          "Role": "Professional",
          "ResumePath": "/start/pro/rate",
          "Seeker": { "CategoryId": "aircon", "AreaId": "metro-manila", "CityId": "makati" },
          "Professional": {
            "CategoryId": "plumbing",
            "ServiceIds": ["plumbing-clog", "plumbing-leak"],
            "AreaId": "metro-manila",
            "CityIds": ["makati", "taytay", "pasig"],
            "ExperienceId": "1-3",
            "RateTierId": "500-1000",
            "DisplayName": "Juan",
            "MobileNumber": "+639171234567"
          },
          "CompletedAt": "2026-06-01T08:00:00+08:00"
        }
        """;

    [Fact]
    public void OldFile_StillLoads_WithLanguageRoleAndAnswers()
    {
        var state = Load(FourAreaVersionJson);

        state.Language.Should().Be(AppLanguage.Taglish);
        state.Role.Should().Be(UserRole.Professional);
        state.Professional.CategoryId.Should().Be("plumbing");
        state.Professional.ServiceIds.Should().Equal("plumbing-clog", "plumbing-leak");
        state.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void OldFile_TownsWeNoLongerServe_AreDropped_AndAreaFollowsTheTownsLeft()
    {
        var state = Load(FourAreaVersionJson);

        state.Professional.CityIds.Should().Equal("taytay");
        state.Professional.AreaId.Should().Be(ReferenceData.ServiceAreaId);
    }

    [Fact]
    public void OldFile_SeekerTownWeNoLongerServe_IsCleared_SoTheTownQuestionIsAskedAgain()
    {
        var state = Load(FourAreaVersionJson);

        state.Seeker.CityId.Should().BeNull();
        state.Seeker.AreaId.Should().BeNull();
    }

    // Written before photos and references existed: the new lists must start empty, not null.
    [Fact]
    public void FileWithoutNewerFields_LoadsWithEmptyLists()
    {
        var state = Load("""{ "Role": "Professional", "Professional": { "CategoryId": "aircon" } }""");

        state.Professional.WorkPhotoIds.Should().BeEmpty();
        state.Professional.References.Should().BeEmpty();
        state.Professional.PublishedAt.Should().BeNull();
    }

    // Written before recent searches and "search or post": the list starts empty, the question is unanswered.
    [Fact]
    public void FileWithoutRecentSearches_LoadsWithAnEmptyList()
    {
        var state = Load("""{ "Role": "Seeker", "Seeker": { "CategoryId": "aircon", "CityId": "taytay" } }""");

        state.Seeker.RecentSearches.Should().BeEmpty();
        state.Seeker.FindBy.Should().BeNull();
    }

    [Fact]
    public void RecentSearches_SurviveSavingAndLoading()
    {
        var saved = new OnboardingState
        {
            Role = UserRole.Seeker,
            Seeker = new SeekerAnswers
            {
                FindBy = ReferenceData.FindByPost,
                RecentSearches = [new RecentSearch("plumbing", "plumbing-leak", null, "angono", "800-1500")],
            },
        };

        var state = Load(JsonSerializer.Serialize(saved, OnboardingJsonContext.Default.OnboardingState));

        state.Seeker.FindBy.Should().Be(ReferenceData.FindByPost);
        state.Seeker.RecentSearches.Should().Equal(new RecentSearch("plumbing", "plumbing-leak", null, "angono", "800-1500"));
    }

    // A newer app may write fields an older one doesn't know (e.g. a downgrade, or a field removed later).
    [Fact]
    public void FileWithUnknownFields_StillLoads()
    {
        var state = Load("""{ "Language": "English", "SomethingNew": 42, "Seeker": { "CityId": "antipolo", "Extra": "x" } }""");

        state.Language.Should().Be(AppLanguage.English);
        state.Seeker.CityId.Should().Be("antipolo");
        state.Seeker.AreaId.Should().Be(ReferenceData.ServiceAreaId);
    }

    [Fact]
    public void EmptyFile_LoadsAsAFreshStart() =>
        Load("{}").Role.Should().BeNull();

    [Fact]
    public void SavedState_RoundTrips_WithEnumsAsWords()
    {
        // Enums are saved as words ("Professional"), so reordering the enum can't change what a saved file means.
        var state = new OnboardingState
        {
            Language = AppLanguage.Taglish,
            Role = UserRole.Seeker,
            Seeker = new SeekerAnswers { CategoryId = "aircon", CityId = "taytay", AreaId = "rizal", BudgetId = "unsure" },
            Professional = { References = [new WorkReference("Aling Nena", "+639181111111")] },
        };

        var json = JsonSerializer.Serialize(state, OnboardingJsonContext.Default.OnboardingState);
        var back = Load(json);

        json.Should().Contain("\"Role\":\"Seeker\"").And.Contain("\"Language\":\"Taglish\"");
        back.Seeker.CityId.Should().Be("taytay");
        back.Professional.References.Should().ContainSingle().Which.Name.Should().Be("Aling Nena");
    }
}
