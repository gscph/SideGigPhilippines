using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The app's config (trades and jobs, towns, price tiers…) lives in Azure SQL (<c>004-config.sql</c>) and reaches the
/// app through <c>GET config</c>, versioned by an ETag so a phone with the latest copy downloads nothing. The app ships a
/// copy for its first launch offline, so the two must agree; a retired trade must not be offered; and what people type
/// under "Others" is kept for analytics without staying tied to a deleted account.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class ConfigTests(ApiTestHost api)
{
    [IntegrationFact]
    public async Task ServerConfig_MatchesTheCopyBuiltIntoTheApp()
    {
        var phone = new TestPhone(api, "Conf");

        var fetched = await phone.Get<IConfigSource<AppConfig>>().FetchAsync(null, CancellationToken.None);

        fetched.Should().NotBeNull();
        fetched!.Version.Should().NotBeNullOrEmpty();
        var config = fetched.Value!;
        var builtIn = ReferenceData.BuiltInConfig;
        const string because = "004-config.sql and ReferenceData.BuiltInConfig must say the same (ids, labels, icons, amounts, order)";
        config.Categories.Should().Equal(builtIn.Categories, because);
        foreach (var category in builtIn.Categories)
        {
            config.ServicesByCategory[category.Id].Should().Equal(builtIn.ServicesByCategory[category.Id], $"the jobs of {category.Id}");
        }

        config.ServicesByCategory.Keys.Should().BeEquivalentTo(builtIn.ServicesByCategory.Keys);
        config.Cities.Should().Equal(builtIn.Cities, because);
        config.PriceTiers.Should().Equal(builtIn.PriceTiers, because);
        config.Experience.Should().Equal(builtIn.Experience, because);
        config.Urgencies.Should().Equal(builtIn.Urgencies, because);
        config.IncomeGoals.Should().Equal(builtIn.IncomeGoals, because);
        config.Prices.Should().BeEquivalentTo(builtIn.Prices, because);
        config.MinimumWage.Should().Be(builtIn.MinimumWage, because);
        ReferenceData.CanUse(config).Should().BeTrue();
    }

    [IntegrationFact]
    public async Task SameVersion_Is304_WithNoBody_AndCachingHeaders()
    {
        var client = api.Client();
        using var first = await client.GetAsync("config");
        var etag = first.Headers.ETag;

        using var request = new HttpRequestMessage(HttpMethod.Get, "config");
        request.Headers.IfNoneMatch.Add(etag!);
        using var again = await client.SendAsync(request);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        etag.Should().NotBeNull();
        first.Headers.CacheControl!.MaxAge.Should().Be(TimeSpan.FromHours(12));
        first.Headers.CacheControl.Public.Should().BeTrue();
        again.StatusCode.Should().Be(HttpStatusCode.NotModified);
        (await again.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
        again.Headers.ETag.Should().Be(etag);
    }

    // JSON answers go out gzipped to a phone that accepts it (much less prepaid data); an empty 304 stays empty,
    // and a caller that doesn't ask (an old app) gets plain JSON.
    [IntegrationFact]
    public async Task CallerAcceptingGzip_GetsTheSameJsonCompressed()
    {
        var client = api.Client();
        using var plain = await client.GetAsync("config");
        var plainBytes = await plain.Content.ReadAsByteArrayAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "config");
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        using var zipped = await client.SendAsync(request);
        var zippedBytes = await zipped.Content.ReadAsByteArrayAsync();
        await using var unzip = new System.IO.Compression.GZipStream(new MemoryStream(zippedBytes), System.IO.Compression.CompressionMode.Decompress);
        using var unzipped = new MemoryStream();
        await unzip.CopyToAsync(unzipped);

        plain.Content.Headers.ContentEncoding.Should().BeEmpty();
        zipped.Content.Headers.ContentEncoding.Should().Equal("gzip");
        unzipped.ToArray().Should().Equal(plainBytes);
        zippedBytes.Length.Should().BeLessThan(plainBytes.Length / 3, "the config is repetitive JSON");

        using var notModified = new HttpRequestMessage(HttpMethod.Get, "config");
        notModified.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        notModified.Headers.IfNoneMatch.Add(plain.Headers.ETag!);
        using var again = await client.SendAsync(notModified);
        again.StatusCode.Should().Be(HttpStatusCode.NotModified);
        again.Content.Headers.ContentEncoding.Should().BeEmpty();
        (await again.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
    }

    [Fact]
    public void AppHttpHandler_AsksForGzip()
    {
        using var handler = SideGigPh.Maui.Services.Api.ApiClient.GzipHandler();

        handler.AutomaticDecompression.Should().HaveFlag(DecompressionMethods.GZip);
    }

    [IntegrationFact]
    public async Task OtherVersion_GetsTheWholeConfig()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "config");
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"old\""));

        using var response = await api.Client().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ConfigDto))!.Catalog.Should().NotBeEmpty();
    }

    [IntegrationFact]
    public async Task PhoneWithTheLatestCopy_IsToldUnchanged()
    {
        var source = new TestPhone(api, "Etag").Get<IConfigSource<AppConfig>>();
        var first = await source.FetchAsync(null, CancellationToken.None);

        var second = await source.FetchAsync(first!.Version, CancellationToken.None);

        second!.IsUnchanged.Should().BeTrue();
        second.Version.Should().Be(first.Version);
    }

    [IntegrationFact]
    public async Task RetiredTrade_IsNotOffered_ButKeepsItsRow()
    {
        var config = await api.Client().GetFromJsonAsync("config", ApiJson.Default.ConfigDto);

        config!.Catalog.Select(c => c.Id).Should().NotContain("home_massage");
        config.Catalog.SelectMany(c => c.Services).Select(s => s.Id).Should().NotContain(id => id.StartsWith("massage-"));
        (await Scalar("SELECT COUNT(*) FROM dbo.Categories WHERE Id = 'home_massage' AND IsActive = 0")).Should().Be(1,
            "old listings and reviews still point at the id");
    }

    [IntegrationFact]
    public async Task OthersNote_IsKept_WithoutWhoWroteIt()
    {
        var phone = new TestPhone(api, "Noel");
        var notes = new OtherJobNotes(phone.Get<IOtherJobNoteRepository>(), NullLogger<OtherJobNotes>.Instance);
        var text = $"Stainless na rehas {Guid.NewGuid():N}"[..40];

        await notes.NoteAsync("welder", UserRole.Professional, text);

        var note = api.OtherJobNotes.All.Should().ContainSingle(n => n.GetString("Text") == text).Subject;
        note.PartitionKey.Should().Be("welder");
        note.GetString("Side").Should().Be("professional");
        note.Keys.Should().NotContain(k => k.Contains("Uid", StringComparison.OrdinalIgnoreCase),
            "a note is analytics only, so deleting the account has nothing to clear");
    }

    [IntegrationFact]
    public async Task OthersNote_EmptyOrBadSide_Is400()
    {
        var client = api.Client(api.Tokens.For("someone"));

        using var empty = await client.PostAsJsonAsync("other-jobs", new OtherJobRequest("welder", Wire.Seeker, "  "), ApiJson.Default.OtherJobRequest);
        using var badSide = await client.PostAsJsonAsync("other-jobs", new OtherJobRequest("welder", "admin", "gate"), ApiJson.Default.OtherJobRequest);
        using var badId = await client.PostAsJsonAsync("other-jobs", new OtherJobRequest("Welder!", Wire.Seeker, "gate"), ApiJson.Default.OtherJobRequest);

        empty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        badSide.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        badId.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<int> Scalar(string sql)
    {
        await using var connection = new SqlConnection(api.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }
}
