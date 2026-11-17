using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.Unit.Reviews;

/// <summary>
/// Blind two-way reviews: if one side's review showed first, the other could answer it in kind, so people would
/// inflate their stars out of fear. After a completed job each review stays hidden until both reviewed, or 14 days.
/// </summary>
public class BlindReviewRulesTests
{
    private static readonly DateTimeOffset Done = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TheWindowIs14Days()
    {
        ReviewRules.BlindFor.Should().Be(TimeSpan.FromDays(14));
        ReviewRules.OpensAt(Done).Should().Be(Done.AddDays(14));
    }

    [Fact]
    public void AReviewAfterTheJob_IsHidden_UntilTheOtherSideReviews()
    {
        var mine = Done.AddHours(1);

        ReviewRules.IsHidden(mine, otherWrittenAt: null, [Done], Done.AddDays(1)).Should().BeTrue();
        ReviewRules.IsHidden(mine, otherWrittenAt: Done.AddHours(2), [Done], Done.AddDays(1)).Should().BeFalse("both reviewed");
    }

    [Fact]
    public void AReviewShows_14DaysAfterTheJob_EvenIfTheOtherNeverReviews()
    {
        var mine = Done.AddHours(1);

        ReviewRules.IsHidden(mine, null, [Done], ReviewRules.OpensAt(Done).AddTicks(-1)).Should().BeTrue();
        ReviewRules.IsHidden(mine, null, [Done], ReviewRules.OpensAt(Done)).Should().BeFalse();
    }

    [Fact]
    public void TheOthersReviewFromBeforeTheJob_DoesNotCount()
    {
        ReviewRules.IsHidden(Done.AddHours(1), otherWrittenAt: Done.AddDays(-3), [Done], Done.AddDays(1)).Should().BeTrue(
            "an old review was already seen, so it can't stand for this job's review");
    }

    [Fact]
    public void AReviewWithoutACompletedJob_OrWrittenBeforeIt_ShowsAtOnce()
    {
        ReviewRules.IsHidden(Done, null, [], Done).Should().BeFalse("a one-way review after a chat");
        ReviewRules.IsHidden(Done.AddDays(-1), null, [Done], Done.AddDays(1)).Should().BeFalse("written before the job: it stays as it was");
    }

    [Fact]
    public void AnyRecentJob_KeepsANewReviewHidden()
    {
        var older = Done.AddDays(-30);

        ReviewRules.IsHidden(Done.AddHours(1), otherWrittenAt: older.AddHours(1), [older, Done], Done.AddDays(1)).Should().BeTrue(
            "the other's review is about the older job; the new one waits for theirs");
    }

    private static PickedJob Job(int? mine = null, bool theyReviewed = false, int? theirs = null, bool completed = true) =>
        new(1, "construction-plumbing", 900, Done.AddDays(-1), IAmTheHousehold: true, DoneByHousehold: true, DoneByWorker: completed, mine,
            completed ? Done : null, theyReviewed, theirs);

    [Fact]
    public void Stage_FollowsBothReviewsAndTheWindow()
    {
        var soon = Done.AddDays(1);

        BlindReviews.StageOf(Job(completed: false), soon).Should().Be(BlindReviewStage.NotCompleted);
        BlindReviews.StageOf(Job(), soon).Should().Be(BlindReviewStage.WaitingForMe);
        BlindReviews.StageOf(Job(theyReviewed: true), soon).Should().Be(BlindReviewStage.WaitingForMe);
        BlindReviews.StageOf(Job(mine: 4), soon).Should().Be(BlindReviewStage.WaitingForThem);
        BlindReviews.StageOf(Job(mine: 4, theyReviewed: true, theirs: 5), soon).Should().Be(BlindReviewStage.Open);
        BlindReviews.StageOf(Job(mine: 4), ReviewRules.OpensAt(Done)).Should().Be(BlindReviewStage.WindowClosed);
    }

    [Fact]
    public void DaysLeft_CountsUpAndNeverSaysZero()
    {
        BlindReviews.DaysLeft(Job(), Done.AddHours(1)).Should().Be(14);
        BlindReviews.DaysLeft(Job(), Done.AddDays(13).AddHours(1)).Should().Be(1);
        BlindReviews.DaysLeft(Job(), Done.AddDays(14).AddHours(-1)).Should().Be(1);
    }

    [Fact]
    public void DaysLeft_PhoneClockBehindTheServer_StillSays14()
    {
        BlindReviews.DaysLeft(Job(), Done.AddSeconds(-30)).Should().Be(14, "the window is 14 days, never \"in 15 days\"");
    }

    [Fact]
    public void AReviewLocks_OnceBothReviewsOfTheLatestJobShow()
    {
        var mine = Done.AddHours(1);

        ReviewRules.IsLocked(mine, otherWrittenAt: null, [Done], Done.AddDays(1)).Should().BeFalse("hidden: it can still change");
        ReviewRules.IsLocked(mine, otherWrittenAt: Done.AddHours(2), [Done], Done.AddDays(1)).Should().BeTrue("both show");
        ReviewRules.IsLocked(mine, otherWrittenAt: null, [Done], ReviewRules.OpensAt(Done)).Should().BeTrue("the 14 days passed");
    }

    [Fact]
    public void AReviewNotAboutTheLatestJob_DoesNotLock()
    {
        ReviewRules.IsLocked(Done, null, [], Done.AddDays(30)).Should().BeFalse("a one-way review after a chat");
        ReviewRules.IsLocked(Done.AddDays(-1), Done.AddHours(1), [Done], Done.AddDays(1)).Should().BeFalse("written before the job");
        ReviewRules.IsLocked(Done.AddHours(1), Done.AddHours(2), [Done, Done.AddDays(20)], Done.AddDays(21)).Should().BeFalse(
            "a newer service: the review can be written again");
    }
}
