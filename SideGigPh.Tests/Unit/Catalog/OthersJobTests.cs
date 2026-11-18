using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Catalog;

/// <summary>
/// Every trade ends with "Others / Wala sa nabanggit". Picking it lets the person type the job (kept for analytics);
/// the job itself stays "Others". The typed text must follow the answer it belongs to, word the Seeker's first
/// message, and go when the answer changes. A trade we stop offering is dropped from saved answers.
/// </summary>
public class OthersJobTests
{
    [Fact]
    public void Seeker_PickingAnotherJob_ForgetsTheOthersText()
    {
        var answers = new SeekerAnswers { CategoryId = "welder" };
        answers.PickService("welder-other");
        answers.OtherJobText = "pintura ng gate";

        answers.PickService("welder-other");
        answers.OtherJobText.Should().Be("pintura ng gate", "tapping Others again keeps what they typed");

        answers.PickService("welder-gate");
        answers.OtherJobText.Should().BeNull();
    }

    [Fact]
    public void Seeker_PickingAnotherTrade_ForgetsTheJobAndText()
    {
        var answers = new SeekerAnswers { CategoryId = "welder", ServiceId = "welder-other", OtherJobText = "pintura" };

        answers.PickCategory("aircon");

        answers.ServiceId.Should().BeNull();
        answers.OtherJobText.Should().BeNull();
    }

    [Fact]
    public void Worker_UntickingOthers_ForgetsTheText_ChangingTrade_Too()
    {
        var draft = new ProfessionalDraft { CategoryId = "welder" };
        draft.ToggleService("welder-gate");
        draft.ToggleService("welder-other");
        draft.OtherJobText = "stainless";

        draft.ToggleService("welder-gate");
        draft.OtherJobText.Should().Be("stainless", "Others is still ticked");

        draft.ToggleService("welder-other");
        draft.OtherJobText.Should().BeNull();

        draft.ToggleService("welder-other");
        draft.OtherJobText = "stainless";
        draft.PickTrade("aircon");
        draft.OtherJobText.Should().BeNull();
        draft.ServiceIds.Should().BeEmpty();
    }

    [Fact]
    public async Task FirstMessage_UnderOthers_SaysWhatTheSeekerTyped()
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.ServiceId = "aircon-other";
        answers.OtherJobText = "  Pa-check ng thermostat ";
        var app = await FakeApp.SeekerAsync(answers);

        app.Seeker.DefaultMessage(TestData.Listing("Ana", services: ["aircon-cleaning", "aircon-other"]))
            .Should().Contain("I need: Pa-check ng thermostat, in Taytay");
    }

    [Fact]
    public async Task FirstMessage_UnderOthersWithNothingTyped_NamesTheTrade_NotOthers()
    {
        var answers = TestData.AirconCleaningInTaytay();
        answers.ServiceId = "aircon-other";
        var app = await FakeApp.SeekerAsync(answers);

        app.Seeker.DefaultMessage(TestData.Listing("Ana", services: ["aircon-other"]))
            .Should().Contain("I need: Aircon Technician,").And.NotContain("Others");
    }

    [Fact]
    public void SavedAnswers_RetiredTrade_IsDropped_SoTheQuestionIsAskedAgain()
    {
        var state = new OnboardingState
        {
            Seeker = new SeekerAnswers { CategoryId = "home_massage", ServiceId = "massage-hilot", OtherJobText = "x" },
            Professional = new ProfessionalDraft { CategoryId = "home_massage", ServiceIds = ["massage-hilot", "massage-other"], OtherJobText = "y" },
        };

        state.FitToCatalog();

        state.Seeker.CategoryId.Should().BeNull();
        state.Seeker.ServiceId.Should().BeNull();
        state.Seeker.OtherJobText.Should().BeNull();
        state.Professional.CategoryId.Should().BeNull();
        state.Professional.ServiceIds.Should().BeEmpty();
        state.Professional.OtherJobText.Should().BeNull();
    }

    [Fact]
    public void SavedAnswers_RetiredJob_IsDropped_TheTradeAndOthersStay()
    {
        var state = new OnboardingState
        {
            Seeker = new SeekerAnswers { CategoryId = "aircon", ServiceId = "aircon-other", OtherJobText = "thermostat" },
            Professional = new ProfessionalDraft { CategoryId = "aircon", ServiceIds = ["aircon-gone", "aircon-cleaning", "aircon-other"], OtherJobText = "duct" },
        };

        state.DropRetiredJobs();

        state.Seeker.ServiceId.Should().Be("aircon-other");
        state.Seeker.OtherJobText.Should().Be("thermostat");
        state.Professional.ServiceIds.Should().Equal("aircon-cleaning", "aircon-other");
        state.Professional.OtherJobText.Should().Be("duct");
    }

    [Fact]
    public void OthersTile_ComesAfterTheTrades_AndIsNeverATrade()
    {
        ReferenceData.TradeChoices.Select(t => t.Id).Should().Equal(
            ReferenceData.Categories.Select(c => c.Id).Append(ReferenceData.OtherTradeId));
        ReferenceData.TradeChoices[^1].Label.Should().Be(new LocalizedText("Others", "Wala sa nabanggit"));
        ReferenceData.Categories.Should().NotContain(c => c.Id == ReferenceData.OtherTradeId);
        AzureSQL.StoreText.IsCode(ReferenceData.OtherTradeId).Should().BeTrue("the server keeps its notes under this id");
    }
}
