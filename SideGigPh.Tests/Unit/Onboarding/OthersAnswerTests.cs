using SideGigPh.Contracts;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Tests.Unit.Onboarding;

/// <summary>
/// "Others" after the trades: someone whose work we don't list must still finish the interview and land in the app.
/// It used to thank them and send them back to the trades, a loop with no way out. Now it is saved as their trade and
/// job (the job question is skipped), kept across launches and shown as what they typed.
/// </summary>
public class OthersAnswerTests
{
    private const string Other = ReferenceData.OtherTradeId;

    // --- Seeker -------------------------------------------------------------

    [Fact]
    public void SeekerPickOthers_SavesItAsTradeAndJob_SoTheTownIsNext()
    {
        var answers = new SeekerAnswers();

        answers.PickCategory(Other);

        answers.ServiceId.Should().Be(Other);
        answers.FirstUnanswered().Should().Be(SeekerStep.City);
        answers.NextPath(SeekerStep.Category, finished: false).Should().Be(OnboardingSteps.PathOf(SeekerStep.City));
    }

    [Fact]
    public void SeekerOthers_BackFromTheTown_GoesToTheTrades_NotAnEmptyJobQuestion()
    {
        var answers = new SeekerAnswers();
        answers.PickCategory(Other);

        answers.BackPath(SeekerStep.City, finished: false).Should().Be(OnboardingSteps.PathOf(SeekerStep.Category));
        answers.StepToShow((int)SeekerStep.Service).Should().Be(SeekerStep.Category);
    }

    [Fact]
    public void SeekerOthers_ChangingToARealTrade_ClearsTheOthersJobAndText()
    {
        var answers = new SeekerAnswers();
        answers.PickCategory(Other);
        answers.OtherJobText = "Plaster ng kisame";

        answers.PickCategory("plumbing");

        answers.ServiceId.Should().BeNull();
        answers.OtherJobText.Should().BeNull();
    }

    [Fact]
    public void SeekerOthers_FinishedInterview_OpensSeekerHome()
    {
        var state = new OnboardingState
        {
            Role = UserRole.Seeker,
            CompletedAt = DateTimeOffset.Now,
            Seeker = { CityId = "taytay", AreaId = "rizal", UrgencyId = ReferenceData.UrgencyToday, BudgetId = ReferenceData.Unsure },
        };
        state.Seeker.PickCategory(Other);
        state.Seeker.OtherJobText = "Plaster ng kisame";

        OnboardingSteps.LaunchPath(state.FitToCatalog()).Should().Be(SeekerRoutes.Home);
        state.Seeker.OtherJobText.Should().Be("Plaster ng kisame", "the text is what search matches on");
    }

    // --- Skilled Worker ------------------------------------------------------

    [Fact]
    public void ProPickOthers_SavesItAsTradeAndJob_SoTheRateIsNext()
    {
        var draft = new ProfessionalDraft();

        draft.PickTrade(Other);

        draft.ServiceIds.Should().Equal(Other);
        draft.FirstUnanswered().Should().Be(ProStep.Rate);
        draft.NextPath(ProStep.Skills).Should().Be(OnboardingSteps.PathOf(ProStep.Rate));
        draft.BackPath(ProStep.Rate, finished: false).Should().Be(OnboardingSteps.PathOf(ProStep.Skills));
        draft.StepToShow((int)ProStep.Services).Should().Be(ProStep.Skills);
    }

    [Fact]
    public void ProOthers_CanMakeACompleteListing_AndKeepsItOnLoad()
    {
        var state = new OnboardingState
        {
            Role = UserRole.Professional,
            CompletedAt = DateTimeOffset.Now,
            Professional = { CityIds = ["taytay"], AreaId = "rizal", ExperienceId = "1-3", RateTierId = RateRange.Format(800, 1500) },
        };
        state.Professional.PickTrade(Other);
        state.Professional.OtherJobText = "Plaster ng kisame";

        state.FitToCatalog();

        state.Professional.IsListingComplete.Should().BeTrue();
        state.Professional.RateTierId.Should().Be("rate-800-1500", "a typed rate range is not an old tier");
        OnboardingSteps.LaunchPath(state).Should().Be(OnboardingSteps.PathOf(ProStep.Photo), "on to the selfie, not back to the trades");
    }

    [Fact]
    public void OtherWork_ShowsWhatTheWorkerTyped_InPlaceOfOthers()
    {
        var text = new AppText();
        var listing = new ProfessionalListing
        {
            Id = "w",
            DisplayName = "Kuya Ben",
            CategoryIds = [Other],
            ServiceIds = [Other],
            CityIds = ["taytay"],
            MobileNumber = string.Empty,
            OtherWork = "Plaster ng kisame",
        };

        text.Trades(listing).Should().Be("Plaster ng kisame");
        text.JobLabels(listing).Should().Equal("Plaster ng kisame");
        listing.TradeOptions().Should().ContainSingle().Which.Id.Should().Be(Other);
    }

    [Fact]
    public void OtherWork_IsSentOnPublish_OnlyWithAnOthersJob()
    {
        var withOthers = new ProfessionalDraft { CategoryId = "plumbing", ServiceIds = ["plumbing-other"], OtherJobText = "Poso negro" };
        var without = new ProfessionalDraft { CategoryId = "plumbing", ServiceIds = ["plumbing-clog"], OtherJobText = "Poso negro" };

        SideGigPh.Maui.Services.Api.ApiMapping.ToPublishRequest(withOthers.ToListing()).OtherWork.Should().Be("Poso negro");
        SideGigPh.Maui.Services.Api.ApiMapping.ToPublishRequest(without.ToListing()).OtherWork.Should().BeNull();
    }
}
