using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Components.Feedback;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Analytics;
using SideGigPh.Maui.Services.Diagnostics;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Feedback;

/// <summary>
/// The feedback board's rules without a server: which suggestions count as "the same" (so a person votes instead of
/// posting a repeat), and how the board on the phone behaves (a vote shows at once and is put back when it fails).
/// </summary>
public class FeedbackTests
{
    [Theory]
    [InlineData("Pay with GCash", "Sana may GCash payment po")]
    [InlineData("Add dark mode", "dark mode please")]
    [InlineData("Show reviews with photos", "Photo sa review")]
    [InlineData("Book a schedule", "Schedule booking for next week")]
    public void SameWish_InDifferentWords_IsSimilar(string posted, string typed) =>
        FeedbackMatch.IsSimilar(posted, typed).Should().BeTrue();

    [Theory]
    [InlineData("Pay with GCash", "Add dark mode")]
    [InlineData("Please add a map", "Please add voice messages")]
    [InlineData("Sana may map po", "Sana may video call po")]
    public void DifferentWishes_AreNotSimilar_EvenWhenTheySharePoliteWords(string posted, string typed) =>
        FeedbackMatch.IsSimilar(posted, typed).Should().BeFalse();

    [Fact]
    public void OnlySmallWords_MatchNothing()
    {
        FeedbackMatch.Words("please add this po").Should().BeEmpty();
        FeedbackMatch.IsSimilar("please add this po", "please add this po").Should().BeFalse();
    }

    [Fact]
    public void Words_IgnoreCasePunctuationAndPluralS()
    {
        FeedbackMatch.Words("Photos, PHOTO & reviews!").Should().BeEquivalentTo(["photo", "review"]);
    }

    [Theory]
    [InlineData("Add map", true)]
    [InlineData("  ok  ", false)]
    [InlineData(null, false)]
    public void ATitle_NeedsAFewLetters(string? title, bool ok) => FeedbackBoard.CanPost(title).Should().Be(ok);

    [Fact]
    public void ATitleLongerThanTheColumn_IsRefused() =>
        FeedbackBoard.CanPost(new string('a', FeedbackRules.MaxTitle + 1)).Should().BeFalse();

    [Fact]
    public async Task AVote_ShowsAtOnce_AndTakesTheServersCount()
    {
        var (board, server, _) = Board();
        server.Posts.Add(Post(1, votes: 4));
        await board.LoadAsync();
        var changes = 0;
        board.Changed += () => changes++;

        (await board.ToggleVoteAsync(board.Posts[0])).Should().BeTrue();

        board.Posts[0].Should().BeEquivalentTo(Post(1, votes: 5) with { VotedByMe = true });
        changes.Should().BeGreaterThanOrEqualTo(2, "once right away, once with the server's count");
    }

    [Fact]
    public async Task AVoteThatDoesNotGoThrough_IsPutBack()
    {
        var (board, server, _) = Board();
        server.Posts.Add(Post(1, votes: 4));
        await board.LoadAsync();
        server.Fail = true;

        (await board.ToggleVoteAsync(board.Posts[0])).Should().BeFalse();

        board.Posts[0].Votes.Should().Be(4);
        board.Posts[0].VotedByMe.Should().BeFalse();
    }

    [Fact]
    public async Task TappingAVotedSuggestion_TakesTheVoteBack()
    {
        var (board, server, _) = Board();
        server.Posts.Add(Post(1, votes: 4) with { VotedByMe = true });
        await board.LoadAsync();

        (await board.ToggleVoteAsync(board.Posts[0])).Should().BeTrue();

        board.Posts[0].Votes.Should().Be(3);
        board.Posts[0].VotedByMe.Should().BeFalse();
    }

    [Fact]
    public async Task ANewSuggestion_GoesOnTop_WithTheRole_AndIsCounted()
    {
        var (board, server, analytics) = Board(UserRole.Professional);
        server.Posts.Add(Post(1, votes: 9));
        await board.LoadAsync();

        var post = await board.PostAsync("  Show my earnings  ", "   ");

        board.Posts[0].Should().Be(post);
        server.Added.Should().ContainSingle().Which.Should().Be(("Show my earnings", (string?)null, (UserRole?)UserRole.Professional));
        analytics.Events.Should().Equal([AppEvents.FeedbackPosted]);
    }

    [Fact]
    public async Task ARefusedSuggestion_SaysSo_AndLeavesTheBoardAsItWas()
    {
        var (board, server, analytics) = Board();
        server.Posts.Add(Post(1, votes: 2));
        await board.LoadAsync();
        server.Refuse = true;

        var post = () => board.PostAsync("Rude words here", null);

        await post.Should().ThrowAsync<TextNotAllowedException>("the page shows Copy.TextNotAllowed, not \"didn't send\"");
        board.Posts.Select(p => p.Id).Should().Equal([1L]);
        server.Added.Should().BeEmpty();
        analytics.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task ScrollingToTheEnd_AddsTheNextPage_WithoutRepeats()
    {
        var (board, server, _) = Board();
        server.Posts.AddRange(Enumerable.Range(1, Paging.Feedback + 3).Select(i => Post(i, votes: 100 - i)));
        await board.LoadAsync();
        board.Posts.Should().HaveCount(Paging.Feedback);
        board.HasMore.Should().BeTrue();

        await board.LoadMoreAsync();

        board.Posts.Select(p => p.Id).Should().Equal(Enumerable.Range(1, Paging.Feedback + 3).Select(i => (long)i));
        board.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task RemovingOrReporting_TakesItOffTheList_UnlessItFailed()
    {
        var (board, server, _) = Board();
        server.Posts.AddRange([Post(1, votes: 1) with { Mine = true }, Post(2, votes: 3), Post(3, votes: 2)]);
        await board.LoadAsync();

        (await board.RemoveAsync(board.Posts[0])).Should().BeTrue();
        (await board.ReportAsync(board.Posts[0])).Should().BeTrue();
        server.Fail = true;
        (await board.ReportAsync(board.Posts[0])).Should().BeFalse();

        board.Posts.Select(p => p.Id).Should().Equal([3L]);
        server.Reported.Should().Equal([2L]);
    }

    [Fact]
    public void OnlyTheWriter_CanRemove_AndOnlyWhileTheirVoteIsTheOnlyOne()
    {
        (Post(1, votes: 1) with { Mine = true }).CanRemove.Should().BeTrue();
        (Post(1, votes: 2) with { Mine = true }).CanRemove.Should().BeFalse();
        Post(1, votes: 1).CanRemove.Should().BeFalse();
    }

    [Fact]
    public void Routes_GoBackToTheSidesMePage()
    {
        SeekerRoutes.Feedback.Should().Be("/feedback/seeker");
        ProRoutes.Feedback.Should().Be("/feedback/professional");
        FeedbackRoutes.Suggest(FeedbackRoutes.ProSide).Should().Be("/feedback/professional/new");
        FeedbackRoutes.Home(FeedbackRoutes.ProSide).Should().Be(ProRoutes.Me);
        FeedbackRoutes.Home(FeedbackRoutes.SeekerSide).Should().Be(SeekerRoutes.Me);
        FeedbackRoutes.Home("//evil.example").Should().Be(SeekerRoutes.Me, "a side from a link is never used as a path");
        FeedbackRoutes.Board("//evil.example").Should().Be("/feedback/seeker");
    }

    private static FeedbackPost Post(long id, int votes) =>
        new(id, $"Idea {id}", null, votes, FeedbackStatus.Open, VotedByMe: false, Mine: false, DateTimeOffset.UnixEpoch);

    private static (FeedbackBoard Board, FakeFeedback Server, FakeAnalytics Analytics) Board(UserRole role = UserRole.Seeker)
    {
        var server = new FakeFeedback();
        var analytics = new FakeAnalytics();
        var board = new FeedbackBoard(
            server,
            new FakeOnboardingStore(new OnboardingState { Role = role }),
            new AppAnalytics(analytics, new NoExperiments(), new BuildInfo("0.1.0-rc.1", "abc1234", AppChannels.Dev)));
        return (board, server, analytics);
    }
}
