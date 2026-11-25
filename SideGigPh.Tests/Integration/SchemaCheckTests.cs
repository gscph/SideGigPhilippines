using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// An API deployed before its schema script fails every call that reads the missing table (search and the Inbox did,
/// on 15 Nov, from a missing <c>011-schema-safety</c>). The warm-up checks <c>dbo.SchemaChanges</c> against the code's list.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class SchemaCheckTests(ApiTestHost api)
{
    [Fact]
    public void TheRequiredList_IsEveryLaterSchemaScriptInTheFolder() =>
        SchemaScripts.Required.Should().Equal(
            TestDatabase.SchemaScripts.Select(Path.GetFileNameWithoutExtension).Where(name => name != "001-schema"),
            "a new NNN-schema-*.sql needs its name in SchemaScripts.Required");

    [IntegrationFact]
    public async Task ADatabaseBuiltLikeLive_MissesNothing()
    {
        using var scope = api.Server.Services.CreateScope();

        (await scope.ServiceProvider.GetRequiredService<WarmupStore>().MissingScriptsAsync(CancellationToken.None)).Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task AScriptThatWasNeverRun_IsNamed()
    {
        using var scope = api.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SideGigPhDb>();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM dbo.SchemaChanges WHERE Script = '011-schema-safety'");
        try
        {
            var missing = await scope.ServiceProvider.GetRequiredService<WarmupStore>().MissingScriptsAsync(CancellationToken.None);

            missing.Should().Equal("011-schema-safety");
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("INSERT INTO dbo.SchemaChanges (Script) VALUES ('011-schema-safety')");
        }
    }

    // The app saves the profile without reading it first (one call on 4G), so the server must keep the first time.
    [IntegrationFact]
    public async Task SavingTheProfileAgain_KeepsWhenItWasFirstSaved()
    {
        var uid = $"returning-{Guid.NewGuid():N}";
        var client = api.Client(api.Tokens.For(uid));
        var later = DateTimeOffset.UtcNow.AddDays(30);
        UserDto Profile(DateTimeOffset createdAt) => new(uid, "Ana", null, null, Wire.English, Wire.Seeker, null, createdAt, createdAt);

        using var first = await client.PutAsJsonAsync("users/me", Profile(DateTimeOffset.UtcNow), ApiJson.Default.UserDto);
        var created = (await first.Content.ReadFromJsonAsync(ApiJson.Default.UserDto))!.CreatedAt;
        using var second = await client.PutAsJsonAsync("users/me", Profile(later), ApiJson.Default.UserDto);

        (await second.Content.ReadFromJsonAsync(ApiJson.Default.UserDto))!.CreatedAt.Should().BeCloseTo(created, TimeSpan.FromMilliseconds(1), "the database keeps milliseconds");
    }
}
