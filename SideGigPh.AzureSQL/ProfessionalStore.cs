using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

public enum PublishOutcome
{
    Published,

    /// <summary>Anti-fraud rule 1: another listing took this number at the same moment (a race; see <see cref="ProfessionalStore.PublishAsync"/>).</summary>
    PhoneTaken,

    /// <summary>Anti-fraud rule 2: another listing has one of these photos.</summary>
    PhotoInUse,

    /// <summary>A required field is missing or isn't a valid id.</summary>
    Invalid,
}

/// <summary>Published Skilled Worker listings (<c>Professionals</c> and its services, towns and work photos).</summary>
public sealed class ProfessionalStore(SideGigPhDb db, PhotoLinks photoLinks, ListingCache cache, ConfigStore config)
{
    /// <summary>Most work photos a listing can have (the app's <c>ProWorkspace.MaxWorkPhotos</c>).</summary>
    public const int MaxWorkPhotos = 6;

    /// <summary>Fewest work photos a listing can be published with (the app's <c>ProWorkspace.MinWorkPhotos</c>).</summary>
    public const int MinWorkPhotos = 2;

    /// <summary>Fewest and most references a listing has (the app's <c>ProWorkspace.MinReferences</c> / <c>MaxReferences</c>).</summary>
    public const int MinReferences = 1;
    public const int MaxReferences = 3;

    /// <summary>
    /// The most jobs or towns one listing may send: far more than any trade has jobs or we serve towns, so a client
    /// can't make one publish insert thousands of rows.
    /// </summary>
    public const int MaxListed = 100;

    /// <summary>The most search results one page may ask for.</summary>
    public const int MaxSearchPage = 50;

    /// <summary>
    /// One trade's listings with their stars, in one area when given, unranked. <c>GET professionals</c> for app
    /// versions before paging; <see cref="SearchPageAsync"/> ranks.
    /// </summary>
    /// <param name="hidden">People hidden from the caller (blocks, <see cref="SafetyStore.HiddenForAsync"/>), left out after the shared cache.</param>
    public async Task<IReadOnlyList<ListingDto>> SearchAsync(
        string categoryId, string? areaId, IReadOnlySet<string> hidden, CancellationToken cancellationToken) =>
        [.. (await CandidatesAsync(categoryId, areaId, cancellationToken)).Where(c => !hidden.Contains(c.Listing.Id)).Select(c => c.Listing)];

    /// <summary>
    /// One page of a trade's workers ranked for this Seeker (<see cref="SearchRanker"/>), <paramref name="take"/> at
    /// most <see cref="MaxSearchPage"/>. The listings come from the 30 s <see cref="ListingCache"/>, so the next pages
    /// rank the same list again without reading the database.
    /// </summary>
    /// <param name="hidden">People hidden from the caller (blocks, <see cref="SafetyStore.HiddenForAsync"/>), left out after the shared cache.</param>
    public async Task<SearchPageDto> SearchPageAsync(
        string categoryId, string? areaId, RankRequest request, int skip, int take, IReadOnlySet<string> hidden, CancellationToken cancellationToken)
    {
        IReadOnlyList<RankCandidate> candidates = await CandidatesAsync(categoryId, areaId, cancellationToken);
        if (hidden.Count > 0)
        {
            candidates = [.. candidates.Where(c => !hidden.Contains(c.Listing.Id))];
        }

        var scales = RankScales.From((await config.GetAsync(cancellationToken)).Config);
        return SearchRanker.Rank(candidates, request, scales, DateTimeOffset.UtcNow)
            .Page(Math.Max(0, skip), Math.Clamp(take, 1, MaxSearchPage));
    }

    /// <summary>
    /// A trade's listings with their ranking signals, kept for <see cref="ListingCache.SearchFor"/>. Ids that can't be a
    /// trade or area find nobody without a database read or a cache entry (random ids would otherwise fill the cache).
    /// </summary>
    private Task<IReadOnlyList<RankCandidate>> CandidatesAsync(string categoryId, string? areaId, CancellationToken cancellationToken) =>
        !StoreText.IsCode(categoryId) || (areaId is not null && !StoreText.IsCode(areaId))
            ? Task.FromResult<IReadOnlyList<RankCandidate>>([])
            : cache.SearchAsync(categoryId, areaId, async () =>
            {
                var rows = await Listings()
                    .Where(p => p.CategoryId == categoryId && (areaId == null || p.AreaId == areaId))
                    .ToListAsync(cancellationToken);
                var listings = await WithReviewsAsync(rows, cancellationToken);
                var signals = await SignalsAsync(rows, cancellationToken);
                return [.. listings.Select(l => new RankCandidate(l, signals[l.Id]))];
            });

    /// <summary>
    /// How each worker behaves on the app (<see cref="WorkerSignals"/>), in two grouped queries: their chats from the
    /// last <see cref="SearchRanker.ResponsivenessWindow"/>, and their account's activity and age. A worker with no
    /// <c>Users</c> row (sample data) counts as active when they last edited the listing and joined when they published.
    /// </summary>
    private async Task<Dictionary<string, WorkerSignals>> SignalsAsync(IReadOnlyList<ProfessionalRow> rows, CancellationToken cancellationToken)
    {
        var ids = rows.Select(p => p.Id).ToList();
        var now = DateTimeOffset.UtcNow;
        var since = now - SearchRanker.ResponsivenessWindow;
        var graceCutoff = now - SearchRanker.ReplyGrace;
        const int dayInMinutes = 24 * 60;

        var chats = ids.Count == 0
            ? []
            : await db.Chats.AsNoTracking()
                .Where(c => ids.Contains(c.ProfessionalId) && c.CreatedAt >= since)
                // A just-started chat isn't held against them yet, and a chat where the Seeker called may have been
                // answered on the phone, so only a reply counts for those.
                .Where(c => c.ProfessionalRepliedAt != null
                    || (c.CreatedAt < graceCutoff && !db.ChatMessages.Any(m => m.ChatId == c.Id && m.Kind == Wire.Call)))
                .GroupBy(c => c.ProfessionalId)
                .Select(g => new
                {
                    g.Key,
                    Received = g.Count(),
                    Answered = g.Count(c => c.ProfessionalRepliedAt != null),
                    AverageMinutes = g.Average(c => c.ProfessionalRepliedAt == null
                        ? (double?)null
                        : EF.Functions.DateDiffMinute(c.CreatedAt, c.ProfessionalRepliedAt.Value) > dayInMinutes
                            ? dayInMinutes
                            : EF.Functions.DateDiffMinute(c.CreatedAt, c.ProfessionalRepliedAt.Value)),
                })
                .ToDictionaryAsync(g => g.Key, cancellationToken);
        var users = ids.Count == 0
            ? []
            : await db.Users.AsNoTracking()
                .Where(u => ids.Contains(u.Uid))
                .Select(u => new { u.Uid, u.LastActiveAt, u.CreatedAt })
                .ToDictionaryAsync(u => u.Uid, cancellationToken);

        return rows.ToDictionary(p => p.Id, p =>
        {
            var chat = chats.GetValueOrDefault(p.Id);
            var user = users.GetValueOrDefault(p.Id);
            var lastActive = user?.LastActiveAt is { } active && active > p.UpdatedAt ? active : p.UpdatedAt;
            return new WorkerSignals(chat?.Received ?? 0, chat?.Answered ?? 0, chat?.AverageMinutes, lastActive, user?.CreatedAt ?? p.PublishedAt);
        });
    }

    public async Task<ListingDto?> GetAsync(string id, CancellationToken cancellationToken) =>
        await Listings().FirstOrDefaultAsync(p => p.Id == id, cancellationToken) is { } row
            ? (await WithReviewsAsync([row], cancellationToken))[0]
            : null;

    /// <summary>
    /// Makes <paramref name="uid"/>'s listing live, or replaces it. The number is the account's verified number
    /// (from the sign-in token), so one phone gets one listing and one account (anti-fraud rule 1; a stale account
    /// still showing the number loses it); a photo can be on one listing only (rule 2).
    /// </summary>
    public async Task<PublishOutcome> PublishAsync(string uid, string mobileNumber, PublishListingRequest request, CancellationToken cancellationToken)
    {
        if (InvalidField(request, mobileNumber) is not null)
        {
            return PublishOutcome.Invalid;
        }

        var services = request.ServiceIds.Distinct().ToList();
        var cities = request.CityIds.Distinct().ToList();
        var workPhotos = request.WorkPhotoIds.Distinct().Take(MaxWorkPhotos).ToList();
        var references = request.References!;

        if (await db.UsedByAnotherAsync(uid, [request.ProfilePhotoId!, .. workPhotos], cancellationToken))
        {
            return PublishOutcome.PhotoInUse;
        }

        try
        {
            var outcome = await db.InTransactionAsync(async ct =>
            {
                // Anti-fraud rule 1, one number = one account, decided by who holds the number NOW: it is the token's
                // SMS-verified number, and the sign-in provider gives a number to one account at a time. So another
                // account still showing it is stale (an old account of the same person, an account deleted without
                // its rows, a recycled SIM): its listing would ring this person, and refusing left them stuck. That
                // listing comes down and that profile loses the number; the number stays on one account.
                await db.Professionals.Where(p => p.MobileNumber == mobileNumber && p.Id != uid).ExecuteDeleteAsync(ct);
                await db.Users.Where(u => u.PhoneNumber == mobileNumber && u.Uid != uid)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.PhoneNumber, (string?)null), ct);

                var now = DateTimeOffset.UtcNow;
                var row = await db.Professionals.FirstOrDefaultAsync(p => p.Id == uid, ct);
                if (row is null)
                {
                    row = new ProfessionalRow
                    {
                        Id = uid,
                        DisplayName = string.Empty,
                        MobileNumber = mobileNumber,
                        CategoryId = request.CategoryId,
                        AreaId = request.AreaId,
                        PublishedAt = now,
                    };
                    db.Professionals.Add(row);
                }
                else
                {
                    // Replace the lists in one go; the new rows are added below.
                    await db.ProfessionalServices.Where(s => s.ProfessionalId == uid).ExecuteDeleteAsync(ct);
                    await db.ProfessionalCities.Where(c => c.ProfessionalId == uid).ExecuteDeleteAsync(ct);
                    await db.ProfessionalWorkPhotos.Where(w => w.ProfessionalId == uid).ExecuteDeleteAsync(ct);
                    await db.ProfessionalReferences.Where(r => r.ProfessionalId == uid).ExecuteDeleteAsync(ct);
                }

                row.DisplayName = StoreText.Name(request.DisplayName, 60);
                row.MobileNumber = mobileNumber;
                row.CategoryId = request.CategoryId;
                row.AreaId = request.AreaId;
                row.ExperienceId = request.ExperienceId;
                row.RateTierId = request.RateTierId;
                row.ProfilePhotoId = request.ProfilePhotoId;
                row.UpdatedAt = now;

                db.ProfessionalServices.AddRange(services.Select(id => new ProfessionalServiceRow { ProfessionalId = uid, ServiceId = id }));
                db.ProfessionalCities.AddRange(cities.Select(id => new ProfessionalCityRow { ProfessionalId = uid, CityId = id }));
                db.ProfessionalWorkPhotos.AddRange(workPhotos.Select((id, i) =>
                    new WorkPhotoRow { PhotoId = id, ProfessionalId = uid, Position = (short)i }));
                db.ProfessionalReferences.AddRange(references.Select((r, i) =>
                    new ReferenceRow { ProfessionalId = uid, Position = (short)i, Name = StoreText.Name(r.Name, 60), MobileNumber = r.MobileNumber }));

                await db.SaveChangesAsync(ct);
                return PublishOutcome.Published;
            }, cancellationToken);
            cache.Clear();
            return outcome;
        }
        catch (DbUpdateException ex) when (SqlErrors.Violates(ex, Constraints.ProfessionalMobileNumber))
        {
            return PublishOutcome.PhoneTaken;
        }
        catch (DbUpdateException ex) when (SqlErrors.Violates(ex, Constraints.WorkPhotoKey) || SqlErrors.Violates(ex, Constraints.ProfessionalProfilePhoto))
        {
            return PublishOutcome.PhotoInUse;
        }
    }

    /// <summary>
    /// The first listing field that's missing or not valid (its JSON name), or null when it can be published.
    /// Besides the interview answers a listing needs a profile photo, at least <see cref="MinWorkPhotos"/> work
    /// photos and <see cref="MinReferences"/> to <see cref="MaxReferences"/> references with Philippine mobile
    /// numbers, none of them <paramref name="ownNumber"/> (the worker's own) and no number twice.
    /// </summary>
    public static string? InvalidField(PublishListingRequest request, string ownNumber) =>
        !StoreText.IsCode(request.CategoryId) ? "categoryId"
        : !StoreText.IsCode(request.AreaId) ? "areaId"
        : request.ServiceIds is not { Count: > 0 and <= MaxListed } || !request.ServiceIds.All(StoreText.IsCode) ? "serviceIds"
        : request.CityIds is not { Count: > 0 and <= MaxListed } || !request.CityIds.All(StoreText.IsCode) ? "cityIds"
        : request.ExperienceId is not null && !StoreText.IsCode(request.ExperienceId) ? "experienceId"
        : request.RateTierId is not null && !StoreText.IsCode(request.RateTierId) ? "rateTierId"
        : !StoreText.IsPhotoId(request.ProfilePhotoId) ? "profilePhotoId"
        : request.WorkPhotoIds is null || !request.WorkPhotoIds.All(StoreText.IsPhotoId)
          || request.WorkPhotoIds.Distinct().Count() < MinWorkPhotos ? "workPhotoIds"
        : request.References is not { Count: >= MinReferences and <= MaxReferences }
          || !request.References.All(r => StoreText.Limit(r?.Name, 60) is not null && StoreText.IsMobileNumber(r!.MobileNumber) && r.MobileNumber != ownNumber)
          || request.References.Select(r => r.MobileNumber).Distinct().Count() != request.References.Count ? "references"
        : null;

    /// <summary>Takes the listing out of search (its services, towns and photos go with it). Its reviews stay.</summary>
    public async Task RemoveAsync(string uid, CancellationToken cancellationToken)
    {
        await db.Professionals.Where(p => p.Id == uid).ExecuteDeleteAsync(cancellationToken);
        cache.Clear();
    }

    private IQueryable<ProfessionalRow> Listings() =>
        db.Professionals.AsNoTracking()
            .Include(p => p.Services)
            .Include(p => p.Cities)
            .Include(p => p.WorkPhotos)
            .AsSplitQuery();

    /// <summary>
    /// The listings with their average stars and review count, and how many references each has (two grouped
    /// queries). Only the count leaves the server: reference names and numbers stay private.
    /// </summary>
    private async Task<IReadOnlyList<ListingDto>> WithReviewsAsync(IReadOnlyList<ProfessionalRow> rows, CancellationToken cancellationToken)
    {
        var ids = rows.Select(p => p.Id).ToList();
        var reviews = ids.Count == 0
            ? new Dictionary<string, ReviewSummaryDto>()
            : await db.Reviews.AsNoTracking()
                .Where(r => ids.Contains(r.ProfessionalId))
                .GroupBy(r => r.ProfessionalId)
                .Select(g => new { g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => new ReviewSummaryDto(g.Average, g.Count), cancellationToken);
        var references = ids.Count == 0
            ? new Dictionary<string, int>()
            : await db.ProfessionalReferences.AsNoTracking()
                .Where(r => ids.Contains(r.ProfessionalId))
                .GroupBy(r => r.ProfessionalId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        return rows.Select(p => ToDto(p, reviews.GetValueOrDefault(p.Id), references.GetValueOrDefault(p.Id))).ToList();
    }

    private ListingDto ToDto(ProfessionalRow p, ReviewSummaryDto? reviews, int referenceCount) => new(
        p.Id,
        p.DisplayName,
        p.MobileNumber,
        p.CategoryId,
        p.AreaId,
        p.Services.Select(s => s.ServiceId).ToList(),
        p.Cities.Select(c => c.CityId).ToList(),
        p.ExperienceId,
        p.RateTierId,
        p.ProfilePhotoId is { } photoId ? new PhotoDto(photoId, photoLinks.For(photoId)) : null,
        p.WorkPhotos.OrderBy(w => w.Position).Select(w => new PhotoDto(w.PhotoId, photoLinks.For(w.PhotoId))).ToList(),
        reviews,
        referenceCount);
}
