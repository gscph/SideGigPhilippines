using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>What became of a new job post (<see cref="JobPostStore.AddAsync"/>).</summary>
/// <param name="Post">The saved post, or null when <paramref name="InvalidField"/> names what was wrong.</param>
/// <param name="InvalidField">The first field that's missing or not valid (its JSON name).</param>
public sealed record JobPostOutcome(JobPostDto? Post, string? InvalidField);

/// <summary>
/// Job posts (<c>JobPosts</c>, <c>JobPostPhotos</c>): a household posts the work it needs done (trade, service, what
/// is wrong, a budget, photos, town), and the workers of that trade who travel that far see it in their feed, posts
/// for a service they offer first, then nearest, then newest (<see cref="FeedAsync"/>). The household's point is
/// private: a worker gets the town and a distance in whole kilometres. Tarbaho stays a directory: the budget is the
/// household's own number and no money passes through the app.
/// </summary>
public sealed class JobPostStore(SideGigPhDb db, PhotoLinks photoLinks, ConfigStore config)
{
    /// <summary>The most posts one page holds, whatever the caller asks for.</summary>
    public const int MaxPage = 30;

    /// <summary>How many of a household's own posts are listed (newest first).</summary>
    public const int MaxMine = 50;

    /// <summary>What a report about a post carries in <c>Reports.SubjectId</c>: <c>job-post:{id}</c>.</summary>
    public const string ReportSubjectPrefix = "job-post:";

    /// <summary>
    /// Saves the caller's post, open at once. The trade, service and town must be ones we serve; the point is the
    /// phone's when it sent a valid one, else the centre of the town. The same words for the same service and town from
    /// the same person while that post is still open (a retry, a double tap) answer with the one already there.
    /// </summary>
    public async Task<JobPostOutcome> AddAsync(string uid, NewJobPostRequest request, CancellationToken cancellationToken)
    {
        if (await InvalidFieldAsync(request, cancellationToken) is { } field)
        {
            return new JobPostOutcome(null, field);
        }

        var description = request.Description.Trim();
        var again = await db.JobPosts.AsNoTracking().Include(p => p.Photos).FirstOrDefaultAsync(
            p => p.SeekerUid == uid && p.Status == JobPostStatuses.Open && p.ServiceId == request.ServiceId && p.CityId == request.CityId && p.Description == description,
            cancellationToken);
        if (again is not null)
        {
            return new JobPostOutcome(ToDto(again, uid), null);
        }

        var point = GeoRules.Point(request.Latitude, request.Longitude) ?? GeoRules.CentreOf([request.CityId]);
        var row = new JobPostRow
        {
            SeekerUid = uid,
            CategoryId = request.CategoryId,
            ServiceId = request.ServiceId,
            Description = description,
            BudgetPesos = request.BudgetPesos,
            CityId = request.CityId,
            Latitude = point?.Latitude,
            Longitude = point?.Longitude,
            CreatedAt = DateTimeOffset.UtcNow,
            Photos = [.. (request.PhotoIds ?? []).Distinct().Select((id, i) => new JobPostPhotoRow { PhotoId = id, Position = (short)i })],
        };
        db.JobPosts.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return new JobPostOutcome(ToDto(row, uid), null);
    }

    /// <summary>The caller's own posts, newest first (closed ones too; one the operator hid is left out).</summary>
    public async Task<List<JobPostDto>> MineAsync(string uid, CancellationToken cancellationToken)
    {
        var rows = await db.JobPosts.AsNoTracking().Include(p => p.Photos)
            .Where(p => p.SeekerUid == uid && p.Status != JobPostStatuses.Hidden)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Take(MaxMine)
            .ToListAsync(cancellationToken);
        return [.. rows.Select(row => ToDto(row, uid))];
    }

    /// <summary>The caller takes their own open post down. False when it isn't theirs or isn't there; closing twice changes nothing.</summary>
    public async Task<bool> CloseAsync(string uid, long postId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await db.JobPosts.Where(p => p.Id == postId && p.SeekerUid == uid && p.Status == JobPostStatuses.Open)
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.Status, JobPostStatuses.Closed).SetProperty(p => p.ClosedAt, now), cancellationToken);
        return await db.JobPosts.AnyAsync(p => p.Id == postId && p.SeekerUid == uid && p.Status != JobPostStatuses.Hidden, cancellationToken);
    }

    /// <summary>
    /// One page of the open posts the caller, a worker with a published listing, can take: posts of their own trade
    /// whose point is within the distance they travel from their base point (<c>Location.STDistance</c>, metres;
    /// when either point is unknown, posts in a town they go to). Never their own posts, nor those of someone
    /// blocked either way. Order: a service the worker offers first, then nearest, then newest.
    /// <paramref name="skip"/> is how many the caller already has.
    /// </summary>
    public async Task<JobPostFeedDto> FeedAsync(string uid, int skip, int take, CancellationToken cancellationToken)
    {
        if (!await db.Professionals.AnyAsync(p => p.Id == uid, cancellationToken))
        {
            return new JobPostFeedDto([], HasMore: false, HasListing: false);
        }

        take = Math.Clamp(take, 1, MaxPage);
        skip = Math.Max(skip, 0);
        var more = take + 1;
        var hits = await db.Database.SqlQuery<FeedHit>($"""
            SELECT j.Id,
                   CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.ProfessionalServices AS s WHERE s.ProfessionalId = p.Id AND s.ServiceId = j.ServiceId)
                             THEN 1 ELSE 0 END AS bit) AS OffersService,
                   CASE WHEN p.Location IS NOT NULL AND j.Location IS NOT NULL THEN p.Location.STDistance(j.Location) END AS DistanceMeters
              FROM dbo.JobPosts AS j
              JOIN dbo.Professionals AS p ON p.Id = {uid}
             WHERE j.Status = 'open'
               AND j.CategoryId = p.CategoryId
               AND j.SeekerUid <> p.Id
               AND NOT EXISTS (SELECT 1 FROM dbo.Blocks AS b
                                WHERE (b.BlockerUid = p.Id AND b.BlockedUid = j.SeekerUid) OR (b.BlockerUid = j.SeekerUid AND b.BlockedUid = p.Id))
               AND ((p.Location IS NOT NULL AND j.Location IS NOT NULL AND p.Location.STDistance(j.Location) <= p.RadiusKm * 1000.0)
                    OR ((p.Location IS NULL OR j.Location IS NULL)
                        AND EXISTS (SELECT 1 FROM dbo.ProfessionalCities AS c WHERE c.ProfessionalId = p.Id AND c.CityId = j.CityId)))
             ORDER BY OffersService DESC,
                      CASE WHEN p.Location IS NOT NULL AND j.Location IS NOT NULL THEN p.Location.STDistance(j.Location) ELSE 1000000000 END,
                      j.CreatedAt DESC, j.Id DESC
            OFFSET {skip} ROWS FETCH NEXT {more} ROWS ONLY
            """).ToListAsync(cancellationToken);

        var page = hits.Take(take).ToList();
        var ids = page.Select(hit => hit.Id).ToList();
        var rows = await db.JobPosts.AsNoTracking().Include(p => p.Photos).Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var posts = page.Where(hit => rows.ContainsKey(hit.Id))
            .Select(hit => ToDto(rows[hit.Id], uid, hit.OffersService, DistanceKm(hit.DistanceMeters)))
            .ToList();
        return new JobPostFeedDto(posts, hits.Count > take, HasListing: true);
    }

    /// <summary>
    /// The caller reports a post (fake, a prank, abuse). It joins the operator's report queue (<c>Reports</c>, about
    /// the household, subject <c>job-post:{id}</c>); the operator hides it by setting its status to
    /// <see cref="JobPostStatuses.Hidden"/>. False when it isn't open or is the caller's own. Reporting twice changes nothing.
    /// </summary>
    public async Task<bool> ReportAsync(string uid, long postId, CancellationToken cancellationToken)
    {
        var post = await db.JobPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId && p.Status == JobPostStatuses.Open, cancellationToken);
        if (post is null || post.SeekerUid == uid)
        {
            return false;
        }

        var subject = $"{ReportSubjectPrefix}{postId}";
        if (!await db.Reports.AnyAsync(r => r.ReporterUid == uid && r.SubjectId == subject, cancellationToken))
        {
            db.Reports.Add(new ReportRow
            {
                ReporterUid = uid,
                TargetUid = post.SeekerUid,
                Kind = ReportKinds.User,
                SubjectId = subject,
                Reason = ReportReasons.Fake,
                Note = StoreText.Limit(post.Description, SafetyStore.MaxNote),
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Delete account: the person's posts go, with their photo rows. Answers with the photo ids, so the caller can
    /// delete the stored photos too. Runs inside the caller's transaction.
    /// </summary>
    internal static async Task<List<string>> ForgetAsync(SideGigPhDb db, string uid, CancellationToken cancellationToken)
    {
        var photos = await db.JobPostPhotos.Where(photo => db.JobPosts.Any(p => p.Id == photo.PostId && p.SeekerUid == uid))
            .Select(photo => photo.PhotoId).ToListAsync(cancellationToken);
        await db.JobPosts.Where(p => p.SeekerUid == uid).ExecuteDeleteAsync(cancellationToken);
        return photos;
    }

    /// <summary>A distance for other people's eyes: whole kilometres, never under 1, so a point can't be worked out from it.</summary>
    public static int? DistanceKm(double? meters) => meters is { } m ? Math.Max(1, (int)Math.Round(m / 1000.0)) : null;

    /// <summary>The first field of a new post that's missing or not valid (its JSON name), or null when it can be saved.</summary>
    private async Task<string?> InvalidFieldAsync(NewJobPostRequest request, CancellationToken cancellationToken)
    {
        if (!StoreText.IsCode(request.CategoryId))
        {
            return "categoryId";
        }

        var served = (await config.GetAsync(cancellationToken)).Config;
        return served.Catalog.FirstOrDefault(c => c.Id == request.CategoryId) is not { } trade ? "categoryId"
            : !trade.Services.Any(s => s.Id == request.ServiceId) ? "serviceId"
            : !served.Towns.Any(t => t.Id == request.CityId) ? "cityId"
            : !JobPostRules.IsDescription(request.Description) ? "description"
            : !JobPostRules.IsBudget(request.BudgetPesos) ? "budgetPesos"
            : request.PhotoIds is { } photos && (photos.Count > JobPostRules.MaxPhotos || !photos.All(StoreText.IsPhotoId)) ? "photoIds"
            : (request.Latitude is not null || request.Longitude is not null) && GeoRules.Point(request.Latitude, request.Longitude) is null ? "latitude"
            : null;
    }

    private JobPostDto ToDto(JobPostRow row, string uid, bool offersService = false, int? distanceKm = null) => new(
        row.Id,
        row.CategoryId,
        row.ServiceId,
        row.Description,
        row.BudgetPesos,
        row.CityId,
        row.Status,
        [.. row.Photos.OrderBy(p => p.Position).Select(p => new PhotoDto(p.PhotoId, photoLinks.For(p.PhotoId)))],
        Mine: row.SeekerUid == uid,
        offersService,
        distanceKm,
        row.CreatedAt);

    /// <summary>One row of the feed query: which post, and how it relates to the worker.</summary>
    private sealed class FeedHit
    {
        public long Id { get; set; }
        public bool OffersService { get; set; }
        public double? DistanceMeters { get; set; }
    }
}
