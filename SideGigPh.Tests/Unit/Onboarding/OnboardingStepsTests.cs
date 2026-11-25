using SideGigPh.Maui.Components.Professional;
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
        RatePesos = 1000,
    };

    private static ProfessionalDraft WithProof(ProfessionalDraft draft)
    {
        draft.ProfilePhotoId = "me";
        return draft;
    }

    // --- Skilled Worker interview -----------------------------------------

    public static TheoryData<string, ProStep> ProMissingAnswers => new()
    {
        { "trade", ProStep.Skills },
        { "jobs", ProStep.Services },
        { "rate", ProStep.Rate },
        { "towns", ProStep.Towns },
        { "experience", ProStep.Experience },
        { "photo", ProStep.Photo },
    };

    [Fact]
    public void ProSteps_AreInTheirOrder() =>
        Enum.GetValues<ProStep>().Should().Equal(
            ProStep.Skills, ProStep.Services, ProStep.Rate, ProStep.Towns, ProStep.Experience,
            ProStep.Photo, ProStep.Number, ProStep.Preview);

    [Theory]
    [MemberData(nameof(ProMissingAnswers))]
    public void ProFirstUnanswered_IsTheFirstMissingQuestion(string missing, ProStep expected)
    {
        var draft = WithProof(FullyAnsweredDraft());
        switch (missing)
        {
            case "trade": draft.CategoryId = null; break;
            case "jobs": draft.ServiceIds.Clear(); break;
            case "towns": draft.CityIds.Clear(); break;
            case "experience": draft.ExperienceId = null; break;
            case "rate": draft.RatePesos = null; break;
            case "photo": draft.ProfilePhotoId = null; break;
        }

        draft.FirstUnanswered().Should().Be(expected);
    }

    [Fact]
    public void ProFirstUnanswered_AllAnsweredWithASelfie_IsThePreview() =>
        WithProof(FullyAnsweredDraft()).FirstUnanswered().Should().Be(ProStep.Preview);

    [Fact]
    public void ProFirstUnanswered_NoSelfie_IsThePhotoQuestion() =>
        FullyAnsweredDraft().FirstUnanswered().Should().Be(ProStep.Photo, "the preview shows the real card, so the photo comes first");

    [Theory]
    [InlineData(null, ProStep.Skills)]
    [InlineData(-5, ProStep.Skills)]   // nonsense in the URL
    [InlineData(3, ProStep.Rate)]
    [InlineData(4, ProStep.Towns)]
    [InlineData(99, ProStep.Preview)]  // past the end
    public void ProStepToShow_FullyAnswered_ShowsTheAskedStepWithinRange(int? requested, ProStep expected) =>
        WithProof(FullyAnsweredDraft()).StepToShow(requested).Should().Be(expected);

    [Fact]
    public void ProStepToShow_NeverSkipsAnUnansweredQuestion()
    {
        // A stale resume path asks for the experience, but the towns were never picked.
        var draft = WithProof(FullyAnsweredDraft());
        draft.CityIds.Clear();

        draft.StepToShow((int)ProStep.Experience).Should().Be(ProStep.Towns);
    }

    [Fact]
    public void CorrectedPath_AStepShownForAnotherUrl_GetsItsOwnUrl_SoNextNeverStaysPut()
    {
        // A worker stopped on the jobs, then their trade was retired: the jobs URL shows the trade question. Without
        // its own URL, Next after a new trade would lead to the jobs URL already open and nothing would move.
        var draft = new ProfessionalDraft();
        var shown = draft.StepToShow((int)ProStep.Services);

        shown.Should().Be(ProStep.Skills);
        OnboardingSteps.CorrectedPath((int)ProStep.Services, shown).Should().Be(OnboardingSteps.PathOf(ProStep.Skills));
        draft.PickTrade("aircon");
        draft.NextPath(shown).Should().NotBe(OnboardingSteps.PathOf(ProStep.Skills));
    }

    [Fact]
    public void CorrectedPath_TheUrlsOwnStep_StaysPut()
    {
        OnboardingSteps.CorrectedPath((int)ProStep.Towns, ProStep.Towns).Should().BeNull();
        OnboardingSteps.CorrectedPath((int)SeekerStep.City, SeekerStep.City).Should().BeNull();
        OnboardingSteps.CorrectedPath(null, ProStep.Skills).Should().Be(OnboardingSteps.PathOf(ProStep.Skills));
    }

    [Fact]
    public void SeekerCorrectedPath_AJobUrlWithNoTrade_OpensTheTradeQuestionAtItsOwnUrl()
    {
        var answers = new SeekerAnswers();
        var shown = answers.StepToShow((int)SeekerStep.Service);

        shown.Should().Be(SeekerStep.Category);
        OnboardingSteps.CorrectedPath((int)SeekerStep.Service, shown).Should().Be(OnboardingSteps.PathOf(SeekerStep.Category));
    }

    [Fact]
    public void ProStepToShow_NoSelfie_NeverOpensTheNumberOrPreview()
    {
        var draft = FullyAnsweredDraft();

        draft.StepToShow((int)ProStep.Number, asksNumber: true).Should().Be(ProStep.Photo);
        draft.StepToShow((int)ProStep.Preview).Should().Be(ProStep.Photo);
    }

    [Theory]
    [InlineData(true, ProStep.Number)]
    [InlineData(false, ProStep.Preview)]
    public void ProNumberQuestion_ShowsOnlyWhenAsked(bool asksNumber, ProStep expected) =>
        WithProof(FullyAnsweredDraft()).StepToShow((int)ProStep.Number, asksNumber).Should().Be(expected);

    [Theory]
    [InlineData(true, "/start/pro/7")]
    [InlineData(false, "/start/pro/8")]
    public void ProNextFromTheSelfie_IsTheNumber_OrThePreviewWhenItIsNotAsked(bool asksNumber, string expected) =>
        WithProof(FullyAnsweredDraft()).NextPath(ProStep.Photo, asksNumber).Should().Be(expected);

    [Theory]
    [InlineData(true, "/start/pro/7")]
    [InlineData(false, "/start/pro/6")]
    public void ProBackFromThePreview_IsTheNumber_OrTheSelfieWhenItIsNotAsked(bool asksNumber, string expected) =>
        WithProof(FullyAnsweredDraft()).BackPath(ProStep.Preview, finished: false, asksNumber).Should().Be(expected);

    [Fact]
    public void ProNextFromTheNumber_IsThePreview() =>
        WithProof(FullyAnsweredDraft()).NextPath(ProStep.Number, asksNumber: true).Should().Be("/start/pro/8");

    [Theory]
    [InlineData(true, false, 0, true)]      // a Google account, a code can be sent now
    [InlineData(true, false, 200, true)]    // a code can be sent after a wait
    [InlineData(true, true, 0, false)]      // a phone sign-in already has a verified number
    [InlineData(true, false, null, false)]  // today's codes are used up: Publish asks later
    [InlineData(false, false, 0, false)]    // no sign-in provider
    public void AsksNumber_OnlyAnAccountWithoutANumber_WhileACodeCanStillBeSentToday(
        bool hasSignIn, bool hasNumber, int? waitSeconds, bool asks) =>
        OnboardingSteps.AsksNumber(hasSignIn, hasNumber, waitSeconds is { } s ? TimeSpan.FromSeconds(s) : null)
            .Should().Be(asks);

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
        var draft = new ProfessionalDraft { CategoryId = "driver", ServiceIds = ["driver-family"] };
        var listed = ReferenceData.ServicesOf("driver").Select(job => job.Id).Where(id => !ReferenceData.IsOtherJob(id)).ToList();

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
        var draft = new ProfessionalDraft { CategoryId = "driver", ServiceIds = ["driver-other"], OtherJobText = "Wedding car" };

        draft.ToggleAllJobs();
        draft.ServiceIds.Should().Contain("driver-other");
        draft.OtherJobText.Should().Be("Wedding car");

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
    [InlineData(ProStep.Towns, false, "/start/pro/3")]
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
    public void NextPath_ChangingAnAnswerLater_ReturnsWhereTheyFinished()
    {
        // Back from the post form lands on the questions: tapping "Post what I need" again must reopen the form.
        var answers = FullSeeker();
        answers.FindBy = ReferenceData.FindByPost;
        answers.NextPath(SeekerStep.FindBy, finished: true).Should().Be("/posts/new");
        answers.NextPath(SeekerStep.Budget, finished: true).Should().Be("/posts/new");

        answers.FindBy = ReferenceData.FindBySearch;
        answers.NextPath(SeekerStep.FindBy, finished: true).Should().Be("/seeker/search");
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

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, SeekerFinishedAt = DateTimeOffset.Now, Seeker = seeker })
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
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, SeekerFinishedAt = DateTimeOffset.Now, Seeker = FullSeeker() })
            .Should().Be("/seeker");

    [Fact]
    public void LaunchPath_FinishedSeekerWhoseTownIsNoLongerServed_AsksTheTownAgain()
    {
        var seeker = FullSeeker();
        seeker.CityId = null;

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, SeekerFinishedAt = DateTimeOffset.Now, Seeker = seeker })
            .Should().Be(OnboardingSteps.SeekerTownPath);
    }

    [Fact]
    public void LaunchPath_FinishedSeekerWhoTappedATradeOnHome_OpensSeekerHome()
    {
        var seeker = FullSeeker();
        seeker.ServiceId = null; // a trade tapped on Home: all its jobs, not a lost answer

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, SeekerFinishedAt = DateTimeOffset.Now, Seeker = seeker })
            .Should().Be(SeekerRoutes.Home);
    }

    [Fact]
    public void LaunchPath_FinishedSeekerWithNoJobWhoseTownIsNoLongerServed_AsksTheTownAgain()
    {
        var seeker = FullSeeker();
        seeker.ServiceId = null;
        seeker.CityId = null;

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, SeekerFinishedAt = DateTimeOffset.Now, Seeker = seeker })
            .Should().Be(OnboardingSteps.SeekerTownPath);
    }

    [Fact]
    public void LaunchPath_FinishedSeekerWithNoTrade_OpensSeekerHome()
    {
        // A retired trade, or a household restored after a reinstall (a town, no last search): Home searches from its
        // pill and trade tiles, so no question is asked again.
        var seeker = FullSeeker();
        seeker.CategoryId = null;
        seeker.ServiceId = null;

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Seeker, SeekerFinishedAt = DateTimeOffset.Now, Seeker = seeker })
            .Should().Be(SeekerRoutes.Home);
    }

    [Fact]
    public void LaunchPath_LiveWorker_OpensProHome()
    {
        var draft = WithProof(FullyAnsweredDraft());
        draft.PublishedAt = DateTimeOffset.Now;

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, ProFinishedAt = DateTimeOffset.Now, Professional = draft })
            .Should().Be("/professional");
    }

    [Fact]
    public void NextPath_FinishedSeekerWithNoTrade_AnsweringTheTownAgain_GoesHome()
    {
        // A restored household whose town is no longer served is asked the town, then lands on Home: not the trade,
        // job, when and budget questions again.
        var answers = new SeekerAnswers { CityId = "taytay" };

        answers.NextPath(SeekerStep.City, finished: true).Should().Be(SeekerRoutes.Home);
    }

    [Fact]
    public void LaunchPath_LiveWorkerMissingAnAnswer_StillOpensProHome()
    {
        // A listing restored after a reinstall can lack an answer the server doesn't keep (experience): Pro home asks for
        // it (ProWorkspace.HasUnfinishedEdit), launch never sends a live worker back into the interview.
        var draft = WithProof(FullyAnsweredDraft());
        draft.ExperienceId = null;
        draft.PublishedAt = DateTimeOffset.Now;

        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, ProFinishedAt = DateTimeOffset.Now, Professional = draft })
            .Should().Be(ProRoutes.Home);
    }

    [Fact]
    public void LaunchPath_FinishedWorkerNotLive_ResumesTheSetup() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, ProFinishedAt = DateTimeOffset.Now, Professional = FullyAnsweredDraft() })
            .Should().Be("/start/pro/6");

    [Fact]
    public void LaunchPath_FinishedWorkerWithProofNotLive_OpensPublish() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, ProFinishedAt = DateTimeOffset.Now, Professional = WithProof(FullyAnsweredDraft()) })
            .Should().Be("/professional/publish");

    [Fact]
    public void AfterInterview_GoesToTheSelfie_ThenPublish_ThenHomeOnceLive()
    {
        var draft = FullyAnsweredDraft();
        draft.AfterInterviewPath().Should().Be("/start/pro/6");

        WithProof(draft);
        draft.AfterInterviewPath().Should().Be("/professional/publish");

        draft.PublishedAt = DateTimeOffset.Now;
        draft.AfterInterviewPath().Should().Be("/professional", "editing the interview of a live listing goes home");
    }

    [Fact]
    public void LaunchPath_FinishedWorkerMissingAnAnswer_ReopensTheInterview() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, ProFinishedAt = DateTimeOffset.Now })
            .Should().Be(OnboardingSteps.ProFirstUnansweredPath);

    [Fact]
    public void FirstPath_EachRoleStartsItsOwnInterview()
    {
        OnboardingSteps.FirstPath(UserRole.Seeker).Should().Be("/start/seeker/1");
        OnboardingSteps.FirstPath(UserRole.Professional).Should().Be("/start/pro/1");
    }

    [Fact]
    public void ProgressBar_GoesUpFromTheFirstQuestionToTheReadyStep_AndNeverDropsBack()
    {
        var bar = Enum.GetValues<ProStep>().Select(OnboardingSteps.ProgressOf)
            .Concat(Enum.GetValues<PublishStep>().Select(OnboardingSteps.ProgressOf))
            .ToList();

        bar.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        bar[0].Should().BeGreaterThan(0);  // endowed progress
        bar[^1].Should().Be(1, "the Ready step that publishes is the end of the bar");
        OnboardingSteps.ProgressOf(PublishStep.Checklist).Should().BeGreaterThan(OnboardingSteps.ProgressOf(ProStep.Preview));
    }

    // --- Going live: the promises, then Ready ----------------------------------

    [Fact]
    public void Publish_InterviewLeadsToThePromises_ThenReady()
    {
        WithProof(FullyAnsweredDraft()).AfterInterviewPath().Should().Be(OnboardingSteps.PathOf(PublishStep.Checklist));
        OnboardingSteps.PathOf(PublishStep.Checklist).Should().Be("/professional/publish", "every Publish link opens the promises");
        OnboardingSteps.PathOf(PublishStep.Ready).Should().Be("/professional/publish/ready");
    }

    [Fact]
    public void Publish_BackWalksTheSteps()
    {
        OnboardingSteps.BackPath(PublishStep.Ready).Should().Be("/professional/publish");
        OnboardingSteps.BackPath(PublishStep.Checklist).Should().Be(OnboardingSteps.PathOf(ProStep.Preview));
    }

    [Theory]
    [InlineData(PublishStep.Checklist, false)]
    [InlineData(PublishStep.Checklist, true)]
    [InlineData(PublishStep.Ready, true)]
    public void Publish_ReadyWorker_SeesTheStep(PublishStep step, bool promisesTicked) =>
        WithProof(FullyAnsweredDraft()).PublishRedirect(step, promisesTicked).Should().BeNull();

    [Fact]
    public void Publish_ReadyStepWithoutEveryPromise_GoesBackToTheChecklist() =>
        WithProof(FullyAnsweredDraft()).PublishRedirect(PublishStep.Ready, promisesTicked: false)
            .Should().Be("/professional/publish", "a link or a killed app never skips the promises");

    [Theory]
    [InlineData(PublishStep.Checklist)]
    [InlineData(PublishStep.Ready)]
    public void Publish_LiveListing_GoesHome_NeverThroughThePromisesAgain(PublishStep step)
    {
        var draft = WithProof(FullyAnsweredDraft());
        draft.PublishedAt = DateTimeOffset.Now;

        draft.PublishRedirect(step, promisesTicked: false).Should().Be("/professional");
    }

    [Theory]
    [InlineData(PublishStep.Checklist)]
    [InlineData(PublishStep.Ready)]
    public void Publish_MissingAnswerOrProof_ReopensThatFirst(PublishStep step)
    {
        var noAnswer = WithProof(FullyAnsweredDraft());
        noAnswer.CityIds.Clear();
        noAnswer.PublishRedirect(step, promisesTicked: true).Should().Be(OnboardingSteps.ProFirstUnansweredPath);

        FullyAnsweredDraft().PublishRedirect(step, promisesTicked: true).Should().Be(OnboardingSteps.PathOf(ProStep.Photo));
    }

    [Fact]
    public void LaunchPath_WorkerKilledOnTheReadyStep_ResumesAtThePromises() =>
        OnboardingSteps.LaunchPath(new OnboardingState { Role = UserRole.Professional, ProFinishedAt = DateTimeOffset.Now, Professional = WithProof(FullyAnsweredDraft()) })
            .Should().Be(OnboardingSteps.PathOf(PublishStep.Checklist), "the ticks live only in memory, so they are asked again");
}
