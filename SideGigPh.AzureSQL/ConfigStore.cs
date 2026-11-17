using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

/// <summary>
/// The app's config (<c>GET config</c>): the trades and jobs (<c>Categories</c>, <c>Services</c>), the barangays we
/// serve per town (<c>Barangays</c>) and the other answer lists (<c>ConfigOptions</c>), all filled by <c>004-config.sql</c>. It changes rarely and may be stale, so each API
/// instance keeps it for <see cref="ConfigFor"/> (restart the Function App to show an edit sooner).
/// </summary>
public sealed class ConfigStore(SideGigPhDb db, IMemoryCache cache)
{
    /// <summary>How long an API instance (and, through <c>Cache-Control</c>, any HTTP cache) keeps the config.</summary>
    public static readonly TimeSpan ConfigFor = TimeSpan.FromHours(12);

    /// <summary>The <c>ConfigOptions.ListId</c> of each list in <see cref="ConfigDto"/>.</summary>
    public static class Lists
    {
        public const string Towns = "towns";
        public const string PriceTiers = "price-tiers";
        public const string Experience = "experience";
        public const string Urgencies = "urgencies";
        public const string IncomeGoals = "income-goals";

        /// <summary>One row: the NCR daily minimum wage (its Amount), the floor of every job's price.</summary>
        public const string MinimumWage = "minimum-wage";

        /// <summary>One row: its Id is the launch's town (a <see cref="Towns"/> id), pre-selected wherever a town is picked.</summary>
        public const string DefaultTown = "default-town";
    }

    private const string CacheKey = "config";

    /// <summary>Every active list in display order, with its version (the HTTP ETag: a hash of the JSON, so it changes only when the content does).</summary>
    public async Task<ConfigSnapshot> GetAsync(CancellationToken cancellationToken) =>
        (await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ConfigFor;
            var config = await ReadAsync(cancellationToken);
            var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(config, ApiJson.Default.ConfigDto));
            return new ConfigSnapshot(config, $"\"{Convert.ToHexStringLower(hash)[..32]}\"");
        }))!;

    private async Task<ConfigDto> ReadAsync(CancellationToken cancellationToken)
    {
        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);
        var services = (await db.Services.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
                .ToListAsync(cancellationToken))
            .ToLookup(s => s.CategoryId);
        var options = (await db.ConfigOptions.AsNoTracking()
                .Where(o => o.IsActive)
                .OrderBy(o => o.SortOrder).ThenBy(o => o.Id)
                .ToListAsync(cancellationToken))
            .ToLookup(o => o.ListId);
        var towns = List(Lists.Towns);
        var barangays = await db.Barangays.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.CityId).ThenBy(b => b.SortOrder).ThenBy(b => b.Id)
            .Select(b => new BarangayDto(b.Id, b.CityId, b.LabelEnglish, b.LabelTaglish))
            .ToListAsync(cancellationToken);

        List<OptionDto> List(string listId) =>
            options[listId].Select(o => new OptionDto(o.Id, o.LabelEnglish, o.LabelTaglish, o.Icon, o.Amount)).ToList();

        return new ConfigDto(
            categories
                .Select(c => new CategoryDto(c.Id, c.LabelEnglish, c.LabelTaglish, c.Icon, c.HintEnglish, c.HintTaglish,
                    services[c.Id].Select(s => new ServiceDto(s.Id, s.LabelEnglish, s.LabelTaglish, s.Icon, Price(s))).ToList()))
                .ToList(),
            towns,
            List(Lists.PriceTiers),
            List(Lists.Experience),
            List(Lists.Urgencies),
            List(Lists.IncomeGoals),
            options[Lists.MinimumWage].Select(o => o.Amount).FirstOrDefault(),
            // Only a town we serve can be the default; barangays of a town we don't serve are left out.
            options[Lists.DefaultTown].Select(o => o.Id).FirstOrDefault(id => towns.Any(t => t.Id == id)),
            [.. barangays.Where(b => towns.Any(t => t.Id == b.TownId))]);
    }

    private static PriceGuideDto? Price(ServiceRow s) =>
        s is { PriceMin: { } min, PriceAverage: { } average, PriceHigh: { } high } ? new PriceGuideDto(min, average, high) : null;
}

/// <summary>The config and its <paramref name="Version"/>: a quoted ETag, e.g. <c>"3f9a…"</c>.</summary>
public sealed record ConfigSnapshot(ConfigDto Config, string Version);
