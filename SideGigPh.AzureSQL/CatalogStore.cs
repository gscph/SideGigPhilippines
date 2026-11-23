using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>What happened to an operator's change to a trade or job.</summary>
public enum CatalogOutcome
{
    Saved,

    /// <summary>No trade or job with that id.</summary>
    NotFound,

    /// <summary>A name, hint or price isn't valid (<see cref="CatalogResult.Field"/> says which).</summary>
    Invalid,

    /// <summary>A trade's "Others" job can be renamed, never retired: picking it is how a job we don't list still finishes.</summary>
    OthersStays,

    /// <summary>A retired job with no price guide can't be offered again until it gets one (the app refuses a config without).</summary>
    NeedsPrice,
}

/// <param name="Field">For <see cref="CatalogOutcome.Invalid"/>: the request's field that isn't valid (its JSON name).</param>
public sealed record CatalogResult(CatalogOutcome Outcome, string? Field = null)
{
    public static readonly CatalogResult Saved = new(CatalogOutcome.Saved);
    public static readonly CatalogResult NotFound = new(CatalogOutcome.NotFound);
}

/// <summary>
/// The operator site's Catalog page (<c>ops/catalog…</c>): the trades (<c>Categories</c>) and jobs (<c>Services</c>)
/// the app offers. An operator renames, adds, retires and restores them; ids are made here once and never change, and
/// nothing is ever deleted (posts, chats, listings and saved answers point at the ids). Every change stamps
/// <c>EditedAt</c> / <c>EditedBy</c>, so <c>004-config.sql</c> leaves that row as the operator saved it, and clears this
/// instance's cached config so <c>GET config</c> shows it at once (other API instances within <see cref="ConfigStore.ConfigFor"/>).
/// Posts and chats keep the names they were made with (<see cref="CatalogNames"/>): a rename here never rewrites them.
/// </summary>
public sealed class CatalogStore(SideGigPhDb db, ConfigStore config)
{
    /// <summary>The name of every new trade's "Others" job, like the ones in <c>004-config.sql</c>.</summary>
    public const string OthersEnglish = "Others";
    public const string OthersTaglish = "Iba pa";

    /// <summary>A trade's id is at most this long, so its "Others" job's id (<c>{id}-other</c>) fits the 40-character column.</summary>
    private const int MaxTradeId = 34;

    private const int MaxId = 40;
    private const string OtherSuffix = "-other";

    /// <summary>Every trade, retired ones too, active first in the app's order, each with its jobs and how much uses them.</summary>
    public async Task<AdminCatalogDto> ReadAsync(CancellationToken cancellationToken)
    {
        var trades = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var services = (await db.Services.AsNoTracking().ToListAsync(cancellationToken)).ToLookup(s => s.CategoryId);
        var workersByTrade = await db.Professionals.AsNoTracking()
            .GroupBy(p => p.CategoryId).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        var workersByService = await db.ProfessionalServices.AsNoTracking()
            .GroupBy(s => s.ServiceId).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        var openPosts = await db.JobPosts.AsNoTracking()
            .Where(p => p.Status == JobPostStatuses.Open)
            .GroupBy(p => new { p.CategoryId, p.ServiceId }).Select(g => new { g.Key.CategoryId, g.Key.ServiceId, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new AdminCatalogDto([.. trades
            .OrderByDescending(c => c.IsActive).ThenBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new AdminTradeDto(
                c.Id, c.LabelEnglish, c.LabelTaglish, c.HintEnglish, c.HintTaglish, c.SortOrder, c.IsActive,
                workersByTrade.GetValueOrDefault(c.Id),
                openPosts.Where(p => p.CategoryId == c.Id).Sum(p => p.Count),
                c.EditedAt,
                [.. services[c.Id]
                    .OrderByDescending(s => s.IsActive).ThenBy(s => s.SortOrder).ThenBy(s => s.Id)
                    .Select(s => new AdminServiceDto(
                        s.Id, s.CategoryId, s.LabelEnglish, s.LabelTaglish, s.SortOrder, s.IsActive, OtherWork.IsOther(s.Id),
                        s is { PriceMin: { } min, PriceAverage: { } average, PriceHigh: { } high } ? new PriceGuideDto(min, average, high) : null,
                        workersByService.GetValueOrDefault(s.Id),
                        openPosts.Where(p => p.ServiceId == s.Id).Sum(p => p.Count),
                        s.EditedAt))]))]);
    }

    /// <summary>A new trade, last in the app's order, with its "Others" job. Its id comes from the English name.</summary>
    public async Task<CatalogResult> AddTradeAsync(string admin, SaveTradeRequest request, CancellationToken cancellationToken)
    {
        if (InvalidTrade(request) is { } field)
        {
            return new CatalogResult(CatalogOutcome.Invalid, field);
        }

        // A trade's id is taken when a trade has it, or when its "Others" job's id ({id}-other) is already a job's
        // (some older trades' "Others" ids don't match the trade's: solar-other is solar_cleaning's).
        var taken = (await db.Categories.Select(c => c.Id).ToListAsync(cancellationToken)).ToHashSet();
        taken.UnionWith((await db.Services.Where(s => s.Id.EndsWith(OtherSuffix)).Select(s => s.Id).ToListAsync(cancellationToken))
            .Select(id => id[..^OtherSuffix.Length]));
        taken.Add(OtherWork.TradeId);
        var id = NewId(Slug(request.English, MaxTradeId), MaxTradeId, taken);
        var now = DateTimeOffset.UtcNow;
        var sort = (await db.Categories.MaxAsync(c => (int?)c.SortOrder, cancellationToken) ?? 0) + 1;
        db.Categories.Add(new CategoryRow
        {
            Id = id,
            LabelEnglish = request.English.Trim(),
            LabelTaglish = request.Taglish.Trim(),
            HintEnglish = Hint(request.HintEnglish),
            HintTaglish = Hint(request.HintTaglish),
            SortOrder = sort,
            IsActive = true,
            EditedAt = now,
            EditedBy = admin,
        });
        db.Services.Add(new ServiceRow
        {
            Id = id + OtherSuffix,
            CategoryId = id,
            LabelEnglish = OthersEnglish,
            LabelTaglish = OthersTaglish,
            SortOrder = 99,
            IsActive = true,
            PriceMin = CatalogRules.OthersPrice.Min,
            PriceAverage = CatalogRules.OthersPrice.Average,
            PriceHigh = CatalogRules.OthersPrice.High,
            EditedAt = now,
            EditedBy = admin,
        });
        return await SaveAsync(cancellationToken);
    }

    /// <summary>Renames a trade (and its hint). Its id, order and jobs stay.</summary>
    public async Task<CatalogResult> UpdateTradeAsync(string admin, string id, SaveTradeRequest request, CancellationToken cancellationToken)
    {
        if (InvalidTrade(request) is { } field)
        {
            return new CatalogResult(CatalogOutcome.Invalid, field);
        }

        if (await db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken) is not { } row)
        {
            return CatalogResult.NotFound;
        }

        row.LabelEnglish = request.English.Trim();
        row.LabelTaglish = request.Taglish.Trim();
        row.HintEnglish = Hint(request.HintEnglish);
        row.HintTaglish = Hint(request.HintTaglish);
        Stamp(row, admin);
        return await SaveAsync(cancellationToken);
    }

    /// <summary>
    /// Retires (<paramref name="active"/> false) or restores a trade. Its jobs keep their own state, except that a
    /// restored trade always gets its "Others" job back (with the default guide when it has none) and a job of it with
    /// no guide stays retired: the app refuses a trade with no jobs, or a job with no price guide.
    /// </summary>
    public async Task<CatalogResult> SetTradeActiveAsync(string admin, string id, bool active, CancellationToken cancellationToken)
    {
        if (await db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken) is not { } row)
        {
            return CatalogResult.NotFound;
        }

        row.IsActive = active;
        Stamp(row, admin);
        if (active)
        {
            var jobs = await db.Services.Where(s => s.CategoryId == id).ToListAsync(cancellationToken);
            foreach (var others in jobs.Where(s => OtherWork.IsOther(s.Id)))
            {
                others.IsActive = true;
                if (!HasPrice(others))
                {
                    (others.PriceMin, others.PriceAverage, others.PriceHigh) =
                        (CatalogRules.OthersPrice.Min, CatalogRules.OthersPrice.Average, CatalogRules.OthersPrice.High);
                }

                Stamp(others, admin);
            }

            foreach (var unpriced in jobs.Where(s => s.IsActive && !OtherWork.IsOther(s.Id) && !HasPrice(s)))
            {
                unpriced.IsActive = false;
                Stamp(unpriced, admin);
            }
        }

        return await SaveAsync(cancellationToken);
    }

    /// <summary>A new job in a trade, just before its "Others" job (which stays last). Its id is the trade's plus the English name.</summary>
    public async Task<CatalogResult> AddServiceAsync(string admin, SaveServiceRequest request, CancellationToken cancellationToken)
    {
        if (InvalidService(request) is { } field)
        {
            return new CatalogResult(CatalogOutcome.Invalid, field);
        }

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return new CatalogResult(CatalogOutcome.Invalid, "categoryId");
        }

        var siblings = await db.Services.Where(s => s.CategoryId == request.CategoryId).ToListAsync(cancellationToken);
        var taken = (await db.Services.Select(s => s.Id).ToListAsync(cancellationToken)).ToHashSet();
        var prefix = request.CategoryId + "-";
        var id = NewId(prefix + Slug(request.English, MaxId - prefix.Length), MaxId, taken);
        var now = DateTimeOffset.UtcNow;
        var sort = (siblings.Where(s => !OtherWork.IsOther(s.Id)).Max(s => (int?)s.SortOrder) ?? 0) + 1;
        foreach (var others in siblings.Where(s => OtherWork.IsOther(s.Id) && s.SortOrder <= sort))
        {
            others.SortOrder = sort + 1;
            Stamp(others, admin);
        }

        db.Services.Add(new ServiceRow
        {
            Id = id,
            CategoryId = request.CategoryId,
            LabelEnglish = request.English.Trim(),
            LabelTaglish = request.Taglish.Trim(),
            SortOrder = sort,
            IsActive = true,
            PriceMin = request.Price?.Min,
            PriceAverage = request.Price?.Average,
            PriceHigh = request.Price?.High,
            EditedAt = now,
            EditedBy = admin,
        });
        return await SaveAsync(cancellationToken);
    }

    /// <summary>Renames a job and sets its price guide. Its id and trade stay.</summary>
    public async Task<CatalogResult> UpdateServiceAsync(string admin, string id, SaveServiceRequest request, CancellationToken cancellationToken)
    {
        if (InvalidService(request) is { } field)
        {
            return new CatalogResult(CatalogOutcome.Invalid, field);
        }

        if (await db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken) is not { } row)
        {
            return CatalogResult.NotFound;
        }

        row.LabelEnglish = request.English.Trim();
        row.LabelTaglish = request.Taglish.Trim();
        row.PriceMin = request.Price?.Min;
        row.PriceAverage = request.Price?.Average;
        row.PriceHigh = request.Price?.High;
        Stamp(row, admin);
        return await SaveAsync(cancellationToken);
    }

    /// <summary>Retires (<paramref name="active"/> false) or restores a job. A trade's "Others" job is never retired.</summary>
    public async Task<CatalogResult> SetServiceActiveAsync(string admin, string id, bool active, CancellationToken cancellationToken)
    {
        if (await db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken) is not { } row)
        {
            return CatalogResult.NotFound;
        }

        if (!active && OtherWork.IsOther(row.Id))
        {
            return new CatalogResult(CatalogOutcome.OthersStays);
        }

        if (active && !HasPrice(row))
        {
            return new CatalogResult(CatalogOutcome.NeedsPrice);
        }

        row.IsActive = active;
        Stamp(row, admin);
        return await SaveAsync(cancellationToken);
    }

    /// <summary>
    /// The id for a new trade or job: <paramref name="candidate"/>, or with <c>-2</c>, <c>-3</c>… when it is taken or
    /// would read as an "Others" id (<see cref="OtherWork.IsOther"/>), cut to <paramref name="max"/> characters.
    /// </summary>
    internal static string NewId(string candidate, int max, IReadOnlySet<string> taken)
    {
        for (var n = 1; ; n++)
        {
            var suffix = n == 1 ? "" : $"-{n}";
            var id = candidate[..Math.Min(candidate.Length, max - suffix.Length)].TrimEnd('-') + suffix;
            if (!taken.Contains(id) && !OtherWork.IsOther(id) && StoreText.IsCode(id))
            {
                return id;
            }
        }
    }

    /// <summary>"Tile & Grout Work" → <c>tile-grout-work</c>: lower-case letters and digits, accents dropped, words joined by <c>-</c>.</summary>
    internal static string Slug(string name, int max)
    {
        var plain = new StringBuilder();
        foreach (var c in name.Trim().Normalize(NormalizationForm.FormD).ToLowerInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            plain.Append(c is (>= 'a' and <= 'z') or (>= '0' and <= '9') ? c : '-');
        }

        var slug = string.Join('-', plain.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        slug = slug[..Math.Min(slug.Length, max)].TrimEnd('-');
        return slug.Length > 0 ? slug : "new";
    }

    private static string? InvalidTrade(SaveTradeRequest request) =>
        !CatalogRules.IsTradeName(request.English) ? "english"
        : !CatalogRules.IsTradeName(request.Taglish) ? "taglish"
        : !CatalogRules.IsHint(request.HintEnglish) ? "hintEnglish"
        : !CatalogRules.IsHint(request.HintTaglish) ? "hintTaglish"
        : null;

    private static string? InvalidService(SaveServiceRequest request) =>
        !CatalogRules.IsServiceName(request.English) ? "english"
        : !CatalogRules.IsServiceName(request.Taglish) ? "taglish"
        : !CatalogRules.IsPrice(request.Price) ? "price"
        : null;

    private static bool HasPrice(ServiceRow s) => s is { PriceMin: not null, PriceAverage: not null, PriceHigh: not null };

    private static string? Hint(string? hint) => string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();

    private static void Stamp(CategoryRow row, string admin)
    {
        row.EditedAt = DateTimeOffset.UtcNow;
        row.EditedBy = admin;
    }

    private static void Stamp(ServiceRow row, string admin)
    {
        row.EditedAt = DateTimeOffset.UtcNow;
        row.EditedBy = admin;
    }

    private async Task<CatalogResult> SaveAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        config.Forget();
        return CatalogResult.Saved;
    }
}

/// <summary>
/// The trade's and job's names as they are now, copied onto a post or chat when it is made (<c>029-schema-catalog-names.sql</c>),
/// like an order line keeping a product's title. Null for "Others" (named by the app's code) or an unknown id.
/// </summary>
internal static class CatalogNames
{
    public static Task<CatalogNameDto?> TradeAsync(this SideGigPhDb db, string? id, CancellationToken cancellationToken) =>
        db.Categories.AsNoTracking().Where(c => c.Id == id)
            .Select(c => new CatalogNameDto(c.LabelEnglish, c.LabelTaglish)).FirstOrDefaultAsync(cancellationToken);

    public static Task<CatalogNameDto?> ServiceAsync(this SideGigPhDb db, string? id, CancellationToken cancellationToken) =>
        db.Services.AsNoTracking().Where(s => s.Id == id)
            .Select(s => new CatalogNameDto(s.LabelEnglish, s.LabelTaglish)).FirstOrDefaultAsync(cancellationToken);

    /// <summary>A copied name from its two columns, or null when there is none.</summary>
    public static CatalogNameDto? Of(string? english, string? taglish) =>
        english is null || taglish is null ? null : new CatalogNameDto(english, taglish);
}
