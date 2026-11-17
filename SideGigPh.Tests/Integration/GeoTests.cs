using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Tests.Integration.Support;

using GeoPoint = SideGigPh.AzureSQL.GeoPoint;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// A worker's base point and travel distance (<c>013-schema-geo.sql</c>) on a real SQL Server: the geography column
/// and spatial index exist, a listing without a point starts at its town's centre, and matching by distance uses each
/// worker's own radius. The point is private: it is never in what other people read.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class GeoTests(ApiTestHost api)
{
    // Taytay's centre and a spot in Antipolo about 6 km from it.
    private static readonly GeoPoint Taytay = GeoRules.TownCentres["taytay"];
    private static readonly GeoPoint InAntipolo = new(14.5865, 121.1763);

    private async Task<HttpResponseMessage> PublishAsync(TestPhone worker, double? latitude, double? longitude, int? radiusKm)
    {
        var listing = (await api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber)).GetFromJsonAsync($"professionals/{worker.Uid}", ApiJson.Default.ListingDto))!;
        var request = new PublishListingRequest(listing.DisplayName, listing.CategoryId, listing.AreaId, listing.ServiceIds, listing.CityIds,
            listing.ExperienceId, listing.RateTierId, listing.ProfilePhoto!.Id, [.. listing.WorkPhotos.Select(p => p.Id)],
            [new ReferenceDto("Aling Nena", TestPhone.UniqueNumber())], latitude, longitude, radiusKm);
        return await api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber)).PutAsJsonAsync("professionals/me", request, ApiJson.Default.PublishListingRequest);
    }

    private async Task<IReadOnlyList<string>> InRangeAsync(string trade, string? job, GeoPoint point)
    {
        using var scope = api.Server.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ProfessionalStore>().WorkersInRangeAsync(trade, job, point, CancellationToken.None);
    }

    [IntegrationFact]
    public async Task AnAppThatSendsNoPoint_StartsTheWorkerAtTheirTownsCentre_With10Km()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Geo", trade: "construction", jobs: ["construction-plumbing"]);
        await worker.Pro.PublishAsync();

        using var scope = api.Server.Services.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<SideGigPhDb>().Professionals.AsNoTracking().SingleAsync(p => p.Id == worker.Uid);
        (row.Latitude, row.Longitude, row.RadiusKm).Should().Be((Taytay.Latitude, Taytay.Longitude, GeoRules.DefaultRadiusKm));
        (await InRangeAsync("construction", "construction-plumbing", Taytay)).Should().Contain(worker.Uid);
    }

    [IntegrationFact]
    public async Task MatchingByDistance_UsesEachWorkersOwnRadius_AndTheJob()
    {
        var near = await TestPhone.WorkerReadyToPublishAsync(api, "Near", trade: "construction", jobs: ["construction-electrical"]);
        await near.Pro.PublishAsync();
        var far = await TestPhone.WorkerReadyToPublishAsync(api, "Far", trade: "construction", jobs: ["construction-electrical"]);
        await far.Pro.PublishAsync();

        // Both are based at Taytay's centre; one goes 3 km, the other 20 km. The job is in Antipolo, about 6 km away.
        (await PublishAsync(near, Taytay.Latitude, Taytay.Longitude, 3)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await PublishAsync(far, Taytay.Latitude, Taytay.Longitude, 20)).StatusCode.Should().Be(HttpStatusCode.OK);

        var matched = await InRangeAsync("construction", "construction-electrical", InAntipolo);
        matched.Should().Contain(far.Uid).And.NotContain(near.Uid);
        (await InRangeAsync("construction", "construction-tiles", InAntipolo)).Should().NotContain(far.Uid, "they don't offer that job");
        (await InRangeAsync("construction", null, Taytay)).Should().Contain([near.Uid, far.Uid]);
    }

    [IntegrationFact]
    public async Task ARepublishWithoutAPoint_KeepsTheWorkersOwnPointAndRadius()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Keeps", trade: "construction", jobs: ["construction-carpentry"]);
        await worker.Pro.PublishAsync();
        (await PublishAsync(worker, InAntipolo.Latitude, InAntipolo.Longitude, 25)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await PublishAsync(worker, null, null, null)).StatusCode.Should().Be(HttpStatusCode.OK); // an older app

        using var scope = api.Server.Services.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<SideGigPhDb>().Professionals.AsNoTracking().SingleAsync(p => p.Id == worker.Uid);
        (row.Latitude, row.Longitude, row.RadiusKm).Should().Be((InAntipolo.Latitude, InAntipolo.Longitude, (short)25));
    }

    [IntegrationFact]
    public async Task APointOutsideThePhilippines_OrARadiusOutOfRange_IsRefused()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Lost");
        await worker.Pro.PublishAsync();

        foreach (var (latitude, longitude, radiusKm, field) in new (double?, double?, int?, string)[]
        {
            (0, 0, null, "latitude"), (14.55, null, null, "latitude"), (51.5, -0.12, null, "latitude"),
            (null, null, 0, "radiusKm"), (null, null, 500, "radiusKm"),
        })
        {
            using var response = await PublishAsync(worker, latitude, longitude, radiusKm);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.Invalid(field));
        }
    }

    [IntegrationFact]
    public async Task TheDatabase_HasTheGeographyColumnAndItsSpatialIndex()
    {
        using var scope = api.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SideGigPhDb>();

        (await db.Database.SqlQuery<string>($"SELECT TYPE_NAME(user_type_id) AS Value FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Professionals') AND name = 'Location'")
            .SingleAsync()).Should().Be("geography");
        (await db.Database.SqlQuery<string>($"SELECT type_desc AS Value FROM sys.indexes WHERE name = 'SIX_Professionals_Location' AND object_id = OBJECT_ID('dbo.Professionals')")
            .SingleAsync()).Should().Be("SPATIAL");
    }

    [Fact]
    public void AListingNeverCarriesTheWorkersPoint()
    {
        var fields = typeof(ListingDto).GetProperties().Select(p => p.Name);

        fields.Should().NotContain(["Latitude", "Longitude", "Location", "RadiusKm"]);
    }
}
