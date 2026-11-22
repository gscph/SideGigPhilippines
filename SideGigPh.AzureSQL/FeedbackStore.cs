using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>Where a suggestion was written (<c>FeedbackPosts.Source</c>).</summary>
public static class FeedbackSources
{
    /// <summary>The app's feedback board (signed in).</summary>
    public const string App = "app";

    /// <summary>The public web page (<c>GET suggest</c>, the Play testers' feedback link); the writer is a cookie id.</summary>
    public const string Web = "web";
}

/// <summary>
/// The feedback board (<c>FeedbackPosts</c>, <c>FeedbackVotes</c>): signed-in people suggest features and vote for
/// the ones they want, and the board shows the most voted first, so the loudest wish is on top and repeats don't
/// add noise. Before posting, the app asks for look-alikes (<see cref="SimilarAsync"/>) and the person either adds
/// their vote to one or says theirs is different. Nobody sees who wrote or voted. The operator moves a suggestion
/// along (<see cref="FeedbackStatuses"/>) in the database.
/// </summary>
public sealed class FeedbackStore(SideGigPhDb db)
{
    /// <summary>The most suggestions one page holds, whatever the caller asks for.</summary>
    public const int MaxPage = 50;

    /// <summary>How many of the most voted suggestions are compared with a new one. The board is small; past this, older low-voted ones aren't matched.</summary>
    public const int MaxCompared = 500;

    /// <summary>What a report about a suggestion carries in <c>Reports.SubjectId</c>: <c>feedback:{id}</c>.</summary>
    public const string ReportSubjectPrefix = "feedback:";

    /// <summary>What a report names when the suggestion's writer deleted their account.</summary>
    public const string NoAuthor = "deleted-account";

    private static readonly string[] OnBoard = [FeedbackStatuses.Open, FeedbackStatuses.Planned, FeedbackStatuses.Done];

    /// <summary>
    /// One page of the board: suggestions still open or planned first, most voted first, then newest; finished ones
    /// after them. <paramref name="skip"/> is how many the caller already has.
    /// </summary>
    public async Task<FeedbackPageDto> ListAsync(string uid, int skip, int take, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, MaxPage);
        var rows = await Visible()
            .OrderBy(p => p.Status == FeedbackStatuses.Done ? 1 : 0)
            .ThenByDescending(p => p.Votes)
            .ThenByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip(Math.Max(skip, 0))
            .Take(take + 1)
            .ToListAsync(cancellationToken);
        var page = rows.Take(take).ToList();
        return new FeedbackPageDto(await ToDtosAsync(uid, page, cancellationToken), rows.Count > take);
    }

    /// <summary>
    /// The suggestions on the board that probably ask for the same thing as <paramref name="text"/>
    /// (<see cref="FeedbackMatch"/>), closest first, then most voted; at most <see cref="FeedbackRules.MaxSimilar"/>.
    /// </summary>
    public async Task<List<FeedbackPostDto>> SimilarAsync(string uid, string? text, CancellationToken cancellationToken)
    {
        var words = FeedbackMatch.Words(text);
        if (words.Count == 0)
        {
            return [];
        }

        var candidates = await Visible()
            .OrderByDescending(p => p.Votes)
            .ThenByDescending(p => p.Id)
            .Take(MaxCompared)
            .ToListAsync(cancellationToken);
        var similar = candidates
            .Select(row => (Row: row, Score: FeedbackMatch.Score(words, FeedbackMatch.Words(row.Title))))
            .Where(match => match.Score >= FeedbackMatch.MinScore)
            .OrderByDescending(match => match.Score)
            .ThenByDescending(match => match.Row.Votes)
            .Take(FeedbackRules.MaxSimilar)
            .Select(match => match.Row)
            .ToList();
        return await ToDtosAsync(uid, similar, cancellationToken);
    }

    /// <summary>
    /// Posts the caller's suggestion with their own vote as its first. Null when the title is too short or too long.
    /// The same title from the same person again (a retry, a double tap) answers with the one already there.
    /// <paramref name="source"/> says where it was written (<see cref="FeedbackSources"/>).
    /// </summary>
    public async Task<FeedbackPostDto?> AddAsync(
        string uid, NewFeedbackRequest request, CancellationToken cancellationToken, string source = FeedbackSources.App)
    {
        if (!FeedbackRules.IsTitle(request.Title) || request.Details is { Length: > 4 * FeedbackRules.MaxDetails }
            || BlockedWords.AnyIn(request.Title, request.Details))
        {
            return null;
        }

        var title = request.Title.Trim();
        if (await Visible().FirstOrDefaultAsync(p => p.AuthorUid == uid && p.Title == title, cancellationToken) is { } again)
        {
            return (await ToDtosAsync(uid, [again], cancellationToken))[0];
        }

        var now = DateTimeOffset.UtcNow;
        var row = await db.InTransactionAsync(async ct =>
        {
            var post = new FeedbackPostRow
            {
                AuthorUid = uid,
                Side = request.Side is Wire.Seeker or Wire.Professional ? request.Side : null,
                Title = title,
                Details = StoreText.Limit(request.Details, FeedbackRules.MaxDetails),
                Votes = 1,
                Source = source,
                CreatedAt = now,
            };
            db.FeedbackPosts.Add(post);
            await db.SaveChangesAsync(ct);
            db.FeedbackVotes.Add(new FeedbackVoteRow { PostId = post.Id, VoterUid = uid, CreatedAt = now });
            await db.SaveChangesAsync(ct);
            return post;
        }, cancellationToken);
        return ToDto(row, uid, votedByMe: true);
    }

    /// <summary>
    /// Adds (<paramref name="on"/>) or takes back the caller's vote and answers with the new count. Voting twice
    /// changes nothing. Null when the suggestion isn't on the board.
    /// </summary>
    public async Task<FeedbackVoteDto?> VoteAsync(string uid, long postId, bool on, CancellationToken cancellationToken)
    {
        if (!await Visible().AnyAsync(p => p.Id == postId, cancellationToken))
        {
            return null;
        }

        if (!on)
        {
            await db.FeedbackVotes.Where(v => v.PostId == postId && v.VoterUid == uid).ExecuteDeleteAsync(cancellationToken);
        }
        else if (!await db.FeedbackVotes.AnyAsync(v => v.PostId == postId && v.VoterUid == uid, cancellationToken))
        {
            db.FeedbackVotes.Add(new FeedbackVoteRow { PostId = postId, VoterUid = uid, CreatedAt = DateTimeOffset.UtcNow });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (SqlErrors.Violates(ex, "PK_FeedbackVotes"))
            {
                // The same vote saved by a retry at the same moment: already counted.
                db.ChangeTracker.Clear();
            }
        }

        await RecountAsync(db, [postId], cancellationToken);
        var votes = await db.FeedbackPosts.Where(p => p.Id == postId).Select(p => p.Votes).FirstOrDefaultAsync(cancellationToken);
        return new FeedbackVoteDto(postId, votes, on);
    }

    /// <summary>
    /// The writer takes their suggestion back. Only while nobody else has voted for it (it is theirs alone); once
    /// others want it too, it stays. False when it isn't theirs, isn't there, or others voted.
    /// </summary>
    public async Task<bool> DeleteAsync(string uid, long postId, CancellationToken cancellationToken)
    {
        var deleted = await db.FeedbackPosts
            .Where(p => p.Id == postId && p.AuthorUid == uid && !db.FeedbackVotes.Any(v => v.PostId == p.Id && v.VoterUid != uid))
            .ExecuteDeleteAsync(cancellationToken);
        // The phone sends a write once more when its answer was lost on the way (ApiClient.WriteTries): one already
        // gone is removed.
        return deleted > 0 || !await db.FeedbackPosts.AnyAsync(p => p.Id == postId, cancellationToken);
    }

    /// <summary>
    /// The caller reports a suggestion (spam, abuse, someone's number). It joins the operator's report queue
    /// (<c>Reports</c>, about the writer, subject <c>feedback:{id}</c>); the operator hides it by setting its status
    /// to <see cref="FeedbackStatuses.Hidden"/>. False when it isn't on the board or is the caller's own.
    /// </summary>
    public async Task<bool> ReportAsync(string uid, long postId, CancellationToken cancellationToken)
    {
        if (await Visible().FirstOrDefaultAsync(p => p.Id == postId, cancellationToken) is not { } post || post.AuthorUid == uid)
        {
            return false;
        }

        var subject = $"{ReportSubjectPrefix}{postId}";
        if (await db.Reports.AnyAsync(r => r.ReporterUid == uid && r.SubjectId == subject, cancellationToken))
        {
            return true; // reported already (a repeat of a report whose answer was lost, or a second tap)
        }

        db.Reports.Add(new ReportRow
        {
            ReporterUid = uid,
            TargetUid = post.AuthorUid ?? NoAuthor,
            Kind = ReportKinds.User,
            SubjectId = subject,
            Reason = ReportReasons.Inappropriate,
            Note = StoreText.Limit(post.Title, SafetyStore.MaxNote),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Delete account: the person's votes go (and the counts follow); the features they suggested stay without their
    /// uid, since the board never showed who wrote them. Runs inside the caller's transaction.
    /// </summary>
    internal static async Task ForgetAsync(SideGigPhDb db, string uid, CancellationToken cancellationToken)
    {
        var voted = await db.FeedbackVotes.Where(v => v.VoterUid == uid).Select(v => v.PostId).ToListAsync(cancellationToken);
        await db.FeedbackVotes.Where(v => v.VoterUid == uid).ExecuteDeleteAsync(cancellationToken);
        await RecountAsync(db, voted, cancellationToken);
        await db.FeedbackPosts.Where(p => p.AuthorUid == uid)
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.AuthorUid, (string?)null), cancellationToken);
    }

    /// <summary>Sets each suggestion's <c>Votes</c> to the number of vote rows it really has (so a count can never drift).</summary>
    private static Task RecountAsync(SideGigPhDb db, IReadOnlyCollection<long> postIds, CancellationToken cancellationToken) =>
        postIds.Count == 0
            ? Task.CompletedTask
            : db.FeedbackPosts.Where(p => postIds.Contains(p.Id))
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.Votes, p => db.FeedbackVotes.Count(v => v.PostId == p.Id)), cancellationToken);

    private IQueryable<FeedbackPostRow> Visible() => db.FeedbackPosts.AsNoTracking().Where(p => OnBoard.Contains(p.Status));

    private async Task<List<FeedbackPostDto>> ToDtosAsync(string uid, IReadOnlyList<FeedbackPostRow> rows, CancellationToken cancellationToken)
    {
        var ids = rows.Select(r => r.Id).ToList();
        var voted = ids.Count == 0
            ? []
            : await db.FeedbackVotes.AsNoTracking().Where(v => v.VoterUid == uid && ids.Contains(v.PostId)).Select(v => v.PostId).ToListAsync(cancellationToken);
        return [.. rows.Select(row => ToDto(row, uid, voted.Contains(row.Id)))];
    }

    private static FeedbackPostDto ToDto(FeedbackPostRow row, string uid, bool votedByMe) =>
        new(row.Id, row.Title, row.Details, row.Votes, row.Status, votedByMe, row.AuthorUid == uid, row.CreatedAt);
}
