using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.Unit.Moderation;

/// <summary>
/// The app's forms hold back text the server would refuse for rude words (<see cref="RudeWords"/>): the post's
/// description, an offer note, a feedback suggestion and the name page's check. Without these, the person taps send
/// and only sees "didn't send" with no reason.
/// </summary>
public class RudeWordsFormTests
{
    private static AppText English()
    {
        var text = new AppText();
        text.Use(AppLanguage.English);
        return text;
    }

    private static JobPostDraft Draft(string description) => new()
    {
        CategoryId = "construction",
        ServiceId = "construction-plumbing",
        CityId = "taytay",
        BudgetPesos = 800,
        Description = description,
    };

    [Fact]
    public void A_rude_description_is_missing_and_says_why()
    {
        var draft = Draft("Barado ang lababo, put@ng ina");

        JobBoard.FirstMissing(draft).Should().Be(JobPostField.Description);
        JobBoard.MissingText(JobPostField.Description, English(), draft).Should().Be("Please take out the rude words.");
        JobBoard.CanPost(Draft("Barado ang lababo sa kusina")).Should().BeTrue();
    }

    [Fact]
    public void A_rude_offer_note_cannot_be_sent()
    {
        JobBoard.CanOffer(900, "g@g0 ka").Should().BeFalse();
        JobBoard.CanOffer(900, "Kaya ko bukas ng umaga").Should().BeTrue();
    }

    [Fact]
    public void A_rude_suggestion_cannot_be_posted()
    {
        FeedbackBoard.CanPost("Add a dark mode please", "vovo").Should().BeFalse();
        FeedbackBoard.CanPost("Add a b0b0 button").Should().BeFalse();
        FeedbackBoard.CanPost("Add a dark mode please", "Mas madali sa gabi").Should().BeTrue();
    }

    [Fact]
    public void The_name_page_check_uses_the_same_lists()
    {
        var check = new RudeWordsCheck();

        check.IsRude("Kup@l").Should().BeTrue();
        check.IsRude("Juan").Should().BeFalse();
    }
}
