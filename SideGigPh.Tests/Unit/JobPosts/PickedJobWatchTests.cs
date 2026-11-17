using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.JobPosts;

/// <summary>
/// An open chat's picked-job card follows the other side's Mark done: marking done sends no chat message, so the card
/// re-reads the job while it waits, and stops once the job is completed.
/// </summary>
public class PickedJobWatchTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);

    private static PickedJob Job(bool household, bool worker) => new(7, "plumbing-clog", 1500, At, IAmTheHousehold: false, household, worker);

    [Fact]
    public void OnlyANewestJobNotYetCompleted_IsWaiting()
    {
        PickedJobWatch.IsWaiting([]).Should().BeFalse();
        PickedJobWatch.IsWaiting([Job(household: false, worker: true)]).Should().BeTrue();
        PickedJobWatch.IsWaiting([Job(household: true, worker: true)]).Should().BeFalse();
    }

    [Fact]
    public async Task WhenTheOtherSideMarksItDone_TheCardHearsOfIt()
    {
        var posts = new FakeJobPosts();
        var board = new JobBoard(posts, new FakePhotoStore());
        var clock = new ManualClock();
        var waiting = Job(household: false, worker: true);
        posts.PickedWith["seeker-1"] = [waiting];
        var heard = new TaskCompletionSource<IReadOnlyList<PickedJob>>();

        using var watch = PickedJobWatch.Start(board, new AppVisibility(), "seeker-1", [waiting],
            jobs => { heard.TrySetResult(jobs); return Task.CompletedTask; }, clock);
        posts.PickedWith["seeker-1"] = [Job(household: true, worker: true)];
        clock.Advance(PickedJobWatch.Interval);

        var jobs = await heard.Task.WaitAsync(TimeSpan.FromSeconds(5));
        jobs.Single().IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task WhileTheAppIsHidden_ItDoesNotRead()
    {
        var posts = new FakeJobPosts();
        var board = new JobBoard(posts, new FakePhotoStore());
        var clock = new ManualClock();
        var waiting = Job(household: false, worker: true);
        posts.PickedWith["seeker-1"] = [Job(household: true, worker: true)];
        var hidden = new AppVisibility();
        hidden.Set(false);
        var heard = false;

        using var watch = PickedJobWatch.Start(board, hidden, "seeker-1", [waiting], _ => { heard = true; return Task.CompletedTask; }, clock);
        clock.Advance(PickedJobWatch.Interval);
        await Task.Delay(100);

        heard.Should().BeFalse();
    }
}
