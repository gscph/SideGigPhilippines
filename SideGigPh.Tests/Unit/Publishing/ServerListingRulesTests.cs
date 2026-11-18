using System.Text.Json;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// The server's last check before a listing goes live (<see cref="ProfessionalStore.InvalidField"/>). It answers
/// <c>400 invalid-{field}</c>, so a rule that is too strict blocks real workers, and one that is too loose lets fake
/// listings in. The last tests make sure the app and the server agree on the numbers (2 to 6 work photos).
/// </summary>
public class ServerListingRulesTests
{
    private static PublishListingRequest Valid() => new(
        DisplayName: "Juan Dela Cruz",
        CategoryId: "aircon",
        AreaId: "rizal",
        ServiceIds: ["aircon-cleaning"],
        CityIds: ["taytay"],
        ExperienceId: "3-10",
        RateTierId: "500-1000",
        ProfilePhotoId: TestData.PhotoId(1),
        WorkPhotoIds: [TestData.PhotoId(2), TestData.PhotoId(3)]);

    [Fact]
    public void InvalidField_CompleteListing_IsAccepted() =>
        ProfessionalStore.InvalidField(Valid()).Should().BeNull();

    // An app from before references were dropped still sends a "references" list: the server must read the rest of
    // the listing and publish it, never refuse it (an unknown JSON field is ignored, not an error).
    [Fact]
    public void OldAppBody_WithReferences_ReadsAndIsAccepted()
    {
        var json = JsonSerializer.Serialize(Valid(), ApiJson.Default.PublishListingRequest)[..^1]
            + ",\"references\":[{\"name\":\"Aling Nena\",\"mobileNumber\":\"+639181111111\"}]}";

        var request = JsonSerializer.Deserialize(json, ApiJson.Default.PublishListingRequest);

        request.Should().BeEquivalentTo(Valid());
        ProfessionalStore.InvalidField(request!).Should().BeNull();
    }

    [Fact]
    public void InvalidField_OneWorkPhoto_IsWorkPhotoIds() =>
        ProfessionalStore.InvalidField(Valid() with { WorkPhotoIds = [TestData.PhotoId(2)] })
            .Should().Be("workPhotoIds");

    [Fact]
    public void InvalidField_SameWorkPhotoTwice_IsWorkPhotoIds() =>
        ProfessionalStore.InvalidField(Valid() with { WorkPhotoIds = [TestData.PhotoId(2), TestData.PhotoId(2)] })
            .Should().Be("workPhotoIds");

    [Fact]
    public void InvalidField_NoProfilePhoto_IsProfilePhotoId() =>
        ProfessionalStore.InvalidField(Valid() with { ProfilePhotoId = null }).Should().Be("profilePhotoId");

    [Fact]
    public void InvalidField_NoTowns_IsCityIds() =>
        ProfessionalStore.InvalidField(Valid() with { CityIds = [] }).Should().Be("cityIds");

    [Fact]
    public void InvalidField_NoJobs_IsServiceIds() =>
        ProfessionalStore.InvalidField(Valid() with { ServiceIds = [] }).Should().Be("serviceIds");

    [Fact]
    public void InvalidField_RateDependsOnJob_IsAccepted() =>
        ProfessionalStore.InvalidField(Valid() with { RateTierId = "quote" }).Should().BeNull();

    // If the app and the server disagree, the app shows "Publish" and the server refuses it (or the reverse).
    [Fact]
    public void ProofRules_AppAndServerAgree()
    {
        ProfessionalDraft.MinWorkPhotos.Should().Be(ProfessionalStore.MinWorkPhotos);
        Maui.Services.ProWorkspace.MaxWorkPhotos.Should().Be(ProfessionalStore.MaxWorkPhotos);
    }

    [Fact]
    public void ReadyDraft_InTheApp_IsAlsoValidOnTheServer()
    {
        // Every catalog id the app sends must pass the server's id check.
        var everyId = Maui.Services.ReferenceData.Categories
            .Concat(Maui.Services.ReferenceData.ServicesByCategory.Values.SelectMany(s => s))
            .Concat(Maui.Services.ReferenceData.Cities)
            .Concat(Maui.Services.ReferenceData.Experience)
            .Concat(Maui.Services.ReferenceData.RateLevels)
            .Append(Maui.Services.ReferenceData.QuoteOption)
            .Select(option => option.Id)
            .Append(Maui.Services.ReferenceData.ServiceAreaId);

        everyId.Should().OnlyContain(id => StoreText.IsCode(id), "the server answers invalid-… for any other id");
    }
}
