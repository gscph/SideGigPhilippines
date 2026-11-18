using System.Text.Json;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// The server's last check before a listing goes live (<see cref="ProfessionalStore.InvalidField"/>). It answers
/// <c>400 invalid-{field}</c>, so a rule that is too strict blocks real workers, and one that is too loose lets fake
/// listings in. The last tests make sure the app and the server agree on the numbers (up to 6 work photos, optional).
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
        RatePesos: 800,
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

    // Work photos are optional: a worker publishes with none and adds them later to be seen more.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(6)]
    public void InvalidField_AnyNumberOfWorkPhotosUpToSix_IsAccepted(int count) =>
        ProfessionalStore.InvalidField(Valid() with { WorkPhotoIds = [.. Enumerable.Range(2, count).Select(TestData.PhotoId)] })
            .Should().BeNull();

    [Fact]
    public void InvalidField_SameWorkPhotoTwice_IsAccepted_TheCopyIsDropped() =>
        ProfessionalStore.InvalidField(Valid() with { WorkPhotoIds = [TestData.PhotoId(2), TestData.PhotoId(2)] })
            .Should().BeNull();

    [Fact]
    public void InvalidField_WorkPhotoIdNotAPhoto_IsWorkPhotoIds() =>
        ProfessionalStore.InvalidField(Valid() with { WorkPhotoIds = [TestData.PhotoId(2), "../not-a-photo"] })
            .Should().Be("workPhotoIds");

    [Fact]
    public void InvalidField_WorkPhotoIdsMissing_IsWorkPhotoIds() =>
        ProfessionalStore.InvalidField(Valid() with { WorkPhotoIds = null! })
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
    public void InvalidField_NoOrZeroPrice_IsRatePesos()
    {
        ProfessionalStore.InvalidField(Valid() with { RatePesos = null }).Should().Be("ratePesos");
        ProfessionalStore.InvalidField(Valid() with { RatePesos = 0 }).Should().Be("ratePesos");
        ProfessionalStore.InvalidField(Valid() with { RatePesos = 1 }).Should().BeNull("any price above ₱0: the guide is only a guide");
    }

    // If the app and the server disagree, the app shows "Publish" and the server refuses it (or the reverse).
    [Fact]
    public void ProofRules_AppAndServerAgree()
    {
        ProfessionalDraft.GoodWorkPhotos.Should().Be(SearchRanker.FullPhotoBonusAt, "the \"be seen more\" item is done when the bonus is full");
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
            .Select(option => option.Id)
            .Append(Maui.Services.ReferenceData.ServiceAreaId);

        everyId.Should().OnlyContain(id => StoreText.IsCode(id), "the server answers invalid-… for any other id");
    }
}
