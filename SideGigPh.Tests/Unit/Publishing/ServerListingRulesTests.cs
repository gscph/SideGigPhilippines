using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// The server's last check before a listing goes live (<see cref="ProfessionalStore.InvalidField"/>). It answers
/// <c>400 invalid-{field}</c>, so a rule that is too strict blocks real workers, and one that is too loose lets fake
/// listings in. The last tests make sure the app and the server agree on the numbers (1–3 references; work photos are optional).
/// </summary>
public class ServerListingRulesTests
{
    private const string Own = TestData.WorkerNumber;

    private static PublishListingRequest Valid() => new(
        DisplayName: "Juan Dela Cruz",
        CategoryId: "aircon",
        AreaId: "rizal",
        ServiceIds: ["aircon-cleaning"],
        CityIds: ["taytay"],
        ExperienceId: "3-10",
        RateTierId: "500-1000",
        ProfilePhotoId: TestData.PhotoId(1),
        WorkPhotoIds: [TestData.PhotoId(2), TestData.PhotoId(3)],
        References: [new ReferenceDto("Aling Nena", TestData.ReferenceNumber)]);

    [Fact]
    public void InvalidField_CompleteListing_IsAccepted() =>
        ProfessionalStore.InvalidField(Valid(), Own).Should().BeNull();

    [Fact]
    public void InvalidField_ReferenceIsWorkersOwnNumber_IsReferences() =>
        ProfessionalStore.InvalidField(Valid() with { References = [new ReferenceDto("Me", Own)] }, Own)
            .Should().Be("references");

    [Fact]
    public void InvalidField_SameReferenceNumberTwice_IsReferences() =>
        ProfessionalStore.InvalidField(Valid() with
        {
            References = [new ReferenceDto("A", TestData.ReferenceNumber), new ReferenceDto("B", TestData.ReferenceNumber)],
        }, Own).Should().Be("references");

    [Fact]
    public void InvalidField_FourReferences_IsReferences() =>
        ProfessionalStore.InvalidField(Valid() with
        {
            References = [.. Enumerable.Range(1, 4).Select(i => new ReferenceDto($"R{i}", $"+63918000000{i}"))],
        }, Own).Should().Be("references");

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void InvalidField_NoReferences_IsReferences(int? count) =>
        ProfessionalStore.InvalidField(Valid() with { References = count is null ? null : [] }, Own)
            .Should().Be("references");

    [Fact]
    public void InvalidField_ReferenceNumberNotE164_IsReferences() =>
        ProfessionalStore.InvalidField(Valid() with { References = [new ReferenceDto("A", "09181111111")] }, Own)
            .Should().Be("references");

    // Work photos are optional: a worker publishes with none and adds them later to be seen more.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(6)]
    public void InvalidField_AnyNumberOfWorkPhotosUpToSix_IsAccepted(int count) =>
        ProfessionalStore.InvalidField(Valid() with { WorkPhotoIds = [.. Enumerable.Range(2, count).Select(TestData.PhotoId)] }, Own)
            .Should().BeNull();

    [Fact]
    public void InvalidField_SameWorkPhotoTwice_IsAccepted_TheCopyIsDropped() =>
        ProfessionalStore.InvalidField(Valid() with { WorkPhotoIds = [TestData.PhotoId(2), TestData.PhotoId(2)] }, Own)
            .Should().BeNull();

    [Fact]
    public void InvalidField_WorkPhotoIdNotAPhoto_IsWorkPhotoIds() =>
        ProfessionalStore.InvalidField(Valid() with { WorkPhotoIds = [TestData.PhotoId(2), "../not-a-photo"] }, Own)
            .Should().Be("workPhotoIds");

    [Fact]
    public void InvalidField_WorkPhotoIdsMissing_IsWorkPhotoIds() =>
        ProfessionalStore.InvalidField(Valid() with { WorkPhotoIds = null! }, Own)
            .Should().Be("workPhotoIds");

    [Fact]
    public void InvalidField_NoProfilePhoto_IsProfilePhotoId() =>
        ProfessionalStore.InvalidField(Valid() with { ProfilePhotoId = null }, Own).Should().Be("profilePhotoId");

    [Fact]
    public void InvalidField_NoTowns_IsCityIds() =>
        ProfessionalStore.InvalidField(Valid() with { CityIds = [] }, Own).Should().Be("cityIds");

    [Fact]
    public void InvalidField_NoJobs_IsServiceIds() =>
        ProfessionalStore.InvalidField(Valid() with { ServiceIds = [] }, Own).Should().Be("serviceIds");

    [Fact]
    public void InvalidField_RateDependsOnJob_IsAccepted() =>
        ProfessionalStore.InvalidField(Valid() with { RateTierId = "quote" }, Own).Should().BeNull();

    [Theory]
    [InlineData("+639171234567", true)]
    [InlineData("09171234567", false)]  // local format: the app converts before sending
    [InlineData("+63917123456", false)] // one digit short
    [InlineData("+63281234567", false)] // landline
    [InlineData(null, false)]
    public void IsMobileNumber_OnlyAcceptsPhilippineMobileInE164(string? number, bool expected) =>
        StoreText.IsMobileNumber(number).Should().Be(expected);

    // If the app and the server disagree, the app shows "Publish" and the server refuses it (or the reverse).
    [Fact]
    public void ProofRules_AppAndServerAgree()
    {
        ProfessionalDraft.GoodWorkPhotos.Should().Be(SearchRanker.FullPhotoBonusAt, "the \"be seen more\" item is done when the bonus is full");
        ProfessionalDraft.MinReferences.Should().Be(ProfessionalStore.MinReferences);
        ProfessionalDraft.MaxReferences.Should().Be(ProfessionalStore.MaxReferences);
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
