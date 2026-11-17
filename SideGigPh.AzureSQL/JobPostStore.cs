using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>What became of a new job post (<see cref="JobPostStore.AddAsync"/>).</summary>
/// <param name="Post">The saved post, or null when <paramref name="InvalidField"/> names what was wrong.</param>
/// <param name="InvalidField">The first field that's missing or not valid (its JSON name).</param>
/// <param name="IsNew">The post was just made (false for a retry answered with the post already there), so workers are told.</param>
public sealed record JobPostOutcome(JobPostDto? Post, string? InvalidField, bool IsNew = false);

/// <summary>What the household's notification about an offer says (<see cref="JobPostStore.OfferAlertAsync"/>).</summary>
public sealed record OfferAlert(string SeekerUid, string WorkerName, int PricePesos);

/// <summary>What became of a worker's offer (<see cref="JobPostStore.SendOfferAsync"/>).</summary>
public enum OfferOutcome
{
    Sent,

    /// <summary>The post isn't open, is the worker's own, isn't their trade, or one of the two blocked the other.</summary>
    NotFound,

    InvalidPrice,
    InvalidNote,
}

/// <summary>
/// Job posts (<c>JobPosts</c>, <c>JobPostPhotos</c>): a household posts the work it needs done (trade, service, what
/// is wrong, a budget, photos, town), and the workers of that trade who travel that far see it in their feed, posts
/// for a service they offer first, then nearest, then newest (<see cref="FeedAsync"/>). The household's point is
/// private: a worker gets the town and a distance in whole kilometres. Tarbaho stays a directory: the budget is the
/// household's own number and no money passes through the app.
/// Offers (<c>JobOffers</c>, inDrive style): a worker takes the budget or names another price
/// (<see cref="SendOfferAsync"/>); the household sees each offer with the worker's listing (<see cref="OffersAsync"/>)
/// and picks one (<see cref="PickAsync"/>), which closes the post.
/// </summary>
public sealed class JobPostStore(SideGigPhDb db, PhotoLinks photoLinks, ConfigStore config, ProfessionalStore professionals)
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
            BudgetMinPesos = request.BudgetMinPesos,
            CityId = request.CityId,
            Latitude = point?.Latitude,
            Longitude = point?.Longitude,
            CreatedAt = DateTimeOffset.UtcNow,
            Photos = [.. (request.PhotoIds ?? []).Distinct().Select((id, i) => new JobPostPhotoRow { PhotoId = id, Position = (short)i })],
        };
        db.JobPosts.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return new JobPostOutcome(ToDto(row, uid), null, IsNew: true);
    }

    /// <summary>The caller's own posts, newest first (closed ones too; one the operator hid is left out).</summary>
    public async Task<List<JobPostDto>> MineAsync(string uid, CancellationToken cancellationToken)
    {
        var rows = await db.JobPosts.AsNoTracking().Include(p => p.Photos)
            .Where(p => p.SeekerUid == uid && p.Status != JobPostStatuses.Hidden)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Take(MaxMine)
            .ToListAsync(cancellationToken);
        var ids = rows.Select(row => row.Id).ToList();
        var offers = await db.JobOffers.AsNoTracking()
            .Where(o => ids.Contains(o.PostId) && o.Status != JobOfferStatuses.Withdrawn)
            .Select(o => new { o.PostId, o.ProUid, o.Status })
            .ToListAsync(cancellationToken);
        return [.. rows.Select(row => ToDto(
            row,
            uid,
            offerCount: offers.Count(o => o.PostId == row.Id),
            pickedProId: offers.FirstOrDefault(o => o.PostId == row.Id && o.Status == JobOfferStatuses.Picked)?.ProUid))];
    }

    /// <summary>
    /// The caller's own offers, as a worker, newest change first (at most <see cref="MaxMine"/>): waiting, picked, or
    /// closed without them. Offers they took back are left out (they chose that), and so are posts the operator hid
    /// and posts of someone blocked either way.
    /// </summary>
    public async Task<List<MyOfferDto>> MyOffersAsync(string uid, CancellationToken cancellationToken)
    {
        var offers = await db.JobOffers.AsNoTracking()
            .Where(o => o.ProUid == uid && o.Status != JobOfferStatuses.Withdrawn)
            .Join(db.JobPosts.AsNoTracking().Where(p => p.Status != JobPostStatuses.Hidden), o => o.PostId, p => p.Id,
                (o, p) => new { Offer = o, p.SeekerUid })
            .Where(x => !db.Blocks.Any(b => (b.BlockerUid == uid && b.BlockedUid == x.SeekerUid) || (b.BlockerUid == x.SeekerUid && b.BlockedUid == uid)))
            .OrderByDescending(x => x.Offer.UpdatedAt).ThenByDescending(x => x.Offer.Id)
            .Take(MaxMine)
            .Select(x => x.Offer)
            .ToListAsync(cancellationToken);
        var ids = offers.Select(o => o.PostId).ToList();
        var posts = await db.JobPosts.AsNoTracking().Include(p => p.Photos).Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        return [.. offers.Where(o => posts.ContainsKey(o.PostId)).Select(o =>
        {
            var post = posts[o.PostId];
            var state = o.Status == JobOfferStatuses.Picked ? MyOfferStates.Picked
                : post.Status == JobPostStatuses.Open ? MyOfferStates.Waiting
                : MyOfferStates.NotPicked;
            return new MyOfferDto(
                ToDto(post, uid, myOfferPesos: o.PricePesos),
                o.PricePesos,
                o.Note,
                state,
                o.UpdatedAt,
                state == MyOfferStates.Picked ? ChatStore.IdFor(uid, post.SeekerUid) : null);
        })];
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
        var mine = await db.JobOffers.AsNoTracking()
            .Where(o => ids.Contains(o.PostId) && o.ProUid == uid && o.Status == JobOfferStatuses.Sent)
            .ToDictionaryAsync(o => o.PostId, o => o.PricePesos, cancellationToken);
        var stars = await PickedJobStore.HouseholdStarsAsync(db, [.. rows.Values.Select(row => row.SeekerUid).Distinct()], cancellationToken);
        var posts = page.Where(hit => rows.ContainsKey(hit.Id))
            .Select(hit => ToDto(rows[hit.Id], uid, hit.OffersService, DistanceKm(hit.DistanceMeters),
                myOfferPesos: mine.TryGetValue(hit.Id, out var price) ? price : null,
                householdReviews: stars.GetValueOrDefault(rows[hit.Id].SeekerUid)))
            .ToList();
        return new JobPostFeedDto(posts, hits.Count > take, HasListing: true);
    }

    /// <summary>
    /// The caller, a worker, takes the post's budget or names another price, with an optional note. Only on an open
    /// post of their published listing's trade that isn't their own, and not between two people where one blocked the
    /// other. Sending again replaces their earlier price and note (and brings back an offer they took back).
    /// </summary>
    public async Task<OfferOutcome> SendOfferAsync(string uid, long postId, SendOfferRequest request, CancellationToken cancellationToken)
    {
        if (!JobOfferRules.IsPrice(request.PricePesos))
        {
            return OfferOutcome.InvalidPrice;
        }

        if (!JobOfferRules.IsNote(request.Note))
        {
            return OfferOutcome.InvalidNote;
        }

        if (!await OpenPostFor(uid, postId).AnyAsync(cancellationToken))
        {
            return OfferOutcome.NotFound;
        }

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        var now = DateTimeOffset.UtcNow;
        var changed = await db.JobOffers.Where(o => o.PostId == postId && o.ProUid == uid && o.Status != JobOfferStatuses.Picked)
            .ExecuteUpdateAsync(set => set
                .SetProperty(o => o.PricePesos, request.PricePesos)
                .SetProperty(o => o.Note, note)
                .SetProperty(o => o.Status, JobOfferStatuses.Sent)
                .SetProperty(o => o.UpdatedAt, now), cancellationToken);
        if (changed == 0 && !await db.JobOffers.AnyAsync(o => o.PostId == postId && o.ProUid == uid, cancellationToken))
        {
            db.JobOffers.Add(new JobOfferRow
            {
                PostId = postId,
                ProUid = uid,
                PricePesos = request.PricePesos,
                Note = note,
                CreatedAt = now,
                UpdatedAt = now,
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // A double tap got there first: that offer stands.
                db.ChangeTracker.Clear();
            }
        }

        return OfferOutcome.Sent;
    }

    /// <summary>The caller takes back their offer on a post still open. False when they have none waiting there.</summary>
    public async Task<bool> WithdrawOfferAsync(string uid, long postId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await db.JobOffers.Where(o => o.PostId == postId && o.ProUid == uid && o.Status == JobOfferStatuses.Sent
                                      && db.JobPosts.Any(p => p.Id == postId && p.Status == JobPostStatuses.Open))
            .ExecuteUpdateAsync(set => set.SetProperty(o => o.Status, JobOfferStatuses.Withdrawn).SetProperty(o => o.UpdatedAt, now), cancellationToken);
        return await db.JobOffers.AnyAsync(o => o.PostId == postId && o.ProUid == uid && o.Status == JobOfferStatuses.Withdrawn, cancellationToken);
    }

    /// <summary>
    /// The offers on the caller's own post, each with the worker's published listing and how far they are from the
    /// post (whole kilometres): the picked one first, then the lowest price, then the earliest. Never a withdrawn
    /// offer, one from a worker whose listing is down, or one between two people where one blocked the other. Null
    /// when the post isn't the caller's.
    /// </summary>
    public async Task<List<JobOfferDto>?> OffersAsync(string uid, long postId, CancellationToken cancellationToken)
    {
        var post = await db.JobPosts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == postId && p.SeekerUid == uid && p.Status != JobPostStatuses.Hidden, cancellationToken);
        if (post is null)
        {
            return null;
        }

        var hits = await db.Database.SqlQuery<OfferHit>($"""
            SELECT o.Id,
                   CASE WHEN p.Location IS NOT NULL AND j.Location IS NOT NULL THEN p.Location.STDistance(j.Location) END AS DistanceMeters
              FROM dbo.JobOffers AS o
              JOIN dbo.JobPosts AS j ON j.Id = o.PostId
              JOIN dbo.Professionals AS p ON p.Id = o.ProUid
             WHERE o.PostId = {postId}
               AND o.Status IN ('sent', 'picked')
               AND NOT EXISTS (SELECT 1 FROM dbo.Blocks AS b
                                WHERE (b.BlockerUid = o.ProUid AND b.BlockedUid = j.SeekerUid) OR (b.BlockerUid = j.SeekerUid AND b.BlockedUid = o.ProUid))
            """).ToListAsync(cancellationToken);
        var distances = hits.ToDictionary(hit => hit.Id, hit => hit.DistanceMeters);
        var ids = distances.Keys.ToList();
        var offers = await db.JobOffers.AsNoTracking().Where(o => ids.Contains(o.Id)).ToListAsync(cancellationToken);
        var listings = (await professionals.GetManyAsync([.. offers.Select(o => o.ProUid).Distinct()], cancellationToken)).ToDictionary(l => l.Id);
        return [.. offers.Where(o => listings.ContainsKey(o.ProUid))
            .OrderByDescending(o => o.Status == JobOfferStatuses.Picked).ThenBy(o => o.PricePesos).ThenBy(o => o.CreatedAt).ThenBy(o => o.Id)
            .Select(o => new JobOfferDto(o.Id, o.PostId, listings[o.ProUid], o.PricePesos, o.Note, o.Status, o.PricePesos == post.BudgetPesos,
                DistanceKm(distances[o.Id]), o.CreatedAt))];
    }

    /// <summary>
    /// The caller picks an offer on their own open post: the offer becomes picked and the post closes (workers stop
    /// seeing it). Answers with the picked offer, or null when the post isn't the caller's and open, or the offer
    /// isn't waiting on it.
    /// </summary>
    public async Task<JobOfferDto?> PickAsync(string uid, long postId, long offerId, CancellationToken cancellationToken)
    {
        var picked = await db.InTransactionAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var closed = await db.JobPosts.Where(p => p.Id == postId && p.SeekerUid == uid && p.Status == JobPostStatuses.Open
                                                      && db.JobOffers.Any(o => o.Id == offerId && o.PostId == postId && o.Status == JobOfferStatuses.Sent))
                .ExecuteUpdateAsync(set => set.SetProperty(p => p.Status, JobPostStatuses.Closed).SetProperty(p => p.ClosedAt, now), ct);
            if (closed == 0)
            {
                return false;
            }

            await db.JobOffers.Where(o => o.Id == offerId)
                .ExecuteUpdateAsync(set => set.SetProperty(o => o.Status, JobOfferStatuses.Picked).SetProperty(o => o.UpdatedAt, now), ct);
            return true;
        }, cancellationToken);

        return picked && await OffersAsync(uid, postId, cancellationToken) is { } offers
            ? offers.FirstOrDefault(o => o.Id == offerId)
            : null;
    }

    /// <summary>
    /// The other person's number, once a job post links the two with a picked offer (the caller's post picked their
    /// offer, or their post picked the caller's). Until then both numbers stay private and they talk in the app's
    /// chat. Null with no pick, a block either way, or no number on the account (a household signed in with Google
    /// may have none). A worker's number is their listing's, else the account's.
    /// </summary>
    public async Task<string?> ContactNumberAsync(string uid, string otherUid, CancellationToken cancellationToken)
    {
        if (uid == otherUid)
        {
            return null;
        }

        var picked = await db.JobOffers.AnyAsync(o => o.Status == JobOfferStatuses.Picked
            && db.JobPosts.Any(p => p.Id == o.PostId
                && ((p.SeekerUid == uid && o.ProUid == otherUid) || (p.SeekerUid == otherUid && o.ProUid == uid))), cancellationToken);
        if (!picked || await db.Blocks.AnyAsync(b => (b.BlockerUid == uid && b.BlockedUid == otherUid)
                                                     || (b.BlockerUid == otherUid && b.BlockedUid == uid), cancellationToken))
        {
            return null;
        }

        return await db.Professionals.Where(p => p.Id == otherUid).Select(p => p.MobileNumber).FirstOrDefaultAsync(cancellationToken)
            ?? await db.Users.Where(u => u.Uid == otherUid).Select(u => u.PhoneNumber).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// The workers to tell about a new post: the same match as their Requests feed (<see cref="FeedAsync"/>): the
    /// post's trade, within the worker's own <c>RadiusKm</c> of the post (a town match when a point is unknown), never
    /// the household itself or someone either of them blocked. The best <paramref name="max"/>: a service they offer
    /// first, then nearest.
    /// </summary>
    public async Task<IReadOnlyList<string>> WorkersToAlertAsync(long postId, int max, CancellationToken cancellationToken) =>
        await db.Database.SqlQuery<string>($"""
            SELECT TOP ({max}) p.Id AS Value
              FROM dbo.JobPosts AS j
              JOIN dbo.Professionals AS p ON p.CategoryId = j.CategoryId
             WHERE j.Id = {postId}
               AND j.Status = 'open'
               AND p.Id <> j.SeekerUid
               AND NOT EXISTS (SELECT 1 FROM dbo.Blocks AS b
                                WHERE (b.BlockerUid = p.Id AND b.BlockedUid = j.SeekerUid) OR (b.BlockerUid = j.SeekerUid AND b.BlockedUid = p.Id))
               AND ((p.Location IS NOT NULL AND j.Location IS NOT NULL AND p.Location.STDistance(j.Location) <= p.RadiusKm * 1000.0)
                    OR ((p.Location IS NULL OR j.Location IS NULL)
                        AND EXISTS (SELECT 1 FROM dbo.ProfessionalCities AS c WHERE c.ProfessionalId = p.Id AND c.CityId = j.CityId)))
             ORDER BY CASE WHEN EXISTS (SELECT 1 FROM dbo.ProfessionalServices AS s WHERE s.ProfessionalId = p.Id AND s.ServiceId = j.ServiceId)
                           THEN 0 ELSE 1 END,
                      CASE WHEN p.Location IS NOT NULL AND j.Location IS NOT NULL THEN p.Location.STDistance(j.Location) ELSE 1000000000 END,
                      p.Id
            """).ToListAsync(cancellationToken);

    /// <summary>
    /// Who to tell about <paramref name="workerUid"/>'s waiting offer on a post, and what it says (their listing's
    /// name and the price). Null when that offer isn't waiting.
    /// </summary>
    public Task<OfferAlert?> OfferAlertAsync(long postId, string workerUid, CancellationToken cancellationToken) =>
        (from o in db.JobOffers.AsNoTracking()
         join p in db.JobPosts on o.PostId equals p.Id
         join w in db.Professionals on o.ProUid equals w.Id
         where o.PostId == postId && o.ProUid == workerUid && o.Status == JobOfferStatuses.Sent
         select new OfferAlert(p.SeekerUid, w.DisplayName, o.PricePesos)).FirstOrDefaultAsync(cancellationToken);

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
    /// Delete account: the person's posts go, with their photo rows and the offers on them, and so do their own offers. Answers with the photo ids, so the caller can
    /// delete the stored photos too. Runs inside the caller's transaction.
    /// </summary>
    internal static async Task<List<string>> ForgetAsync(SideGigPhDb db, string uid, CancellationToken cancellationToken)
    {
        var photos = await db.JobPostPhotos.Where(photo => db.JobPosts.Any(p => p.Id == photo.PostId && p.SeekerUid == uid))
            .Select(photo => photo.PhotoId).ToListAsync(cancellationToken);
        await db.JobPosts.Where(p => p.SeekerUid == uid).ExecuteDeleteAsync(cancellationToken);
        await db.JobOffers.Where(o => o.ProUid == uid).ExecuteDeleteAsync(cancellationToken);
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

        // "Others" (a kind of work we don't list) is its own trade and job; the description says what it is.
        var knownJob = request.CategoryId == OtherWork.TradeId
            ? request.ServiceId == OtherWork.TradeId
            : served.Catalog.FirstOrDefault(c => c.Id == request.CategoryId) is { } trade ? trade.Services.Any(s => s.Id == request.ServiceId)
            : (bool?)null;
        return knownJob is null ? "categoryId"
            : knownJob == false ? "serviceId"
            : !served.Towns.Any(t => t.Id == request.CityId) ? "cityId"
            : !JobPostRules.IsDescription(request.Description) ? "description"
            : !JobPostRules.IsBudget(request.BudgetPesos) ? "budgetPesos"
            : !JobPostRules.IsBudgetRange(request.BudgetMinPesos, request.BudgetPesos) ? "budgetMinPesos"
            : request.PhotoIds is { } photos && (photos.Count > JobPostRules.MaxPhotos || !photos.All(StoreText.IsPhotoId)) ? "photoIds"
            : (request.Latitude is not null || request.Longitude is not null) && GeoRules.Point(request.Latitude, request.Longitude) is null ? "latitude"
            : null;
    }

    /// <summary>
    /// The caller's way to an open post they may make an offer on: of their published listing's trade, not their own,
    /// and not from someone they blocked or who blocked them.
    /// </summary>
    private IQueryable<JobPostRow> OpenPostFor(string uid, long postId) =>
        db.JobPosts.AsNoTracking().Where(j => j.Id == postId
            && j.Status == JobPostStatuses.Open
            && j.SeekerUid != uid
            && db.Professionals.Any(p => p.Id == uid && p.CategoryId == j.CategoryId)
            && !db.Blocks.Any(b => (b.BlockerUid == uid && b.BlockedUid == j.SeekerUid) || (b.BlockerUid == j.SeekerUid && b.BlockedUid == uid)));

    private JobPostDto ToDto(JobPostRow row, string uid, bool offersService = false, int? distanceKm = null,
        int offerCount = 0, int? myOfferPesos = null, string? pickedProId = null, ReviewSummaryDto? householdReviews = null) => new(
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
        row.CreatedAt,
        offerCount,
        myOfferPesos,
        pickedProId,
        row.BudgetMinPesos,
        householdReviews);

    /// <summary>One row of the offers query: which offer, and how far its worker is from the post.</summary>
    private sealed class OfferHit
    {
        public long Id { get; set; }
        public double? DistanceMeters { get; set; }
    }

    /// <summary>One row of the feed query: which post, and how it relates to the worker.</summary>
    private sealed class FeedHit
    {
        public long Id { get; set; }
        public bool OffersService { get; set; }
        public double? DistanceMeters { get; set; }
    }
}
