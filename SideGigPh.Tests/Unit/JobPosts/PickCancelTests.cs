using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.JobPosts;

/// <summary>
/// Calling off a picked job on the phone (<see cref="PickCancel"/>, <see cref="JobBoard.CancelPickAsync"/>): each side
/// picks from its own reasons, a completed job stays, a failed call changes nothing, and the chat message says why.
/// </summary>
public class PickCancelTests
{
    private readonly FakeJobPosts _posts = new();
    private readonly JobBoard _board;

    public PickCancelTests() => _board = new JobBoard(_posts, new FakePhotoStore());

    private static PickedJob Job(bool household, bool completed = false) =>
        new(7, "construction-plumbing", 900, DateTimeOffset.UnixEpoch, household, completed, completed);

    private PickedJob Picked(bool household, bool completed = false)
    {
        var job = Job(household, completed);
        _posts.PickedWith["other"] = [job];
        return job;
    }

    [Fact]
    public void A_household_can_say_the_worker_didnt_come_but_not_that_it_cant_make_it()
    {
        PickCancel.ReasonsFor(household: true).Should().Contain(PickCancelReasons.NoShow).And.NotContain(PickCancelReasons.CantMakeIt);
        PickCancel.ReasonsFor(household: false).Should().Contain(PickCancelReasons.CantMakeIt).And.NotContain(PickCancelReasons.NoShow);
        PickCancel.ReasonsFor(household: true).Last().Should().Be(PickCancelReasons.Other);
        PickCancel.ReasonsFor(household: false).Last().Should().Be(PickCancelReasons.Other);
    }

    [Fact]
    public void It_needs_a_reason_of_this_side_and_a_note_that_fits()
    {
        PickCancel.IsReady(true, null, null).Should().BeFalse();
        PickCancel.IsReady(true, PickCancelReasons.CantMakeIt, null).Should().BeFalse("that is the worker's reason");
        PickCancel.IsReady(true, PickCancelReasons.NoShow, new string('a', PickCancelRules.MaxNote + 1)).Should().BeFalse();
        PickCancel.IsReady(true, PickCancelReasons.NoShow, "  ").Should().BeTrue();
    }

    [Fact]
    public void A_completed_job_cant_be_called_off()
    {
        PickCancel.CanCancel(Job(household: true)).Should().BeTrue();
        PickCancel.CanCancel(Job(household: true) with { DoneByHousehold = true }).Should().BeTrue("one side done is not completed");
        PickCancel.CanCancel(Job(household: true, completed: true)).Should().BeFalse();
    }

    [Fact]
    public async Task Cancelling_sends_the_reason_and_the_trimmed_note_and_opens_the_post_again()
    {
        var job = Picked(household: true);
        _posts.SeedMine(new JobPost(7, "construction", "construction-plumbing", "Barado ang lababo sa kusina", 900, "taytay", [],
            IsOpen: false, Mine: true, OffersService: false, DistanceKm: null, DateTimeOffset.UnixEpoch, PickedProId: "other"));
        await _board.LoadMineAsync();

        (await _board.CancelPickAsync(job, PickCancelReasons.NoShow, "  Hindi dumating  ")).Should().Be(CancelPickResult.Cancelled);

        _posts.Cancelled.Should().Equal((7L, PickCancelReasons.NoShow, (string?)"Hindi dumating"));
        _board.Mine.Single().Should().Match<JobPost>(p => p.IsOpen && p.PickedProId == null);
    }

    [Fact]
    public async Task Offline_nothing_changes_and_it_says_it_didnt_send()
    {
        var job = Picked(household: false);
        _posts.Fail = true;

        (await _board.CancelPickAsync(job, PickCancelReasons.CantMakeIt, null)).Should().Be(CancelPickResult.Failed);

        _posts.Cancelled.Should().BeEmpty();
    }

    [Fact]
    public async Task Completed_meanwhile_answers_completed()
    {
        var job = Picked(household: true);
        _posts.PickedWith["other"] = [job with { DoneByHousehold = true, DoneByWorker = true }];

        (await _board.CancelPickAsync(job, PickCancelReasons.NotNeeded, null)).Should().Be(CancelPickResult.Completed);
    }

    [Fact]
    public void The_chat_message_names_the_service_and_the_reason_with_the_note_under_it()
    {
        var text = new AppText();

        text.CancelMessage("Plumbing", PickCancelReasons.CantMakeIt, null)
            .Should().Be("I cancelled this: Plumbing. Reason: I can't make it.");
        text.CancelMessage("Plumbing", PickCancelReasons.NoShow, " Sayang ")
            .Should().Be("I cancelled this: Plumbing. Reason: The worker didn't come.\nSayang");
    }
}
