using System.Net;
using System.Net.Http.Json;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The operator site's Catalog page on the real API and database (<c>ops/catalog…</c>): trades and jobs are added,
/// renamed, retired and restored, the app's config follows at once, and — like an order line keeping a product's
/// title — posts and chats already made keep the names they were made with. Each test makes its own trade (the
/// database is shared) and retires it at the end, so the app's lists stay as <c>004-config.sql</c> made them.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class CatalogTests(ApiTestHost api)
{
    private HttpClient Operator => api.Client(api.Tokens.For(ApiTestHost.AdminUid));

    private static string Tag() => $"Zq{Guid.NewGuid():N}"[..10];

    private async Task<AdminCatalogDto> SendAsync(HttpMethod method, string path, HttpContent? body = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = body };
        using var response = await Operator.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync(ApiJson.Default.AdminCatalogDto))!;
    }

    private Task<AdminCatalogDto> AddTradeAsync(string english) =>
        SendAsync(HttpMethod.Post, "ops/catalog/trades", JsonContent.Create(new SaveTradeRequest(english, $"{english} TL"), ApiJson.Default.SaveTradeRequest));

    private Task<AdminCatalogDto> AddJobAsync(string tradeId, string english, PriceGuideDto? price = null) =>
        SendAsync(HttpMethod.Post, "ops/catalog/services",
            JsonContent.Create(new SaveServiceRequest(tradeId, english, $"{english} TL", price), ApiJson.Default.SaveServiceRequest));

    private Task<AdminCatalogDto> RenameJobAsync(string id, string tradeId, string english) =>
        SendAsync(HttpMethod.Put, $"ops/catalog/services/{id}",
            JsonContent.Create(new SaveServiceRequest(tradeId, english, $"{english} TL"), ApiJson.Default.SaveServiceRequest));

    private Task<ConfigDto> ConfigAsync() => api.Client().GetFromJsonAsync("config", ApiJson.Default.ConfigDto)!;

    /// <summary>A new trade with one job (and its "Others"); returns the trade and that job.</summary>
    private async Task<(AdminTradeDto Trade, AdminServiceDto Job)> TradeWithJobAsync(string tag)
    {
        var trade = (await AddTradeAsync($"Trade {tag}")).Trades.Single(t => t.English == $"Trade {tag}");
        var job = (await AddJobAsync(trade.Id, $"Job {tag}", new PriceGuideDto(500, 800, 1200)))
            .Trades.Single(t => t.Id == trade.Id).Services.Single(s => s.English == $"Job {tag}");
        return (trade, job);
    }

    private Task RetireAsync(string tradeId) => SendAsync(HttpMethod.Delete, $"ops/catalog/trades/{tradeId}");

    [IntegrationFact]
    public async Task ANewTrade_GetsAnOthersJob_AndTheAppsConfigShowsItAtOnce()
    {
        var tag = Tag();
        var (trade, job) = await TradeWithJobAsync(tag);
        try
        {
            trade.Id.Should().StartWith("trade-zq");
            job.Id.Should().StartWith(trade.Id + "-job-");
            var jobs = (await SendAsync(HttpMethod.Get, "ops/catalog")).Trades.Single(t => t.Id == trade.Id).Services;
            jobs.Select(s => s.Id).Should().Equal(job.Id, trade.Id + "-other");
            jobs[1].IsOther.Should().BeTrue();
            jobs[0].Price.Should().Be(new PriceGuideDto(500, 800, 1200));

            var config = await ConfigAsync();
            var offered = config.Catalog.Should().ContainSingle(c => c.Id == trade.Id).Subject;
            offered.English.Should().Be($"Trade {tag}");
            offered.Services.Select(s => s.Id).Should().Equal(job.Id, trade.Id + "-other");
        }
        finally
        {
            await RetireAsync(trade.Id);
        }

        (await ConfigAsync()).Catalog.Should().NotContain(c => c.Id == trade.Id, "a retired trade leaves the app's lists");
        (await SendAsync(HttpMethod.Get, "ops/catalog")).Trades.Single(t => t.Id == trade.Id).IsActive.Should().BeFalse("it is retired, never deleted");
    }

    [IntegrationFact]
    public async Task RetireAndRestore_AJob_AndTheOthersJob_StaysForever()
    {
        var (trade, job) = await TradeWithJobAsync(Tag());
        try
        {
            await SendAsync(HttpMethod.Delete, $"ops/catalog/services/{job.Id}");
            (await ConfigAsync()).Catalog.Single(c => c.Id == trade.Id).Services.Should().NotContain(s => s.Id == job.Id);

            await SendAsync(HttpMethod.Post, $"ops/catalog/services/{job.Id}/restore");
            (await ConfigAsync()).Catalog.Single(c => c.Id == trade.Id).Services.Should().Contain(s => s.Id == job.Id);

            using var others = await Operator.DeleteAsync($"ops/catalog/services/{trade.Id}-other");
            others.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await others.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.OthersStays);
        }
        finally
        {
            await RetireAsync(trade.Id);
        }
    }

    [IntegrationFact]
    public async Task RenamingAJob_ChangesTheConfig_ButNotPostsOrChatsAlreadyMade()
    {
        var tag = Tag();
        var (trade, job) = await TradeWithJobAsync(tag);
        try
        {
            var household = await TestPhone.SeekerAsync(api, "Lorna");
            var asHousehold = api.Client(api.Tokens.For(household.Uid));
            using var posted = await asHousehold.PostAsJsonAsync("job-posts",
                new NewJobPostRequest(trade.Id, job.Id, $"Kailangan ng tulong {tag}", 900, "taytay", Title: "Kailangan ng tulong"), ApiJson.Default.NewJobPostRequest);
            posted.StatusCode.Should().Be(HttpStatusCode.OK, await posted.Content.ReadAsStringAsync());
            var post = (await posted.Content.ReadFromJsonAsync(ApiJson.Default.JobPostDto))!;
            post.ServiceName.Should().Be(new CatalogNameDto($"Job {tag}", $"Job {tag} TL"));
            post.TradeName.Should().Be(new CatalogNameDto($"Trade {tag}", $"Trade {tag} TL"));

            var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Kuya");
            await worker.Pro.PublishAsync();
            var chatId = ChatStore.IdFor(worker.Uid, household.Uid);
            using (var sent = await asHousehold.PostAsJsonAsync($"chats/{chatId}/messages",
                       new SendMessageRequest($"m-{tag}", worker.Uid, household.Uid, Wire.Text, "Hi po", "P", "Lorna", job.Id, "taytay", null, null),
                       ApiJson.Default.SendMessageRequest))
            {
                sent.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            await RenameJobAsync(job.Id, trade.Id, $"Renamed {tag}");
            await SendAsync(HttpMethod.Put, $"ops/catalog/trades/{trade.Id}",
                JsonContent.Create(new SaveTradeRequest($"New trade {tag}", "Bago"), ApiJson.Default.SaveTradeRequest));

            // The app's lists show the new names at once...
            var offered = (await ConfigAsync()).Catalog.Single(c => c.Id == trade.Id);
            offered.English.Should().Be($"New trade {tag}");
            offered.Services.Single(s => s.Id == job.Id).English.Should().Be($"Renamed {tag}");

            // ...but what the household posted and the chat about it keep the names they were made with.
            var mine = (await asHousehold.GetFromJsonAsync("job-posts/mine", ApiJson.Default.ListJobPostDto))!.Single(p => p.Id == post.Id);
            (mine.ServiceId, mine.ServiceName, mine.TradeName).Should().Be((job.Id, new CatalogNameDto($"Job {tag}", $"Job {tag} TL"), new CatalogNameDto($"Trade {tag}", $"Trade {tag} TL")));
            var chat = (await asHousehold.GetFromJsonAsync($"chats/{chatId}", ApiJson.Default.ChatDto))!;
            chat.ServiceName.Should().Be(new CatalogNameDto($"Job {tag}", $"Job {tag} TL"));

            // A post made after the rename gets the new name.
            using var later = await asHousehold.PostAsJsonAsync("job-posts",
                new NewJobPostRequest(trade.Id, job.Id, $"Isa pa {tag}", 900, "taytay", Title: "Isa pa"), ApiJson.Default.NewJobPostRequest);
            (await later.Content.ReadFromJsonAsync(ApiJson.Default.JobPostDto))!.ServiceName!.English.Should().Be($"Renamed {tag}");
        }
        finally
        {
            await RetireAsync(trade.Id);
        }
    }

    [IntegrationFact]
    public async Task TheConfigScript_LeavesAnOperatorsChangesAlone()
    {
        var (trade, job) = await TradeWithJobAsync(Tag());
        var before = (await SendAsync(HttpMethod.Get, "ops/catalog")).Trades.Single(t => t.Id == "solar_cleaning");
        try
        {
            await SendAsync(HttpMethod.Put, "ops/catalog/trades/solar_cleaning",
                JsonContent.Create(new SaveTradeRequest("Solar (operator)", before.Taglish, before.HintEnglish, before.HintTaglish), ApiJson.Default.SaveTradeRequest));

            api.RunConfigScript();

            var after = await SendAsync(HttpMethod.Get, "ops/catalog");
            after.Trades.Single(t => t.Id == "solar_cleaning").English.Should().Be("Solar (operator)", "004-config.sql must not undo an operator's rename");
            after.Trades.Single(t => t.Id == trade.Id).IsActive.Should().BeTrue("004-config.sql must not retire a trade an operator added");
            after.Trades.Single(t => t.Id == trade.Id).Services.Should().Contain(s => s.Id == job.Id && s.IsActive);
        }
        finally
        {
            await SendAsync(HttpMethod.Put, "ops/catalog/trades/solar_cleaning",
                JsonContent.Create(new SaveTradeRequest(before.English, before.Taglish, before.HintEnglish, before.HintTaglish), ApiJson.Default.SaveTradeRequest));
            await RetireAsync(trade.Id);
        }
    }

    [IntegrationFact]
    public async Task ANameThatIsTooLong_IsRefused()
    {
        using var response = await Operator.PostAsJsonAsync("ops/catalog/trades",
            new SaveTradeRequest(new string('a', CatalogRules.MaxTradeName + 1), "Ok"), ApiJson.Default.SaveTradeRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [IntegrationTheory]
    [InlineData("GET", "ops/catalog")]
    [InlineData("POST", "ops/catalog/trades")]
    [InlineData("PUT", "ops/catalog/trades/aircon")]
    [InlineData("DELETE", "ops/catalog/trades/aircon")]
    [InlineData("DELETE", "ops/catalog/services/aircon-cleaning")]
    public async Task OnlyOperators_ChangeTheCatalog(string method, string route)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), route)
        {
            Content = JsonContent.Create(new SaveTradeRequest("Hacked", "Hacked"), ApiJson.Default.SaveTradeRequest),
        };
        using var someone = await api.Client(api.Tokens.For($"not-admin-{Guid.NewGuid():N}")).SendAsync(request);

        someone.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ConfigAsync()).Catalog.Single(c => c.Id == "aircon").English.Should().NotBe("Hacked");
    }
}
