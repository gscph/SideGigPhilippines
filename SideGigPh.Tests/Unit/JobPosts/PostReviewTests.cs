using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;
using SideGigPh.Tests.TestSupport;

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

    [Fact]
    public void ClosingTheForm_KeepsTypedWork_Once()
    {
        var board = new JobBoard(new FakeJobPosts(), new FakePhotoStore());
        var typed = Filled();
        typed.Description = "Hindi na malamig";

        board.KeepUnsent(typed);
        board.TakeUnsent().Should().BeSameAs(typed, "the problem they typed comes back");
        board.TakeUnsent().Should().BeNull("only once: the next form starts fresh");

        board.KeepUnsent(Filled());
        board.TakeUnsent().Should().BeNull("nothing typed, nothing to keep: the next form is pre-filled from their last search");
    }

    [Fact]
    public void ATradeWithJobs_OpensItsJobsPage_AndOthersDoesNot()
    {
        PostReview.HasJobChoice("aircon").Should().BeTrue("the tap moves on to the trade's jobs");
        PostReview.HasJobChoice(ReferenceData.OtherTradeId).Should().BeFalse("\"Others\" is its own job, picked with the trade");
        PostReview.HasJobChoice(null).Should().BeFalse();
    }

    [Fact]
    public void TheWhatSheet_SavesOnlyWithAJob()
    {
        var draft = new JobPostDraft();
        PostReview.PickTrade(draft, "aircon");

        PostReview.CanSave(draft, PostPart.What).Should().BeFalse("a trade alone can't be posted (Post stayed grey)");
        PostReview.CanSave(draft, PostPart.Where).Should().BeTrue("the other sheets save as they are");

        draft.ServiceId = "aircon-cleaning";
        PostReview.CanSave(draft, PostPart.What).Should().BeTrue();

        PostReview.PickTrade(draft, ReferenceData.OtherTradeId);
        PostReview.CanSave(draft, PostPart.What).Should().BeTrue("\"Others\" picks its one job with the trade");
    }

    [Fact]
    public void EachSheet_HasItsOwnUrlPart_AndBackGoesToTheForm()
    {
        foreach (var part in Enum.GetValues<PostPart>())
        {
            PostReview.FromSlug(PostReview.Slug(part)).Should().Be(part, "the URL opens the same sheet");
        }

        PostReview.Slug(PostPart.What).Should().Be("what");
        PostReview.FromSlug("BUDGET").Should().Be(PostPart.Budget);
        PostReview.FromSlug(null).Should().BeNull("the form itself");
        PostReview.FromSlug("photos").Should().BeNull("a word it doesn't know shows the form");

        PostReview.SheetDepth(null).Should().Be(1);
        PostReview.SheetDepth("aircon").Should().Be(2, "the trades page sits between a trade's jobs and the form");
    }

    [Fact]
    public void EachMissingField_BelongsToItsRow_AndTheProblemToTheForm()
    {
        PostReview.PartOf(JobPostField.Trade).Should().Be(PostPart.What);
        PostReview.PartOf(JobPostField.Service).Should().Be(PostPart.What);
        PostReview.PartOf(JobPostField.Town).Should().Be(PostPart.Where);
        PostReview.PartOf(JobPostField.Budget).Should().Be(PostPart.Budget);
        PostReview.PartOf(JobPostField.Description).Should().BeNull("typed on the form, not on a sheet");
    }

    [Fact]
    public void EmptyRows_SayWhatToDo_AndDoneRowsSayNothing()
    {
        var text = English();
        var empty = new JobPostDraft();

        text.PostRowMissing(empty, PostPart.What).Should().Be("Pick the kind of work.");
        text.PostRowMissing(empty, PostPart.Where).Should().Be("Pick your town.");
        text.PostRowMissing(empty, PostPart.Budget).Should().StartWith("Type your budget");
        text.PostRowMissing(new JobPostDraft { CategoryId = "aircon" }, PostPart.What).Should().Be("Pick what exactly you need.");

        var filled = Filled();
        foreach (var part in Enum.GetValues<PostPart>())
        {
            text.PostRowMissing(filled, part).Should().BeNull("{0} is done", part);
        }

        filled.BudgetMinPesos = 2000;
        text.PostRowMissing(filled, PostPart.Budget).Should().Contain("not less than", "a budget typed but refused still says why");
    }

    [Fact]
    public void UnderPost_EverythingStillNeeded_IsNamed_UntilItCanBePosted()
    {
        var text = English();

        text.PostStillNeeded(new JobPostDraft()).Should().Be("Still needed: the service, your town, your budget, the problem");

        var draft = Filled();
        text.PostStillNeeded(draft).Should().Be("Still needed: the problem");

        draft.Description = "Hindi na malamig ang aircon";
        text.PostStillNeeded(draft).Should().BeNull("Post works");

        text.Use(AppLanguage.Taglish);
        text.PostStillNeeded(new JobPostDraft { Description = draft.Description, CityId = "angono" })
            .Should().Be("Kulang pa: ang service, budget mo");
    }
}
