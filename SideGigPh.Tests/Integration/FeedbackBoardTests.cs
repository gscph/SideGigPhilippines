using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The feedback board end to end (the app's <see cref="FeedbackBoard"/> → API → SQL): people suggest features, the
/// most voted come first, a look-alike is offered before a repeat is posted, one person has one vote, and Delete
/// account takes the person's votes with it. Every test words its suggestions with its own made-up tag, because all
/// tests share one board.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class FeedbackBoardTests(ApiTestHost api)
{
    private static string Tag() => $"zq{Guid.NewGuid():N}"[..10];

    [IntegrationFact]
    public async Task MostVotedSuggestion_ComesFirst_ForEveryone()
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        var ben = await TestPhone.SeekerAsync(api, "Ben");
        var cora = await TestPhone.SeekerAsync(api, "Cora");
        var quiet = await ana.Get<FeedbackBoard>().PostAsync($"Dark mode {tag}", null);
        var wanted = await ana.Get<FeedbackBoard>().PostAsync($"Pay with GCash {tag}", "So I don't need cash on hand");

        (await ben.Get<FeedbackBoard>().VoteAsync(SeenByAnother(wanted), on: true)).Should().BeTrue();
        (await cora.Get<FeedbackBoard>().VoteAsync(SeenByAnother(wanted), on: true)).Should().BeTrue();

        var board = cora.Get<FeedbackBoard>();
        await LoadAllAsync(board);
        var mine = board.Posts.Where(p => p.Title.EndsWith(tag)).ToList();
        mine.Select(p => p.Id).Should().Equal([wanted.Id, quiet.Id], "3 votes beat 1");
        mine[0].Votes.Should().Be(3, "the writer's own vote is the first");
        mine[0].VotedByMe.Should().BeTrue();
        mine[0].Mine.Should().BeFalse("Cora didn't write it");
        mine[0].Details.Should().Be("So I don't need cash on hand");
        mine[1].Votes.Should().Be(1);
        mine[1].VotedByMe.Should().BeFalse();
    }

    [IntegrationFact]
    public async Task SomeoneAlreadyAskedForIt_TheLookAlikeIsOffered_AndAVoteIsAddedInsteadOfARepeat()
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        var ben = await TestPhone.WorkerReadyToPublishAsync(api, "Ben");
        var first = await ana.Get<FeedbackBoard>().PostAsync($"Pay with GCash wallet {tag}", null);
        await ana.Get<FeedbackBoard>().PostAsync($"Dark mode {tag}", null);
        var board = ben.Get<FeedbackBoard>();

        var similar = await board.FindSimilarAsync($"Sana may GCash payment po {tag}");

        similar.Select(p => p.Id).Should().Equal([first.Id], "the dark mode idea is about something else");
        (await board.VoteAsync(similar[0], on: true)).Should().BeTrue();
        await LoadAllAsync(board);
        board.Posts.Single(p => p.Id == first.Id).Votes.Should().Be(2, "Ben confirmed it is the same and added his vote");
        board.Posts.Count(p => p.Title.EndsWith(tag)).Should().Be(2, "nothing new was posted");
    }

    [IntegrationTheory]
    [InlineData("Add a map gago ka", null)]
    [InlineData("Add a map please", "More: " + FakeTextModerator.Marker)]
    public async Task ARudeSuggestion_IsRefused_AndNeverReachesTheBoard(string title, string? details)
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        var board = ana.Get<FeedbackBoard>();

        var post = () => board.PostAsync($"{title} {tag}", details);

        await post.Should().ThrowAsync<TextNotAllowedException>();
        await LoadAllAsync(board);
        board.Posts.Should().NotContain(p => p.Title.EndsWith(tag));
    }

    [IntegrationFact]
    public async Task OnePerson_OneVote_AndItCanBeTakenBack()
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        var ben = await TestPhone.SeekerAsync(api, "Ben");
        var post = await ana.Get<FeedbackBoard>().PostAsync($"Book a schedule {tag}", null);
        var client = api.Client(api.Tokens.For(ben.Uid));

        using var once = await client.PutAsync($"feedback/{post.Id}/vote", null);
        using var twice = await client.PutAsync($"feedback/{post.Id}/vote", null);
        var afterTwice = await twice.Content.ReadFromJsonAsync(ApiJson.Default.FeedbackVoteDto);
        using var back = await client.DeleteAsync($"feedback/{post.Id}/vote");
        var afterBack = await back.Content.ReadFromJsonAsync(ApiJson.Default.FeedbackVoteDto);

        once.StatusCode.Should().Be(HttpStatusCode.OK);
        afterTwice!.Votes.Should().Be(2, "voting twice counts once");
        afterBack!.Votes.Should().Be(1);
        afterBack.VotedByMe.Should().BeFalse();
    }

    [IntegrationFact]
    public async Task SameSuggestionSentTwiceByTheSamePerson_IsOneSuggestion()
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        var board = ana.Get<FeedbackBoard>();

        var first = await board.PostAsync($"Show the price list {tag}", null);
        var again = await board.PostAsync($"  Show the price list {tag} ", null);

        again.Id.Should().Be(first.Id);
        again.Votes.Should().Be(1);
    }

    [IntegrationFact]
    public async Task OwnSuggestion_CanBeRemoved_OnlyWhileNobodyElseVoted()
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        var ben = await TestPhone.SeekerAsync(api, "Ben");
        var board = ana.Get<FeedbackBoard>();
        var alone = await board.PostAsync($"Typo idea {tag}", null);
        var shared = await board.PostAsync($"Wanted idea {tag}", null);
        (await ben.Get<FeedbackBoard>().VoteAsync(SeenByAnother(shared), on: true)).Should().BeTrue();

        (await ben.Get<FeedbackBoard>().RemoveAsync(alone)).Should().BeFalse("it isn't Ben's");
        (await board.RemoveAsync(alone)).Should().BeTrue();
        (await board.RemoveAsync(shared)).Should().BeFalse("Ben wants it too now");

        await LoadAllAsync(board);
        board.Posts.Where(p => p.Title.EndsWith(tag)).Select(p => p.Id).Should().Equal([shared.Id]);
    }

    [IntegrationFact]
    public async Task ReportedSuggestion_JoinsTheOperatorsQueue_AndAHiddenOneLeavesTheBoard()
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        var ben = await TestPhone.SeekerAsync(api, "Ben");
        var post = await ana.Get<FeedbackBoard>().PostAsync($"Call this number {tag}", null);
        var board = ben.Get<FeedbackBoard>();
        await LoadAllAsync(board);

        (await board.ReportAsync(board.Posts.Single(p => p.Id == post.Id))).Should().BeTrue();

        board.Posts.Should().NotContain(p => p.Id == post.Id, "it leaves the reporter's list at once");
        (await ScalarAsync("SELECT COUNT(*) FROM Reports WHERE ReporterUid = @a AND TargetUid = @b AND SubjectId = @c AND Status = 'open'",
            ben.Uid, ana.Uid, $"{FeedbackStore.ReportSubjectPrefix}{post.Id}")).Should().Be(1);

        await ScalarAsync("UPDATE FeedbackPosts SET Status = 'hidden' WHERE Id = @a; SELECT 1", post.Id);
        await LoadAllAsync(ana.Get<FeedbackBoard>());
        ana.Get<FeedbackBoard>().Posts.Should().NotContain(p => p.Id == post.Id, "the operator hid it");
        using var vote = await api.Client(api.Tokens.For(ben.Uid)).PutAsync($"feedback/{post.Id}/vote", null);
        vote.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [IntegrationFact]
    public async Task DeleteAccount_TakesTheVotes_AndLeavesTheSuggestionWithoutItsWriter()
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        var ben = await TestPhone.SeekerAsync(api, "Ben");
        var hers = await ana.Get<FeedbackBoard>().PostAsync($"Voice messages {tag}", null);
        var his = await ben.Get<FeedbackBoard>().PostAsync($"Tagalog search {tag}", null);
        (await ana.Get<FeedbackBoard>().VoteAsync(SeenByAnother(his), on: true)).Should().BeTrue();
        (await ben.Get<FeedbackBoard>().VoteAsync(SeenByAnother(hers), on: true)).Should().BeTrue();

        await ana.Account.DeleteAccountAsync();

        var board = ben.Get<FeedbackBoard>();
        await LoadAllAsync(board);
        board.Posts.Single(p => p.Id == his.Id).Votes.Should().Be(1, "her vote left with her");
        board.Posts.Single(p => p.Id == hers.Id).Votes.Should().Be(1, "her suggestion stays, with only Ben's vote");
        (await ScalarAsync("SELECT COUNT(*) FROM FeedbackPosts WHERE Id = @a AND AuthorUid IS NULL", hers.Id)).Should().Be(1);
        (await ScalarAsync("SELECT COUNT(*) FROM FeedbackVotes WHERE VoterUid = @a", ana.Uid)).Should().Be(0);
    }

    [IntegrationFact]
    public async Task TooShortATitle_OrAnUnknownSuggestion_IsRefused()
    {
        var phone = await TestPhone.SeekerAsync(api, "Ana");
        var client = api.Client(api.Tokens.For(phone.Uid));

        using var tooShort = await client.PostAsJsonAsync("feedback", new NewFeedbackRequest("ok", null, Wire.Seeker), ApiJson.Default.NewFeedbackRequest);
        using var unknown = await client.PutAsync("feedback/999999999/vote", null);
        using var notANumber = await client.PutAsync("feedback/abc/vote", null);

        tooShort.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        notANumber.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [IntegrationFact]
    public async Task TheBoardComesInPages()
    {
        var tag = Tag();
        var ana = await TestPhone.SeekerAsync(api, "Ana");
        var board = ana.Get<FeedbackBoard>();
        var posted = new List<long>();
        for (var i = 0; i < 3; i++)
        {
            posted.Add((await board.PostAsync($"Paged idea number {i} {tag}", null)).Id);
        }

        var client = api.Client(api.Tokens.For(ana.Uid));
        var first = await client.GetFromJsonAsync("feedback?skip=0&take=2", ApiJson.Default.FeedbackPageDto);
        var all = new List<FeedbackPostDto>(first!.Posts);
        for (var page = first; page!.HasMore;)
        {
            page = await client.GetFromJsonAsync($"feedback?skip={all.Count}&take=2", ApiJson.Default.FeedbackPageDto);
            all.AddRange(page!.Posts);
        }

        first.Posts.Should().HaveCount(2);
        first.HasMore.Should().BeTrue();
        all.Select(p => p.Id).Should().OnlyHaveUniqueItems().And.Contain(posted);
    }

    /// <summary>The writer's suggestion as another person's phone shows it: not theirs, not voted yet.</summary>
    private static FeedbackPost SeenByAnother(FeedbackPost post) => post with { Mine = false, VotedByMe = false };

    private static async Task LoadAllAsync(FeedbackBoard board)
    {
        await board.LoadAsync();
        while (board.HasMore)
        {
            await board.LoadMoreAsync();
        }
    }

    private async Task<int> ScalarAsync(string sql, params object[] values)
    {
        await using var connection = new SqlConnection(api.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        for (var i = 0; i < values.Length; i++)
        {
            command.Parameters.AddWithValue($"@{(char)('a' + i)}", values[i]);
        }

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
