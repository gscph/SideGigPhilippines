using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.Unit.Onboarding;

/// <summary>
/// The first minutes in the app: the interview questions and where the app opens. A wrong rule here strands people
/// on a blank step, loops them back to a question they answered, or skips one the listing needs. These test the
/// rules the interview pages use (<see cref="OnboardingSteps"/>).
/// </summary>
public class OnboardingStepsTests
{
    private static ProfessionalDraft FullyAnsweredDraft() => new()
    {
        CategoryId = "plumbing",
        ServiceIds = ["plumbing-clog"],
        CityIds = ["taytay"],
        AreaId = "rizal",
        ExperienceId = "1-3",
        RateTierId = "quote",
        IncomeGoalId = "10000",
    };

    // --- Skilled Worker interview -----------------------------------------

    public static TheoryData<string, ProStep> ProMissingAnswers => new()
    {
        { "trade", ProStep.Skills },
        { "jobs", ProStep.Services },
        { "towns", ProStep.Towns },
        { "experience", ProStep.Experience },
        { "rate", ProStep.Rate },
        { "goal", ProStep.Goal },
    };

    [Theory]
    [MemberData(nameof(ProMissingAnswers))]
    public void ProFirstUnanswered_IsTheFirstMissingQuestion(string missing, ProStep expected)
    {
        var draft = FullyAnsweredDraft();
        switch (missing)
        {
            case "trade": draft.CategoryId = null; break;
            case "jobs": draft.ServiceIds.Clear(); break;
            case "towns": draft.CityIds.Clear(); break;
            case "experience": draft.ExperienceId = null; break;
            case "rate": draft.RateTierId = null; break;
            case "goal": draft.IncomeGoalId = null; break;
        }

        draft.FirstUnanswered().Should().Be(expected);
    }

    [Fact]
    public void ProFirstUnanswered_AllAnswered_IsThePreview() =>
        FullyAnsweredDraft().FirstUnanswered().Should().Be(ProStep.Preview);

    [Theory]
    [InlineData(null, ProStep.Skills)]
    [InlineData(-5, ProStep.Skills)]   // nonsense in the URL
    [InlineData(3, ProStep.Towns)]
    [InlineData(99, ProStep.Preview)]  // past the end
    public void ProStepToShow_FullyAnswered_ShowsTheAskedStepWithinRange(int? requested, ProStep expected) =>
        FullyAnsweredDraft().StepToShow(requested).Should().Be(expected);

    [Fact]
    public void ProStepToShow_NeverSkipsAnUnansweredQuestion()
    {
        // A stale resume path asks for the rate, but the towns were never picked.
        var draft = FullyAnsweredDraft();
        draft.CityIds.Clear();

        draft.StepToShow((int)ProStep.Rate).Should().Be(ProStep.Towns);
    }

    [Fact]
    public void PickTrade_DifferentTrade_ClearsTheJobsOfTheOldTrade()
    {
        var draft = FullyAnsweredDraft();

        draft.PickTrade("aircon");

        draft.CategoryId.Should().Be("aircon");
        draft.ServiceIds.Should().BeEmpty();
    }

    [Fact]
    public void PickTrade_SameTrade_KeepsTheJobs()
    {
        var draft = FullyAnsweredDraft();

        draft.PickTrade("plumbing");

        draft.ServiceIds.Should().Equal("plumbing-clog");
    }

    [Fact]
    public void ToggleService_AddsThenRemoves()
    {
        var draft = new ProfessionalDraft();

        draft.ToggleService("aircon-leak");
        draft.ServiceIds.Should().Equal("aircon-leak");

        draft.ToggleService("aircon-leak");
        draft.ServiceIds.Should().BeEmpty();
    }

    [Fact]
    public void ToggleTown_SetsTheAreaFromTheTowns_AndClearsItWithNoTowns()
    {
        var draft = new ProfessionalDraft();

        draft.ToggleTown("antipolo");
        draft.AreaId.Should().Be(ReferenceData.ServiceAreaId, "the API needs the area on the listing");

        draft.ToggleTown("antipolo");
        draft.AreaId.Should().BeNull();
    }

    [Fact]
    public void ToggleAllTowns_AddsEveryServedTown_ThenRemovesThem()
    {
        var draft = new ProfessionalDraft { CityIds = ["taytay"] };

        draft.ToggleAllTowns();
        draft.CityIds.Should().BeEquivalentTo(ReferenceData.Cities.Select(c => c.Id));
        draft.AreaId.Should().Be(ReferenceData.ServiceAreaId);

        draft.ToggleAllTowns();
        draft.CityIds.Should().BeEmpty();
        draft.AreaId.Should().BeNull();
    }

    [Theory]
    [InlineData(ProStep.Towns, false, "/start/pro/2")]
    [InlineData(ProStep.Skills, false, OnboardingSteps.RolePath)]
    [InlineData(ProStep.Skills, true, "/professional")]
    public void ProBackPath_GoesToThePreviousQuestion_OrOutOfTheInterview(ProStep step, bool finished, string expected) =>
        OnboardingSteps.BackPath(step, finished).Should().Be(expected);

    // --- Seeker interview ---------------------------------------------------

    private static SeekerAnswers FullSeeker() => new()
    {
        CategoryId = "aircon",
        ServiceId = "aircon-cleaning",
        CityId = "taytay",
        UrgencyId = "today",
        BudgetId = "unsure",
    };

    [Theory]
    [InlineData("category", SeekerStep.Category)]
    [InlineData("service", SeekerStep.Service)]
    [InlineData("city", SeekerStep.City)]
    [InlineData("when", SeekerStep.When)]
    [InlineData("budget", SeekerStep.Budget)]
    [InlineData("none", SeekerStep.Finding)]
    public void SeekerFirstUnanswered_IsTheFirstMissingQuestion(string missing, SeekerStep expected)
    {
        var answers = FullSeeker();
        switch (missing)
        {
            case "category": answers.CategoryId = null; break;
            case "service": answers.ServiceId = null; break;
            case "city": answers.CityId = null; break;
            case "when": answers.UrgencyId = null; break;
            case "budget": answers.BudgetId = null; break;
        }

        answers.FirstUnanswered().Should().Be(expected);
    }

    [Theory]
    [InlineData(null, SeekerStep.Category)]
    [InlineData(0, SeekerStep.Category)]
    [InlineData(4, SeekerStep.When)]
    [InlineData(42, SeekerStep.Finding)]
    public void SeekerStepToShow_ShowsTheAskedStepWithinRange(int? requested, SeekerStep expected) =>
        FullSeeker().StepToShow(requested).Should().Be(expected);

    [Fact]
    public void SeekerStepToShow_NeverSkipsAnUnansweredQuestion()
    {
        var answers = FullSeeker();
        answers.CityId = null;

        answers.StepToShow((int)SeekerStep.Budget).Should().Be(SeekerStep.City);
    }

    [Fact]
    public void PickCategory_DifferentTrade_ClearsTheJob()
    {
        var answers = FullSeeker();

        answers.PickCategory("plumbing");

        answers.ServiceId.Should().BeNull();
    }

    [Fact]
    public void PickCategory_SameTrade_KeepsTheJob()
    {
        var answers = FullSeeker();

        answers.PickCategory("aircon");

        answers.ServiceId.Should().Be("aircon-cleaning");
    }

    [Theory]
    [InlineData("angono", "rizal")]
    [InlineData("makati", null)] // a town we don't serve has no area
    public void PickTown_SetsTheTownAndItsArea(string town, string? area)
    {
        var answers = new SeekerAnswers();

        answers.PickTown(town);

        answers.CityId.Should().Be(town);
        answers.AreaId.Should().Be(area);
    }

    [Fact]
    public void NextPath_DuringTheInterview_IsTheNextQuestion() =>
        new SeekerAnswers().NextPath(SeekerStep.City, finished: false).Should().Be("/start/seeker/4");

    [Fact]
    public void NextPath_ChangingAnAnswerLater_GoesBackToResults() =>
        FullSeeker().NextPath(SeekerStep.Category, finished: true).Should().Be("/seeker/search");

    [Fact]
    public void NextPath_ChangingTheTrade_AsksTheJobAgainBeforeResults()
    {
        var answers = FullSeeker();
        answers.PickCategory("plumbing");

        answers.NextPath(SeekerStep.Category, finished: true).Should().Be("/start/seeker/2");
    }

    [Theory]
    [InlineData(SeekerStep.When, false, "/start/seeker/3")]
    [InlineData(SeekerStep.Category, false, OnboardingSteps.RolePath)]
    [InlineData(SeekerStep.When, true, "/seeker/search")]
    public void SeekerBackPath_GoesToThePreviousQuestion_OrBackToResults(SeekerStep step, bool finished, string expected) =>
        OnboardingSteps.BackPath(step, finished).Should().Be(expected);

    // --- Where the app opens --------------------------------------------------

    [Fact]
    public void LaunchPath_NoRoleYet_IsTheRoleFork() =>
        OnboardingSteps.LaunchPath(new OnboardingState()).Should().Be(OnboardingSteps.RolePath);

    [Fact]
    public void LaunchPath_MidInterview_ResumesWhereTheyLeft() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, ResumePath = "/start/pro/4" })
            .Should().Be("/start/pro/4");

    [Theory]
    [InlineData(UserRole.Seeker, "/start/seeker")]
    [InlineData(UserRole.Professional, "/start/pro")]
    public void LaunchPath_RolePickedNothingAnswered_StartsThatInterview(UserRole role, string expected) =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = role }).Should().Be(expected);

    [Fact]
    public void LaunchPath_FinishedSeeker_OpensSeekerHome() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, CompletedAt = DateTimeOffset.Now, Seeker = FullSeeker() })
            .Should().Be("/seeker");

    [Fact]
    public void LaunchPath_FinishedSeekerWhoseTownIsNoLongerServed_AsksTheTownAgain() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, CompletedAt = DateTimeOffset.Now })
            .Should().Be(OnboardingSteps.SeekerTownPath);

    [Fact]
    public void LaunchPath_FinishedWorker_OpensProHome() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, CompletedAt = DateTimeOffset.Now, Professional = FullyAnsweredDraft() })
            .Should().Be("/professional");

    [Fact]
    public void LaunchPath_FinishedWorkerMissingAnAnswer_ReopensTheInterview() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, CompletedAt = DateTimeOffset.Now })
            .Should().Be(OnboardingSteps.ProFirstUnansweredPath);
}
