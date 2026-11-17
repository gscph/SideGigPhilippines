using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Reviews;

/// <summary>
/// Reviews are how good workers rise in search and how Seekers decide whom to trust. Only Seekers who contacted a
/// worker may review them (no fake reviews), only 24 hours after the chat started (stars about the job, not the
/// first reply), stars must be 1–5, and writing again replaces the old review.
/// </summary>
public class ReviewTests
{
    private static ProfessionalListing Ana() => TestData.Listing("Ana");

    private static Task<FakeApp> SeekerWhoContactedAna(List<Review>? written = null, TimeSpan? chatAge = null) =>
        FakeApp.SeekerAsync(before: app =>
        {
            app.Chats.Inbox = [TestData.ChatWith("ana", TestData.SeekerUid, startedAt: DateTimeOffset.Now - (chatAge ?? TimeSpan.FromDays(2)))];
            app.Profiles.ById["ana"] = Ana();
            app.Reviews.BySeeker = written ?? [];
        });

    [Fact]
    public async Task ChatStartedLessThan24HoursAgo_CannotRateYet()
    {
        var app = await SeekerWhoContactedAna(chatAge: TimeSpan.FromHours(23));

        app.Seeker.CanRate("ana").Should().BeFalse();
        app.Seeker.ToRate.Should().BeEmpty("rating opens 24 hours after the first message");
        (await app.Seeker.SubmitReviewAsync(Ana(), 5, null, "Maricel")).Should().BeFalse();
        app.Reviews.Saved.Should().BeEmpty();
    }

    [Fact]
    public async Task ChatStarted24HoursAgo_CanRate()
    {
        var app = await SeekerWhoContactedAna(chatAge: TimeSpan.FromHours(24.1));

        app.Seeker.CanRate("ana").Should().BeTrue();
        app.Seeker.ToRate.Should().ContainSingle().Which.Id.Should().Be("ana");
    }

    [Fact]
    public async Task EarlierReview_CanStillBeChanged_EvenOnARecentChat()
    {
        var old = new Review("ana_seeker-uid", "ana", TestData.SeekerUid, "Maricel", 2, null, null, DateTimeOffset.Now.AddHours(-1));
        var app = await SeekerWhoContactedAna([old], chatAge: TimeSpan.FromHours(2));

        app.Seeker.CanRate("ana").Should().BeTrue();
        (await app.Seeker.SubmitReviewAsync(Ana(), 4, null, "Maricel")).Should().BeTrue();
    }

    [Fact]
    public async Task ALockedReview_CannotBeChanged()
    {
        var locked = new Review("ana_seeker-uid", "ana", TestData.SeekerUid, "Maricel", 3, null, null, DateTimeOffset.Now.AddDays(-1), Locked: true);
        var app = await SeekerWhoContactedAna([locked]);

        app.Seeker.ReviewLocked("ana").Should().BeTrue();
        app.Seeker.CanRate("ana").Should().BeFalse("both reviews of the service show: no changing stars after seeing the other's");
        (await app.Seeker.SubmitReviewAsync(Ana(), 1, null, "Maricel")).Should().BeFalse();
        app.Reviews.Saved.Should().BeEmpty();
    }

    [Fact]
    public void ReviewRules_OpenExactly24HoursAfterTheChatStarts()
    {
        var started = new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

        ReviewRules.CanRate(started, started.AddHours(24).AddTicks(-1)).Should().BeFalse();
        ReviewRules.CanRate(started, started.AddHours(24)).Should().BeTrue();
    }

    [Fact]
    public async Task SubmitReview_ContactedWorker_SavesItWithTheJobFromTheChat()
    {
        var app = await SeekerWhoContactedAna();

        var saved = await app.Seeker.SubmitReviewAsync(Ana(), 5, "  Mabilis at malinis  ", "  Maricel D.  ");

        saved.Should().BeTrue();
        var review = app.Reviews.Saved.Single();
        review.ProfessionalId.Should().Be("ana");
        review.SeekerId.Should().Be(TestData.SeekerUid);
        review.Rating.Should().Be(5);
        review.Comment.Should().Be("Mabilis at malinis");
        review.ReviewerName.Should().Be("Maricel D.");
        review.ServiceId.Should().Be("aircon-cleaning");
        app.Seeker.MyReviewOf("ana").Should().Be(review);
        app.Seeker.ToRate.Should().BeEmpty("Ana is rated now");
        app.Seeker.Answers.ReviewerName.Should().Be("Maricel D.", "the name is remembered for next time");
    }

    [Fact]
    public async Task SubmitReview_BlankComment_IsSavedAsNoComment()
    {
        var app = await SeekerWhoContactedAna();

        await app.Seeker.SubmitReviewAsync(Ana(), 4, "   ", "Maricel");

        app.Reviews.Saved.Single().Comment.Should().BeNull();
    }

    [Fact]
    public async Task SubmitReview_Again_ReplacesTheEarlierOne()
    {
        var old = new Review("ana_seeker-uid", "ana", TestData.SeekerUid, "Maricel", 2, null, null, DateTimeOffset.Now.AddDays(-3));
        var app = await SeekerWhoContactedAna([old]);
        app.Seeker.MyReviewOf("ana").Should().Be(old);

        await app.Seeker.SubmitReviewAsync(Ana(), 5, null, "Maricel");

        app.Seeker.MyReviews.Should().ContainSingle().Which.Rating.Should().Be(5);
    }

    [Fact]
    public async Task SubmitReview_WorkerNeverContacted_IsRefused()
    {
        var app = await FakeApp.SeekerAsync();

        (await app.Seeker.SubmitReviewAsync(Ana(), 5, null, "Maricel")).Should().BeFalse();
        app.Reviews.Saved.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task SubmitReview_StarsOutsideOneToFive_IsRefused(int stars)
    {
        var app = await SeekerWhoContactedAna();

        (await app.Seeker.SubmitReviewAsync(Ana(), stars, null, "Maricel")).Should().BeFalse();
        app.Reviews.Saved.Should().BeEmpty();
    }

    [Fact]
    public async Task SetName_IsTrimmed_AndBlankClearsIt()
    {
        var app = await FakeApp.SeekerAsync();

        await app.Seeker.SetNameAsync("  Ria  ");
        app.Seeker.Answers.ReviewerName.Should().Be("Ria");

        await app.Seeker.SetNameAsync(" ");
        app.Seeker.Answers.ReviewerName.Should().BeNull();
    }

    [Fact]
    public async Task WorkerReviews_AverageTheirStars()
    {
        var app = new FakeApp(TestData.FinishedPro(TestData.ReadyDraft()), TestData.Worker());
        app.Reviews.AboutPro =
        [
            new Review("a", TestData.WorkerUid, "s1", "A", 5, null, null, DateTimeOffset.Now),
            new Review("b", TestData.WorkerUid, "s2", "B", 4, null, null, DateTimeOffset.Now),
        ];
        await app.Pro.EnsureLoadedAsync();

        app.Pro.ReviewSummary.Should().Be(new ReviewSummary(4.5, 2));
    }

    [Fact]
    public async Task WorkerReviews_NoneYet_HasNoSummary()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());

        app.Pro.ReviewSummary.Should().BeNull();
    }
}
