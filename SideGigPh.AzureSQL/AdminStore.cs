using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>One page of an admin list: what to look for, how to sort, which rows. <see cref="Take"/> is kept within 1–<see cref="AdminLists.MaxTake"/>.</summary>
public sealed record AdminListQuery(string? Search = null, string? Sort = null, bool Desc = true, int Skip = 0, int Take = AdminLists.DefaultTake, string? Filter = null)
{
    public int SafeSkip => Math.Max(0, Skip);

    public int SafeTake => Math.Clamp(Take, 1, AdminLists.MaxTake);

    /// <summary>The search words, trimmed and cut, or null.</summary>
    public string? Words => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim()[..Math.Min(Search.Trim().Length, 100)];
}

/// <summary>How the operator site shows a person's contact details: enough to tell two apart, not enough to contact them.</summary>
public static class AdminMask
{
    private const char Dot = '•';

    /// <summary><c>+639171234567</c> → <c>+63 9•• ••• 4567</c>; null or blank stays null.</summary>
    public static string? Phone(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return null;
        }

        var digits = new string(number.Where(char.IsDigit).ToArray());
        if (digits.Length < 7)
        {
            return new string(Dot, digits.Length);
        }

        var last = digits[^4..];
        return digits.StartsWith("63", StringComparison.Ordinal) && digits.Length == 12
            ? $"+63 {digits[2]}{Dot}{Dot} {Dot}{Dot}{Dot} {last}"
            : $"{new string(Dot, digits.Length - 4)}{last}";
    }

    /// <summary><c>juan@gmail.com</c> → <c>j•••@gmail.com</c>; null or blank stays null.</summary>
    public static string? Email(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var at = email.IndexOf('@');
        return at <= 0 ? $"{email[0]}{Dot}{Dot}{Dot}" : $"{email[0]}{Dot}{Dot}{Dot}{email[at..]}";
    }

    /// <summary>A random install id cut to its first 8 characters: enough to group one phone's reports.</summary>
    public static string Install(string installId) => installId.Length > 8 ? installId[..8] : installId;
}

/// <summary>
/// The operator site's reads (<c>ops/…</c>): the dashboard's numbers and the lists of accounts, households, workers,
/// error reports and what people typed under "Others", a page at a time. Read only, no tracking. Contact details leave here masked
/// (<see cref="AdminMask"/>); review counts include blind reviews still hidden from the public.
/// </summary>
public sealed class AdminStore(SideGigPhDb db)
{
    /// <summary>Philippine time, for "today" and the sign-ups per day.</summary>
    public static readonly TimeSpan Manila = TimeSpan.FromHours(8);

    /// <summary>How many days of sign-ups the dashboard draws.</summary>
    public const int SignUpDays = 30;

    /// <summary>How many rows each "by trade / by town" table keeps.</summary>
    public const int TopRows = 12;

    public async Task<AdminStatsDto> StatsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var today = new DateTimeOffset(now.ToOffset(Manila).Date, Manila);
        var week = now.AddDays(-7);
        var month = now.AddDays(-30);
        var day = now.AddDays(-1);

        var users = db.Users.AsNoTracking();
        var listings = db.Professionals.AsNoTracking();
        var userStats = new AdminUserStatsDto(
            Total: await users.CountAsync(cancellationToken),
            Seekers: await users.CountAsync(u => u.Role == Wire.Seeker, cancellationToken),
            Workers: await listings.CountAsync(cancellationToken)
                     + await users.CountAsync(u => u.Role == Wire.Professional && !listings.Any(p => p.Id == u.Uid), cancellationToken),
            NoRole: await users.CountAsync(u => u.Role == null && !listings.Any(p => p.Id == u.Uid), cancellationToken),
            LiveListings: await listings.CountAsync(cancellationToken),
            NewToday: await users.CountAsync(u => u.CreatedAt >= today, cancellationToken),
            New7Days: await users.CountAsync(u => u.CreatedAt >= week, cancellationToken),
            New30Days: await users.CountAsync(u => u.CreatedAt >= month, cancellationToken),
            Active7Days: await users.CountAsync(u => u.LastActiveAt >= week, cancellationToken),
            Active30Days: await users.CountAsync(u => u.LastActiveAt >= month, cancellationToken));

        var posts = db.JobPosts.AsNoTracking();
        var offers = db.JobOffers.AsNoTracking();
        var activity = new AdminActivityStatsDto(
            Posts: await posts.CountAsync(cancellationToken),
            OpenPosts: await posts.CountAsync(p => p.Status == JobPostStatuses.Open, cancellationToken),
            Posts7Days: await posts.CountAsync(p => p.CreatedAt >= week, cancellationToken),
            Offers: await offers.CountAsync(cancellationToken),
            PickedOffers: await offers.CountAsync(o => o.Status == JobOfferStatuses.Picked, cancellationToken),
            CompletedJobs: await offers.CountAsync(o => o.Status == JobOfferStatuses.Picked && o.HouseholdDoneAt != null && o.WorkerDoneAt != null, cancellationToken),
            Chats: await db.Chats.CountAsync(cancellationToken),
            Chats7Days: await db.Chats.CountAsync(c => c.CreatedAt >= week, cancellationToken),
            Messages7Days: await db.ChatMessages.CountAsync(m => m.At >= week, cancellationToken),
            Reviews: await db.Reviews.CountAsync(cancellationToken),
            OtherJobNotes: await db.OtherJobNotes.CountAsync(cancellationToken),
            FeedbackPosts: await db.FeedbackPosts.CountAsync(cancellationToken));

        var phToday = JobPostRules.PhilippineToday(DateTimeOffset.UtcNow);
        var trust = new AdminTrustStatsDto(
            PendingBadges: await db.WorkerVerifications.CountAsync(v => v.Status == VerificationStatuses.Pending, cancellationToken),
            ApprovedBadges: await db.WorkerVerifications.CountAsync(
                v => v.Status == VerificationStatuses.Approved && VerificationKinds.All.Contains(v.Kind)
                    && (v.ValidUntil == null || v.ValidUntil >= phToday), cancellationToken),
            OpenReports: await db.Reports.CountAsync(r => r.Status == "open", cancellationToken),
            Blocks: await db.Blocks.CountAsync(cancellationToken),
            ErrorReports24Hours: await db.ErrorReports.CountAsync(r => r.ReceivedAt >= day, cancellationToken),
            ErrorReports7Days: await db.ErrorReports.CountAsync(r => r.ReceivedAt >= week, cancellationToken));

        var trades = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.LabelEnglish, cancellationToken);
        var towns = await TownLabelsAsync(cancellationToken);

        return new AdminStatsDto(
            now,
            userStats,
            activity,
            trust,
            await SignUpsAsync(now, cancellationToken),
            await TopAsync(listings.GroupBy(p => p.CategoryId).Select(g => new CountLine { Id = g.Key, Count = g.Count() }), trades, cancellationToken),
            await TopAsync(db.ProfessionalCities.AsNoTracking().GroupBy(c => c.CityId).Select(g => new CountLine { Id = g.Key, Count = g.Count() }), towns, cancellationToken),
            await TopAsync(posts.GroupBy(p => p.CategoryId).Select(g => new CountLine { Id = g.Key, Count = g.Count() }), trades, cancellationToken),
            await TopAsync(posts.GroupBy(p => p.CityId).Select(g => new CountLine { Id = g.Key, Count = g.Select(p => p.SeekerUid).Distinct().Count() }), towns, cancellationToken),
            await TopAsync(users.GroupBy(u => u.Language).Select(g => new CountLine { Id = g.Key, Count = g.Count() }), Languages, cancellationToken));
    }

    /// <summary>The last <see cref="SignUpDays"/> Philippine days, oldest first, every day present (0 when nobody signed up).</summary>
    private async Task<List<AdminDayDto>> SignUpsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var lastDay = DateOnly.FromDateTime(now.ToOffset(Manila).Date);
        var firstDay = lastDay.AddDays(-(SignUpDays - 1));
        var from = new DateTimeOffset(firstDay.ToDateTime(TimeOnly.MinValue), Manila);
        var joined = await db.Users.AsNoTracking()
            .Where(u => u.CreatedAt >= from)
            .Select(u => new { u.CreatedAt, u.Role, HasListing = db.Professionals.Any(p => p.Id == u.Uid) })
            .ToListAsync(cancellationToken);
        var byDay = joined.ToLookup(u => DateOnly.FromDateTime(u.CreatedAt.ToOffset(Manila).Date));
        return Enumerable.Range(0, SignUpDays)
            .Select(i => firstDay.AddDays(i))
            .Select(d => new AdminDayDto(
                d,
                byDay[d].Count(u => u.Role == Wire.Seeker),
                byDay[d].Count(u => u.Role == Wire.Professional || (u.Role != Wire.Seeker && u.HasListing)),
                byDay[d].Count(u => u.Role is null && !u.HasListing)))
            .ToList();
    }

    private static readonly Dictionary<string, string> Languages = new()
    {
        [Wire.English] = "English",
        [Wire.Taglish] = "Taglish",
    };

    /// <summary>The <see cref="TopRows"/> biggest counts, biggest first, each with its English label (the id when it has none).</summary>
    private static async Task<List<AdminCountDto>> TopAsync(IQueryable<CountLine> counts, IReadOnlyDictionary<string, string> labels, CancellationToken cancellationToken) =>
        (await counts.ToListAsync(cancellationToken))
            .OrderByDescending(c => c.Count).ThenBy(c => c.Id).Take(TopRows)
            .Select(c => new AdminCountDto(c.Id, labels.TryGetValue(c.Id, out var label) ? label : c.Id, c.Count))
            .ToList();

    private Task<Dictionary<string, string>> TownLabelsAsync(CancellationToken cancellationToken) =>
        db.ConfigOptions.AsNoTracking().Where(o => o.ListId == ConfigStore.Lists.Towns)
            .ToDictionaryAsync(o => o.Id, o => o.LabelEnglish, cancellationToken);

    /// <summary>Every account. <see cref="AdminListQuery.Filter"/>: <c>seeker</c>, <c>professional</c>, <c>none</c> or all.</summary>
    public async Task<AdminUserPageDto> UsersAsync(AdminListQuery query, CancellationToken cancellationToken)
    {
        var rows = db.Users.AsNoTracking().Select(u => new UserLine
        {
            Uid = u.Uid,
            Name = u.DisplayName,
            Phone = u.PhoneNumber,
            Email = u.Email,
            Role = u.Role,
            Language = u.Language,
            HasPhoto = u.ProfilePhotoId != null,
            HasListing = db.Professionals.Any(p => p.Id == u.Uid),
            CreatedAt = u.CreatedAt,
            LastActiveAt = u.LastActiveAt,
        });

        rows = query.Filter switch
        {
            Wire.Seeker => rows.Where(u => u.Role == Wire.Seeker),
            Wire.Professional => rows.Where(u => u.Role == Wire.Professional || u.HasListing),
            AdminLists.NoRole => rows.Where(u => u.Role == null && !u.HasListing),
            _ => rows,
        };

        if (query.Words is { } words)
        {
            rows = rows.Where(u => u.Uid.Contains(words) || (u.Name != null && u.Name.Contains(words))
                                   || (u.Phone != null && u.Phone.Contains(words)) || (u.Email != null && u.Email.Contains(words)));
        }

        rows = (query.Sort, query.Desc) switch
        {
            (AdminLists.SortName, false) => rows.OrderBy(u => u.Name).ThenBy(u => u.Uid),
            (AdminLists.SortName, true) => rows.OrderByDescending(u => u.Name).ThenBy(u => u.Uid),
            (AdminLists.SortActive, false) => rows.OrderBy(u => u.LastActiveAt).ThenBy(u => u.Uid),
            (AdminLists.SortActive, true) => rows.OrderByDescending(u => u.LastActiveAt).ThenBy(u => u.Uid),
            (_, false) => rows.OrderBy(u => u.CreatedAt).ThenBy(u => u.Uid),
            _ => rows.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Uid),
        };

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows.Skip(query.SafeSkip).Take(query.SafeTake).ToListAsync(cancellationToken);
        return new AdminUserPageDto(
            page.Select(u => new AdminUserDto(u.Uid, u.Name, AdminMask.Phone(u.Phone), AdminMask.Email(u.Email), u.Role, u.Language,
                u.HasPhoto, u.HasListing, u.CreatedAt, u.LastActiveAt)).ToList(),
            total);
    }

    /// <summary>The households (<c>Role = seeker</c>) and what each did. <see cref="AdminListQuery.Filter"/> = a town id: households that posted there.</summary>
    public async Task<AdminSeekerPageDto> SeekersAsync(AdminListQuery query, CancellationToken cancellationToken)
    {
        var rows = db.Users.AsNoTracking().Where(u => u.Role == Wire.Seeker).Select(u => new SeekerLine
        {
            Uid = u.Uid,
            Name = u.DisplayName,
            Phone = u.PhoneNumber,
            Language = u.Language,
            TownId = db.JobPosts.Where(p => p.SeekerUid == u.Uid).OrderByDescending(p => p.CreatedAt).Select(p => p.CityId).FirstOrDefault()
                     ?? db.Chats.Where(c => c.SeekerId == u.Uid && c.CityId != null).OrderByDescending(c => c.CreatedAt).Select(c => c.CityId).FirstOrDefault(),
            Posts = db.JobPosts.Count(p => p.SeekerUid == u.Uid),
            OpenPosts = db.JobPosts.Count(p => p.SeekerUid == u.Uid && p.Status == JobPostStatuses.Open),
            Chats = db.Chats.Count(c => c.SeekerId == u.Uid),
            CompletedJobs = db.JobOffers.Count(o => o.Status == JobOfferStatuses.Picked && o.HouseholdDoneAt != null && o.WorkerDoneAt != null
                                                    && db.JobPosts.Any(p => p.Id == o.PostId && p.SeekerUid == u.Uid)),
            ReviewsWritten = db.Reviews.Count(r => r.SeekerId == u.Uid),
            CreatedAt = u.CreatedAt,
            LastActiveAt = u.LastActiveAt,
        });

        if (!string.IsNullOrEmpty(query.Filter))
        {
            rows = rows.Where(s => s.TownId == query.Filter);
        }

        if (query.Words is { } words)
        {
            rows = rows.Where(s => s.Uid.Contains(words) || (s.Name != null && s.Name.Contains(words)) || (s.Phone != null && s.Phone.Contains(words)));
        }

        rows = (query.Sort, query.Desc) switch
        {
            (AdminLists.SortName, false) => rows.OrderBy(s => s.Name).ThenBy(s => s.Uid),
            (AdminLists.SortName, true) => rows.OrderByDescending(s => s.Name).ThenBy(s => s.Uid),
            (AdminLists.SortPosts, false) => rows.OrderBy(s => s.Posts).ThenBy(s => s.Uid),
            (AdminLists.SortPosts, true) => rows.OrderByDescending(s => s.Posts).ThenBy(s => s.Uid),
            (AdminLists.SortCompleted, false) => rows.OrderBy(s => s.CompletedJobs).ThenBy(s => s.Uid),
            (AdminLists.SortCompleted, true) => rows.OrderByDescending(s => s.CompletedJobs).ThenBy(s => s.Uid),
            (AdminLists.SortActive, false) => rows.OrderBy(s => s.LastActiveAt).ThenBy(s => s.Uid),
            (AdminLists.SortActive, true) => rows.OrderByDescending(s => s.LastActiveAt).ThenBy(s => s.Uid),
            (_, false) => rows.OrderBy(s => s.CreatedAt).ThenBy(s => s.Uid),
            _ => rows.OrderByDescending(s => s.CreatedAt).ThenBy(s => s.Uid),
        };

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows.Skip(query.SafeSkip).Take(query.SafeTake).ToListAsync(cancellationToken);
        var towns = await TownLabelsAsync(cancellationToken);
        return new AdminSeekerPageDto(
            page.Select(s => new AdminSeekerDto(s.Uid, s.Name, AdminMask.Phone(s.Phone), s.Language, s.TownId,
                s.TownId is { } town ? towns.GetValueOrDefault(town, town) : null,
                s.Posts, s.OpenPosts, s.Chats, s.CompletedJobs, s.ReviewsWritten, s.CreatedAt, s.LastActiveAt)).ToList(),
            total);
    }

    /// <summary>
    /// The skilled workers: every live listing (its account's profile may not be saved), then the worker-side accounts with no listing yet. <see cref="AdminListQuery.Filter"/>:
    /// a trade id, <c>live</c> or <c>not-live</c>.
    /// </summary>
    public async Task<AdminWorkerPageDto> WorkersAsync(AdminListQuery query, CancellationToken cancellationToken)
    {
        var today = JobPostRules.PhilippineToday(DateTimeOffset.UtcNow); // a lapsed clearance is no badge

        // Live listings (with the account's profile when it was saved), then worker-side accounts with no listing yet.
        var live =
            from p in db.Professionals.AsNoTracking()
            join u in db.Users.AsNoTracking() on p.Id equals u.Uid into account
            from u in account.DefaultIfEmpty()
            select new WorkerLine
            {
                Uid = p.Id,
                Name = p.DisplayName,
                Phone = p.MobileNumber,
                TradeId = p.CategoryId,
                OtherWork = p.OtherWorkText,
                RatePesos = p.RatePesos,
                ExperienceId = p.ExperienceId,
                Live = true,
                HasPhoto = p.ProfilePhotoId != null,
                CreatedAt = u != null ? u.CreatedAt : p.PublishedAt,
                PublishedAt = p.PublishedAt,
                LastActiveAt = u != null ? u.LastActiveAt : null,
            };
        var notLive = db.Users.AsNoTracking()
            .Where(u => u.Role == Wire.Professional && !db.Professionals.Any(p => p.Id == u.Uid))
            .Select(u => new WorkerLine
            {
                Uid = u.Uid,
                Name = u.DisplayName,
                Phone = u.PhoneNumber,
                TradeId = null,
                OtherWork = null,
                RatePesos = 0,
                ExperienceId = null,
                Live = false,
                HasPhoto = u.ProfilePhotoId != null,
                CreatedAt = u.CreatedAt,
                PublishedAt = null,
                LastActiveAt = u.LastActiveAt,
            });
        var rows = live.Concat(notLive).Select(w => new WorkerLine
        {
            Uid = w.Uid,
            Name = w.Name,
            Phone = w.Phone,
            TradeId = w.TradeId,
            OtherWork = w.OtherWork,
            RatePesos = w.RatePesos,
            ExperienceId = w.ExperienceId,
            Live = w.Live,
            HasPhoto = w.HasPhoto,
            WorkPhotos = db.ProfessionalWorkPhotos.Count(x => x.ProfessionalId == w.Uid),
            Badges = db.WorkerVerifications.Count(v => v.Uid == w.Uid && v.Status == VerificationStatuses.Approved && VerificationKinds.All.Contains(v.Kind)
                && (v.ValidUntil == null || v.ValidUntil >= today)),
            PendingBadges = db.WorkerVerifications.Count(v => v.Uid == w.Uid && v.Status == VerificationStatuses.Pending),
            Reviews = db.Reviews.Count(r => r.ProfessionalId == w.Uid),
            Rating = db.Reviews.Where(r => r.ProfessionalId == w.Uid).Average(r => (double?)r.Rating),
            Chats = db.Chats.Count(c => c.ProfessionalId == w.Uid),
            Offers = db.JobOffers.Count(o => o.ProUid == w.Uid),
            CompletedJobs = db.JobOffers.Count(o => o.ProUid == w.Uid && o.Status == JobOfferStatuses.Picked && o.HouseholdDoneAt != null && o.WorkerDoneAt != null),
            CreatedAt = w.CreatedAt,
            PublishedAt = w.PublishedAt,
            LastActiveAt = w.LastActiveAt,
        });

        rows = query.Filter switch
        {
            null or "" => rows,
            "live" => rows.Where(w => w.Live),
            "not-live" => rows.Where(w => !w.Live),
            var trade => rows.Where(w => w.TradeId == trade),
        };

        if (query.Words is { } words)
        {
            rows = rows.Where(w => w.Uid.Contains(words) || (w.Name != null && w.Name.Contains(words)) || (w.Phone != null && w.Phone.Contains(words))
                                   || (w.OtherWork != null && w.OtherWork.Contains(words)));
        }

        rows = (query.Sort, query.Desc) switch
        {
            (AdminLists.SortName, false) => rows.OrderBy(w => w.Name).ThenBy(w => w.Uid),
            (AdminLists.SortName, true) => rows.OrderByDescending(w => w.Name).ThenBy(w => w.Uid),
            (AdminLists.SortRating, false) => rows.OrderBy(w => w.Rating).ThenBy(w => w.Reviews).ThenBy(w => w.Uid),
            (AdminLists.SortRating, true) => rows.OrderByDescending(w => w.Rating).ThenByDescending(w => w.Reviews).ThenBy(w => w.Uid),
            (AdminLists.SortCompleted, false) => rows.OrderBy(w => w.CompletedJobs).ThenBy(w => w.Uid),
            (AdminLists.SortCompleted, true) => rows.OrderByDescending(w => w.CompletedJobs).ThenBy(w => w.Uid),
            (AdminLists.SortRate, false) => rows.OrderBy(w => w.RatePesos).ThenBy(w => w.Uid),
            (AdminLists.SortRate, true) => rows.OrderByDescending(w => w.RatePesos).ThenBy(w => w.Uid),
            (AdminLists.SortActive, false) => rows.OrderBy(w => w.LastActiveAt).ThenBy(w => w.Uid),
            (AdminLists.SortActive, true) => rows.OrderByDescending(w => w.LastActiveAt).ThenBy(w => w.Uid),
            (_, false) => rows.OrderBy(w => w.CreatedAt).ThenBy(w => w.Uid),
            _ => rows.OrderByDescending(w => w.CreatedAt).ThenBy(w => w.Uid),
        };

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows.Skip(query.SafeSkip).Take(query.SafeTake).ToListAsync(cancellationToken);

        var uids = page.Select(w => w.Uid).ToList();
        var townIds = await db.ProfessionalCities.AsNoTracking().Where(c => uids.Contains(c.ProfessionalId))
            .Select(c => new { c.ProfessionalId, c.CityId }).ToListAsync(cancellationToken);
        var trades = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.LabelEnglish, cancellationToken);
        var towns = await TownLabelsAsync(cancellationToken);
        var townsOf = townIds.ToLookup(c => c.ProfessionalId, c => towns.GetValueOrDefault(c.CityId, c.CityId));

        return new AdminWorkerPageDto(
            page.Select(w => new AdminWorkerDto(
                w.Uid, w.Name, AdminMask.Phone(w.Phone), w.TradeId,
                w.TradeId is { } trade ? TradeLabel(trades, trade) : null,
                w.OtherWork, townsOf[w.Uid].Any() ? string.Join(", ", townsOf[w.Uid].Order()) : null,
                w.RatePesos, w.ExperienceId, w.Live, w.HasPhoto, w.WorkPhotos, w.Badges, w.PendingBadges, w.Reviews,
                w.Rating is { } rating ? Math.Round(rating, 2) : null,
                w.Chats, w.Offers, w.CompletedJobs, w.CreatedAt, w.PublishedAt, w.LastActiveAt)).ToList(),
            total);
    }

    /// <summary>
    /// The error reports the apps sent, newest first by default. <see cref="AdminListQuery.Filter"/>: an error kind
    /// (<see cref="ErrorKinds"/>) or a channel (<see cref="AppChannels"/>). Search looks in the message, source, route and version.
    /// </summary>
    public async Task<AdminErrorReportPageDto> ErrorReportsAsync(AdminListQuery query, CancellationToken cancellationToken)
    {
        var rows = db.ErrorReports.AsNoTracking();
        if (!string.IsNullOrEmpty(query.Filter))
        {
            rows = AppChannels.IsKnown(query.Filter)
                ? rows.Where(r => r.Channel == query.Filter)
                : rows.Where(r => r.Kind == query.Filter);
        }

        if (query.Words is { } words)
        {
            rows = rows.Where(r => r.Message.Contains(words) || r.Source.Contains(words) || (r.Route != null && r.Route.Contains(words))
                                   || r.AppVersion.Contains(words) || r.InstallId.StartsWith(words));
        }

        rows = (query.Sort, query.Desc) switch
        {
            (AdminLists.SortCount, false) => rows.OrderBy(r => r.Count).ThenBy(r => r.Id),
            (AdminLists.SortCount, true) => rows.OrderByDescending(r => r.Count).ThenByDescending(r => r.Id),
            (AdminLists.SortLast, false) => rows.OrderBy(r => r.LastAt).ThenBy(r => r.Id),
            (AdminLists.SortLast, true) => rows.OrderByDescending(r => r.LastAt).ThenByDescending(r => r.Id),
            (_, false) => rows.OrderBy(r => r.Id),
            _ => rows.OrderByDescending(r => r.Id),
        };

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows.Skip(query.SafeSkip).Take(query.SafeTake).ToListAsync(cancellationToken);
        return new AdminErrorReportPageDto(
            page.Select(r => new AdminErrorReportDto(r.Id, r.Kind, r.Source, r.Message, r.Detail, r.Route, r.Trail, r.Count,
                r.AppVersion, r.CodeVersion, r.Channel, r.Platform, r.Device, AdminMask.Install(r.InstallId),
                r.FirstAt, r.LastAt, r.ReceivedAt)).ToList(),
            total);
    }

    /// <summary>
    /// What people typed after picking "Others" (<c>OtherJobNotes</c>), newest first by default, with the writer's name
    /// when their account still exists. <see cref="AdminListQuery.Filter"/>: a side (<c>seeker</c> / <c>professional</c>)
    /// or a trade id (<see cref="OtherWork.TradeId"/> = typed as their own trade). Search looks in the text, the uid and the name.
    /// </summary>
    public async Task<AdminOtherJobPageDto> OtherJobsAsync(AdminListQuery query, CancellationToken cancellationToken)
    {
        var rows =
            from n in db.OtherJobNotes.AsNoTracking()
            join u in db.Users.AsNoTracking() on n.Uid equals u.Uid into account
            from u in account.DefaultIfEmpty()
            select new OtherJobLine
            {
                Id = n.Id,
                Side = n.Side,
                TradeId = n.CategoryId,
                Text = n.Text,
                Uid = n.Uid,
                Name = u != null ? u.DisplayName : null,
                CreatedAt = n.CreatedAt,
            };

        rows = query.Filter switch
        {
            null or "" => rows,
            Wire.Seeker or Wire.Professional => rows.Where(n => n.Side == query.Filter),
            var trade => rows.Where(n => n.TradeId == trade),
        };

        if (query.Words is { } words)
        {
            rows = rows.Where(n => n.Text.Contains(words) || (n.Uid != null && n.Uid.Contains(words)) || (n.Name != null && n.Name.Contains(words)));
        }

        rows = query.Desc ? rows.OrderByDescending(n => n.Id) : rows.OrderBy(n => n.Id);

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows.Skip(query.SafeSkip).Take(query.SafeTake).ToListAsync(cancellationToken);
        var trades = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.LabelEnglish, cancellationToken);
        return new AdminOtherJobPageDto(
            page.Select(n => new AdminOtherJobDto(n.Id, n.Side, n.TradeId, TradeLabel(trades, n.TradeId), n.Text, n.Uid, n.Name, n.CreatedAt)).ToList(),
            total);
    }

    /// <summary>A trade's English label; "Others" for the interviews' own "Others" tile, the id when it has none.</summary>
    private static string TradeLabel(IReadOnlyDictionary<string, string> trades, string id) =>
        trades.GetValueOrDefault(id, id == OtherWork.TradeId ? "Others" : id);

    // Projections EF can sort and filter on (member init, not constructors).
    private sealed class CountLine
    {
        public required string Id { get; init; }
        public int Count { get; init; }
    }

    private sealed class OtherJobLine
    {
        public long Id { get; init; }
        public required string Side { get; init; }
        public required string TradeId { get; init; }
        public required string Text { get; init; }
        public string? Uid { get; init; }
        public string? Name { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }

    private sealed class UserLine
    {
        public required string Uid { get; init; }
        public string? Name { get; init; }
        public string? Phone { get; init; }
        public string? Email { get; init; }
        public string? Role { get; init; }
        public required string Language { get; init; }
        public bool HasPhoto { get; init; }
        public bool HasListing { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? LastActiveAt { get; init; }
    }

    private sealed class SeekerLine
    {
        public required string Uid { get; init; }
        public string? Name { get; init; }
        public string? Phone { get; init; }
        public required string Language { get; init; }
        public string? TownId { get; init; }
        public int Posts { get; init; }
        public int OpenPosts { get; init; }
        public int Chats { get; init; }
        public int CompletedJobs { get; init; }
        public int ReviewsWritten { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? LastActiveAt { get; init; }
    }

    private sealed class WorkerLine
    {
        public required string Uid { get; init; }
        public string? Name { get; init; }
        public string? Phone { get; init; }
        public string? TradeId { get; init; }
        public string? OtherWork { get; init; }
        public int RatePesos { get; init; }
        public string? ExperienceId { get; init; }
        public bool Live { get; init; }
        public bool HasPhoto { get; init; }
        public int WorkPhotos { get; init; }
        public int Badges { get; init; }
        public int PendingBadges { get; init; }
        public int Reviews { get; init; }
        public double? Rating { get; init; }
        public int Chats { get; init; }
        public int Offers { get; init; }
        public int CompletedJobs { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? PublishedAt { get; init; }
        public DateTimeOffset? LastActiveAt { get; init; }
    }
}
