using SideGigPh.Maui.Components.Seeker;
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
    };

    // --- Skilled Worker interview -----------------------------------------

    public static TheoryData<string, ProStep> ProMissingAnswers => new()
    {
        { "trade", ProStep.Skills },
        { "jobs", ProStep.Services },
        { "towns", ProStep.Towns },
        { "experience", ProStep.Experience },
        { "rate", ProStep.Rate },
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
    public void ToggleAllJobs_TicksEveryListedJobButOthers_ThenRemovesAll()
    {
        var draft = new ProfessionalDraft { CategoryId = "it", ServiceIds = ["it-software"] };
        var listed = ReferenceData.ServicesOf("it").Select(job => job.Id).Where(id => !ReferenceData.IsOtherJob(id)).ToList();

        draft.ToggleAllJobs();
        draft.ServiceIds.Should().BeEquivalentTo(listed, "\"Others\" asks them to type the work, so it isn't ticked for them");
        draft.HasAllJobs().Should().BeTrue();

        draft.ToggleAllJobs();
        draft.ServiceIds.Should().BeEmpty();
        draft.HasAllJobs().Should().BeFalse();
    }

    [Fact]
    public void ToggleAllJobs_KeepsATickedOthers_AndRemoveAllForgetsItsText()
    {
        var draft = new ProfessionalDraft { CategoryId = "it", ServiceIds = ["it-other"], OtherJobText = "Drone repair" };

        draft.ToggleAllJobs();
        draft.ServiceIds.Should().Contain("it-other");
        draft.OtherJobText.Should().Be("Drone repair");

        draft.ToggleAllJobs();
        draft.ServiceIds.Should().BeEmpty();
        draft.OtherJobText.Should().BeNull();
    }

    [Fact]
    public void HasAllJobs_WithoutATrade_IsFalse() =>
        new ProfessionalDraft().HasAllJobs().Should().BeFalse("there is nothing to select, so the button reads \"Select all\"");

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
        FindBy = ReferenceData.FindBySearch,
    };

    [Theory]
    [InlineData("category", SeekerStep.Category)]
    [InlineData("service", SeekerStep.Service)]
    [InlineData("city", SeekerStep.City)]
    [InlineData("when", SeekerStep.When)]
    [InlineData("budget", SeekerStep.Budget)]
    [InlineData("findBy", SeekerStep.FindBy)]
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
            case "findBy": answers.FindBy = null; break;
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

    [Fact]
    public void NextPath_AfterTheBudget_AsksSearchOrPost() =>
        FullSeeker().NextPath(SeekerStep.Budget, finished: false).Should().Be("/start/seeker/6");

    [Fact]
    public void NextPath_ChangingAnAnswer_NeverAsksSearchOrPost_ForSomeoneWhoFinishedBeforeIt()
    {
        var answers = FullSeeker();
        answers.FindBy = null;

        answers.NextPath(SeekerStep.Budget, finished: true).Should().Be("/seeker/search");
    }

    [Theory]
    [InlineData(ReferenceData.FindBySearch, "/seeker/search")]
    [InlineData(ReferenceData.FindByPost, "/posts/new")]
    [InlineData(null, "/seeker/search")]
    public void FinishPath_OpensTheResults_OrThePostForm(string? findBy, string expected)
    {
        var answers = FullSeeker();
        answers.FindBy = findBy;

        answers.FinishPath().Should().Be(expected);
    }

    [Fact]
    public void LaunchPath_SeekerWhoFinishedBeforeSearchOrPostExisted_OpensSeekerHome()
    {
        var seeker = FullSeeker();
        seeker.FindBy = null;

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, CompletedAt = DateTimeOffset.Now, Seeker = seeker })
            .Should().Be(SeekerRoutes.Home);
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
    public void LaunchPath_FinishedSeekerWhoseTownIsNoLongerServed_AsksTheTownAgain()
    {
        var seeker = FullSeeker();
        seeker.CityId = null;

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, CompletedAt = DateTimeOffset.Now, Seeker = seeker })
            .Should().Be(OnboardingSteps.SeekerTownPath);
    }

    [Fact]
    public void LaunchPath_FinishedSeekerWhoTappedATradeOnHome_OpensSeekerHome()
    {
        var seeker = FullSeeker();
        seeker.ServiceId = null; // a trade tapped on Home: all its jobs, not a lost answer

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, CompletedAt = DateTimeOffset.Now, Seeker = seeker })
            .Should().Be(SeekerRoutes.Home);
    }

    [Fact]
    public void LaunchPath_FinishedSeekerWithNoJobWhoseTownIsNoLongerServed_AsksTheTownAgain()
    {
        var seeker = FullSeeker();
        seeker.ServiceId = null;
        seeker.CityId = null;

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, CompletedAt = DateTimeOffset.Now, Seeker = seeker })
            .Should().Be(OnboardingSteps.SeekerTownPath);
    }

    [Fact]
    public void LaunchPath_FinishedSeekerWhoseTradeWasRetired_AsksTheTradeAgain()
    {
        var seeker = FullSeeker();
        seeker.CategoryId = null;
        seeker.ServiceId = null;

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, CompletedAt = DateTimeOffset.Now, Seeker = seeker })
            .Should().Be(OnboardingSteps.PathOf(SeekerStep.Category));
    }

    [Fact]
    public void LaunchPath_LiveWorker_OpensProHome()
    {
        var draft = WithProof(FullyAnsweredDraft());
        draft.PublishedAt = DateTimeOffset.Now;

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, CompletedAt = DateTimeOffset.Now, Professional = draft })
            .Should().Be("/professional");
    }

    [Fact]
    public void LaunchPath_FinishedWorkerNotLive_ResumesTheSetup() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, CompletedAt = DateTimeOffset.Now, Professional = FullyAnsweredDraft() })
            .Should().Be("/start/pro/setup/1");

    [Fact]
    public void LaunchPath_FinishedWorkerWithProofNotLive_OpensPublish() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, CompletedAt = DateTimeOffset.Now, Professional = WithProof(FullyAnsweredDraft()) })
            .Should().Be("/professional/publish");

    // --- Skilled Worker setup (proof, then Publish) -------------------------

    private static ProfessionalDraft WithProof(ProfessionalDraft draft)
    {
        draft.ProfilePhotoId = "me";
        draft.WorkPhotoIds.AddRange(["w1", "w2"]);
        return draft;
    }

    [Fact]
    public void Setup_OpensAtTheFirstMissingProof_InOrder()
    {
        var draft = FullyAnsweredDraft();
        draft.FirstUnfinished().Should().Be(ProSetupStep.Photo);

        draft.ProfilePhotoId = "me";
        draft.FirstUnfinished().Should().Be(ProSetupStep.WorkPhotos);

        draft.WorkPhotoIds.Add("w1");
        draft.FirstUnfinished().Should().Be(ProSetupStep.WorkPhotos, "one work photo isn't enough");

        draft.WorkPhotoIds.Add("w2");
        draft.FirstUnfinished().Should().BeNull("own photo and 2 work photos are the whole setup");
        draft.AfterInterviewPath().Should().Be("/professional/publish");
    }

    [Theory]
    [InlineData(null, ProSetupStep.Photo)]
    [InlineData(3, ProSetupStep.Photo)]
    [InlineData(99, ProSetupStep.Photo)]
    [InlineData(-1, ProSetupStep.Photo)]
    public void SetupStep_NeverSkipsAMissingProof(int? requested, ProSetupStep expected) =>
        FullyAnsweredDraft().SetupStepToShow(requested).Should().Be(expected);

    [Theory]
    [InlineData(1, ProSetupStep.Photo)]
    [InlineData(2, ProSetupStep.WorkPhotos)]
    [InlineData(3, ProSetupStep.WorkPhotos)] // the old references step's URL (a saved back link) lands on the last step
    [InlineData(99, ProSetupStep.WorkPhotos)]
    public void SetupStep_AllDone_AnyStepCanBeRevisited(int requested, ProSetupStep expected) =>
        WithProof(FullyAnsweredDraft()).SetupStepToShow(requested).Should().Be(expected);

    [Fact]
    public void Setup_NextWalksTheStepsThenPublish()
    {
        OnboardingSteps.NextPath(ProSetupStep.Photo).Should().Be("/start/pro/setup/2");
        OnboardingSteps.NextPath(ProSetupStep.WorkPhotos).Should().Be("/professional/publish");
    }

    [Fact]
    public void Setup_BackFromTheFirstStep_IsTheInterviewPreview()
    {
        OnboardingSteps.BackPath(ProSetupStep.Photo).Should().Be(OnboardingSteps.PathOf(ProStep.Preview));
        OnboardingSteps.BackPath(ProSetupStep.WorkPhotos).Should().Be("/start/pro/setup/1");
    }

    [Fact]
    public void AfterInterview_GoesToSetup_ThenPublish_ThenHomeOnceLive()
    {
        var draft = FullyAnsweredDraft();
        draft.AfterInterviewPath().Should().Be("/start/pro/setup/1");

        WithProof(draft);
        draft.AfterInterviewPath().Should().Be("/professional/publish");

        draft.PublishedAt = DateTimeOffset.Now;
        draft.AfterInterviewPath().Should().Be("/professional", "editing the interview of a live listing goes home");
    }

    [Fact]
    public void LaunchPath_FinishedWorkerMissingAnAnswer_ReopensTheInterview() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, CompletedAt = DateTimeOffset.Now })
            .Should().Be(OnboardingSteps.ProFirstUnansweredPath);

    [Fact]
    public void FirstPath_EachRoleStartsItsOwnInterview()
    {
        OnboardingSteps.FirstPath(UserRole.Seeker).Should().Be("/start/seeker/1");
        OnboardingSteps.FirstPath(UserRole.Professional).Should().Be("/start/pro/1");
    }

    [Theory]
    [InlineData(UserRole.Professional, 0, true)]
    [InlineData(UserRole.Professional, 200, true)]
    [InlineData(UserRole.Professional, null, false)]
    [InlineData(UserRole.Seeker, 0, false)]
    public void AsksNumberAfterRole_OnlyWorkersWhileACodeCanStillBeSentToday(UserRole role, int? waitSeconds, bool asks) =>
        OnboardingSteps.AsksNumberAfterRole(role, waitSeconds is { } s ? TimeSpan.FromSeconds(s) : null)
            .Should().Be(asks);

    [Fact]
    public void ProgressBar_GoesUpFromTheFirstQuestionToTheLastSetupStep_AndNeverDropsBack()
    {
        var bar = Enum.GetValues<ProStep>().Select(OnboardingSteps.ProgressOf)
            .Concat(Enum.GetValues<ProSetupStep>().Select(OnboardingSteps.ProgressOf))
            .ToList();

        bar.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        bar[0].Should().BeGreaterThan(0);  // endowed progress
        bar[^1].Should().Be(1);
        OnboardingSteps.ProgressOf(ProSetupStep.Photo).Should().BeGreaterThan(OnboardingSteps.ProgressOf(ProStep.Preview));
    }
}
