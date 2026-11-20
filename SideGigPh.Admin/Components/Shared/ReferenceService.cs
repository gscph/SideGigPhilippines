using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Shared;

/// <summary>
/// The trades, towns and experience answers the app shows (the public <c>GET config</c>, the same one the phones read),
/// loaded once per visit for the lists' filters and labels. Empty lists when it can't be read: filters then offer "All" only.
/// </summary>
public sealed class ReferenceService(AdminApi api)
{
    private Task<ConfigDto?>? _config;

    public async Task<IReadOnlyList<OptionDto>> TradesAsync() =>
        (await ConfigAsync())?.Catalog.Select(c => new OptionDto(c.Id, c.English, c.Taglish, null, 0)).ToList() ?? [];

    public async Task<IReadOnlyList<OptionDto>> TownsAsync() => (await ConfigAsync())?.Towns ?? [];

    /// <summary>"3–10 years" for <c>3-10</c>; the id itself when unknown, a dash for none.</summary>
    public async Task<IReadOnlyDictionary<string, string>> ExperienceAsync() =>
        (await ConfigAsync())?.Experience.ToDictionary(e => e.Id, e => e.English) ?? [];

    private Task<ConfigDto?> ConfigAsync() => _config ??= LoadAsync();

    private async Task<ConfigDto?> LoadAsync()
    {
        try
        {
            return await api.GetAsync("config", ApiJson.Default.ConfigDto);
        }
        catch (AdminApiException)
        {
            _config = null; // try again next time
            return null;
        }
    }
}
