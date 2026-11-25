using SideGigPh.Admin.Components.Shared;
using SideGigPh.Admin.Services;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Catalog;

/// <summary>
/// The trades and jobs the app offers (<c>ops/catalog</c>): add, rename, retire and restore. Every change answers with
/// the whole catalog, kept here as <see cref="Current"/>. Nothing is ever deleted: ids stay, and posts and chats keep
/// the names they were made with, so a rename only changes what people see from now on. Phones pick the change up when
/// their copy of the config is refreshed (at most 12 hours).
/// </summary>
public sealed class CatalogService(AdminApi api, ReferenceService reference)
{
    private const string Path = "ops/catalog";

    public AdminCatalogDto? Current { get; private set; }

    public async Task LoadAsync() => Keep(await api.GetAsync(Path, ApiJson.Default.AdminCatalogDto));

    public async Task AddTradeAsync(SaveTradeRequest request) =>
        Keep(await api.SendAsync(HttpMethod.Post, $"{Path}/trades", request, ApiJson.Default.SaveTradeRequest, ApiJson.Default.AdminCatalogDto));

    public async Task UpdateTradeAsync(string id, SaveTradeRequest request) =>
        Keep(await api.SendAsync(HttpMethod.Put, $"{Path}/trades/{Uri.EscapeDataString(id)}", request, ApiJson.Default.SaveTradeRequest, ApiJson.Default.AdminCatalogDto));

    /// <summary>Retires (<paramref name="active"/> false) or restores a trade.</summary>
    public async Task SetTradeActiveAsync(string id, bool active) =>
        Keep(await api.SendAsync(active ? HttpMethod.Post : HttpMethod.Delete,
            active ? $"{Path}/trades/{Uri.EscapeDataString(id)}/restore" : $"{Path}/trades/{Uri.EscapeDataString(id)}", ApiJson.Default.AdminCatalogDto));

    public async Task AddServiceAsync(SaveServiceRequest request) =>
        Keep(await api.SendAsync(HttpMethod.Post, $"{Path}/services", request, ApiJson.Default.SaveServiceRequest, ApiJson.Default.AdminCatalogDto));

    public async Task UpdateServiceAsync(string id, SaveServiceRequest request) =>
        Keep(await api.SendAsync(HttpMethod.Put, $"{Path}/services/{Uri.EscapeDataString(id)}", request, ApiJson.Default.SaveServiceRequest, ApiJson.Default.AdminCatalogDto));

    /// <summary>Retires (<paramref name="active"/> false) or restores a job. A trade's "Others" job can't be retired.</summary>
    public async Task SetServiceActiveAsync(string id, bool active) =>
        Keep(await api.SendAsync(active ? HttpMethod.Post : HttpMethod.Delete,
            active ? $"{Path}/services/{Uri.EscapeDataString(id)}/restore" : $"{Path}/services/{Uri.EscapeDataString(id)}", ApiJson.Default.AdminCatalogDto));

    /// <summary>What retiring a trade means for the people already in it, for the "are you sure" box.</summary>
    public static string RetireTradeWarning(AdminTradeDto trade) =>
        $"\"{trade.English}\" disappears from the app's lists for new posts, searches and sign-ups. "
        + $"{Count(trade.Workers, "skilled worker")} and {Count(trade.OpenPosts, "open post")} are in it now: "
        + "posts and chats already made keep their name. Nothing is deleted; you can restore it.";

    /// <summary>What retiring a job means, for the "are you sure" box.</summary>
    public static string RetireServiceWarning(AdminServiceDto service) =>
        $"\"{service.English}\" disappears from the app's lists. {Count(service.Workers, "skilled worker")} offer it and "
        + $"{Count(service.OpenPosts, "open post")} ask for it: posts and chats already made keep their name. Nothing is deleted; you can restore it.";

    /// <summary>"1 skilled worker", "3 skilled workers".</summary>
    public static string Count(int n, string thing) => n == 1 ? $"1 {thing}" : $"{n:N0} {thing}s";

    /// <summary>"₱755 – ₱1,200 – ₱2,000", or a dash with no guide.</summary>
    public static string PriceText(PriceGuideDto? price) =>
        price is null ? "—" : $"₱{price.Min:N0} · ₱{price.Average:N0} · ₱{price.High:N0}";

    private void Keep(AdminCatalogDto catalog)
    {
        Current = catalog;
        reference.Forget(); // the lists' trade filters read the config: show the new names there too
    }
}
