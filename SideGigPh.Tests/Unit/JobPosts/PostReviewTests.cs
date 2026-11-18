using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Tests.Unit.JobPosts;

/// <summary>
/// The new post page as Airbnb's "Review and continue": the card's rows (What / Where / Your budget, each Change or
/// Add) and the change sheets, whose Cancel must put the answers back. A wrong rule here posts the wrong service, town
/// or budget, or keeps an answer the household cancelled.
/// </summary>
public class PostReviewTests
{
    private static AppText English()
    {
        var text = new AppText();
        text.Use(AppLanguage.English);
        return text;
    }

    private static JobPostDraft Filled() => new()
    {
        CategoryId = "aircon", ServiceId = "aircon-cleaning", CityId = "angono", BarangayId = "angono-san-isidro",
        BudgetMinPesos = 800, BudgetPesos = 1500,
    };

    [Fact]
    public void Rows_SayTheAnswers_OrAskToAdd()
    {
        var text = English();
        var draft = Filled();

        text.PostWhat(draft).Should().Be("Aircon cleaning");
        text.PostTradeUnder(draft).Should().Be("Aircon Technician");
        text.PostWhere(draft).Should().Be("Angono · Brgy. San Isidro");
        PostReviewText.PostBudgetText(draft).Should().Be("₱800 – ₱1,500");
        text.PostAction(draft, PostPart.Budget).Should().Be("Change");

        var empty = new JobPostDraft();
        text.PostWhat(empty).Should().BeNull();
        text.PostWhere(empty).Should().BeNull();
        PostReviewText.PostBudgetText(empty).Should().BeNull();
        foreach (var part in Enum.GetValues<PostPart>())
        {
            text.PostAction(empty, part).Should().Be("Add", "{0} has no answer yet", part);
        }
    }

    [Fact]
    public void ATradeWithoutItsService_ShowsTheTrade_AndStillAsksToAdd()
    {
        var text = English();
        var draft = new JobPostDraft { CategoryId = "aircon" };

        text.PostWhat(draft).Should().Be("Aircon Technician");
        text.PostTradeUnder(draft).Should().BeNull("the trade is already the row's value");
        PostReview.Has(draft, PostPart.What).Should().BeFalse("a post needs the exact service");
    }

    [Fact]
    public void OnlyAnUpTo_IsOnePrice()
    {
        PostReviewText.PostBudgetText(new JobPostDraft { BudgetPesos = 1500 }).Should().Be("₱1,500");
        PostReviewText.PostBudgetText(new JobPostDraft { BudgetMinPesos = 1500, BudgetPesos = 1500 }).Should().Be("₱1,500");
    }

    [Fact]
    public void Cancel_PutsTheAnswersBack()
    {
        var draft = Filled();
        draft.Description = "Hindi na malamig";
        var before = PostReview.Take(draft);

        PostReview.PickTrade(draft, "it");
        PostReview.PickTown(draft, "taytay");
        draft.BudgetPesos = 99;
        PostReview.Restore(draft, before);

        draft.Should().BeEquivalentTo(Filled(), options => options.Excluding(d => d.Description));
        draft.Description.Should().Be("Hindi na malamig", "a sheet never touches the description");
    }

    [Fact]
    public void AnotherTrade_ClearsTheService_AnotherTown_ClearsTheBarangay()
    {
        var draft = Filled();

        PostReview.PickTrade(draft, "aircon");
        draft.ServiceId.Should().Be("aircon-cleaning", "the same trade keeps its service");
        PostReview.PickTrade(draft, "it");
        draft.ServiceId.Should().BeNull();
        PostReview.PickTrade(draft, ReferenceData.OtherTradeId);
        draft.ServiceId.Should().Be(ReferenceData.OtherTradeId, "\"Others\" has one service, picked for them");

        PostReview.PickTown(draft, "angono");
        draft.BarangayId.Should().Be("angono-san-isidro");
        PostReview.PickTown(draft, "taytay");
        draft.BarangayId.Should().BeNull("San Isidro is in Angono");
    }
}
