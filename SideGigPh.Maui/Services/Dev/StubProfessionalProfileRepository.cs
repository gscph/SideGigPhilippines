using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Dev;

/// <summary>
/// Stand-in until Firestore is wired. Profile lookup returns "no profile"; search returns
/// SAMPLE listings across Rizal, Metro Manila, Laguna and Cavite so the Seeker flow can be exercised end to end.
/// The numbers use the unassigned +63 900 prefix so they never reach a real person.
/// Flip <see cref="PretendHasProfile"/> to <c>true</c> to exercise the Professional route.
/// A published listing is kept in memory only (lost on restart, never leaves the phone), so
/// on this device the Seeker side can find it.
/// </summary>
public sealed class StubProfessionalProfileRepository : IProfessionalProfileRepository
{
    // Dev toggle: true routes to the Professional page after sign-in.
    private const bool PretendHasProfile = false;

    private static readonly IReadOnlyList<ProfessionalListing> SampleListings =
    [
        Sample("1", "Mang Jun Aircon Services", "aircon", ["aircon-cleaning", "aircon-repair", "aircon-install", "aircon-freon"], ["taytay", "angono"], "10-plus", "500-1000"),
        Sample("2", "Kuya Ben Cool Aircon", "aircon", ["aircon-cleaning", "aircon-leak", "aircon-repair"], ["antipolo", "taytay"], "3-10", "under-500"),
        Sample("3", "RJ Aircon & Ref", "aircon", ["aircon-install", "aircon-repair", "aircon-freon"], ["binangonan", "angono"], "1-3", "1000-2000"),
        Sample("4", "Tatay Ramon Tubero", "plumbing", ["plumbing-leak", "plumbing-clog", "plumbing-replace"], ["taytay", "antipolo"], "10-plus", "500-1000"),
        Sample("5", "AquaFix Plumbing", "plumbing", ["plumbing-install", "plumbing-pump", "plumbing-leak"], ["binangonan", "angono"], "3-10", "1000-2000"),
        Sample("6", "Edwin Electrical Works", "electrical", ["electrical-no-power", "electrical-outlet", "electrical-lights", "electrical-wiring"], ["taytay", "angono", "binangonan"], "3-10", "500-1000"),
        Sample("7", "Rodel Electrician", "electrical", ["electrical-no-power", "electrical-outlet", "electrical-lights"], ["antipolo"], "1-3", "under-500"),
        Sample("8", "Mang Carding Karpintero", "carpentry", ["carpentry-furniture", "carpentry-door-window", "carpentry-cabinet"], ["taytay", "antipolo"], "10-plus", "1000-2000"),
        Sample("9", "Jomar Woodworks", "carpentry", ["carpentry-cabinet", "carpentry-ceiling-roof", "carpentry-partition"], ["binangonan"], "3-10", "2000-5000"),
        Sample("10", "Bubong Masters Rizal", "roofing", ["roofing-leak", "roofing-gutter", "roofing-sheet", "roofing-waterproofing"], ["antipolo", "taytay", "angono"], "10-plus", "2000-5000"),
        Sample("11", "Nonoy Welding Shop", "welder", ["welder-repair", "welder-gate", "welder-grills", "welder-railings"], ["taytay", "angono"], "3-10", "1000-2000"),
        Sample("12", "Mang Pido Tiles & Semento", "masonry_tiling", ["masonry-tiles", "masonry-cement", "masonry-crack", "masonry-cr-renovation"], ["binangonan", "angono"], "10-plus", "2000-5000"),
        Sample("13", "Ate Liza PasaBuy", "errand_runner", ["errand-grocery", "errand-bills", "errand-pasabuy", "errand-parcel"], ["taytay", "antipolo"], "1-3", "under-500"),
        Sample("14", "SunClean Solar Services", "solar_cleaning", ["solar-cleaning", "solar-checkup", "solar-inverter"], ["antipolo", "binangonan"], "1-3", "1000-2000"),
        Sample("15", "Aling Nena Hilot & Massage", "home_massage", ["massage-full-body", "massage-hilot", "massage-ventosa", "massage-foot"], ["angono", "binangonan", "taytay"], "10-plus", "500-1000"),

        // Metro Manila, Laguna (Nuvali side) and Cavite, so search works outside Rizal too.
        Sample("16", "QC Aircon Doctors", "aircon", ["aircon-cleaning", "aircon-repair", "aircon-freon"], ["quezon-city", "marikina", "san-juan"], "3-10", "500-1000"),
        Sample("17", "Pasig Tubero Express", "plumbing", ["plumbing-leak", "plumbing-clog", "plumbing-install"], ["pasig", "mandaluyong", "makati"], "10-plus", "1000-2000"),
        Sample("18", "Nuvali Home Aircon", "aircon", ["aircon-cleaning", "aircon-install", "aircon-repair"], ["santa-rosa", "calamba", "cabuyao"], "3-10", "1000-2000"),
        Sample("19", "Sta. Rosa Electrical", "electrical", ["electrical-no-power", "electrical-wiring", "electrical-outlet"], ["santa-rosa", "binan", "san-pedro"], "1-3", "500-1000"),
        Sample("20", "Bacoor Plumbing Masters", "plumbing", ["plumbing-leak", "plumbing-replace", "plumbing-pump"], ["bacoor", "imus", "kawit"], "3-10", "500-1000"),
        Sample("21", "Imus Home Massage", "home_massage", ["massage-full-body", "massage-hilot", "massage-foot"], ["imus", "dasmarinas", "general-trias"], "1-3", "500-1000"),
    ];

    private readonly List<ProfessionalListing> _published = [];

    public Task<bool> HasProfileAsync(string uid) => Task.FromResult(PretendHasProfile);

    public Task<IReadOnlyList<ProfessionalListing>> SearchAsync(ProfessionalSearchQuery query)
    {
        IReadOnlyList<ProfessionalListing> matches = _published.Concat(SampleListings)
            .Where(p => p.CategoryIds.Contains(query.CategoryId))
            .Where(p => query.AreaId is null || p.CityIds.Any(cityId => ReferenceData.AreaOf(cityId) == query.AreaId))
            .ToList();
        return Task.FromResult(matches);
    }

    public Task<ProfessionalListing?> GetByIdAsync(string professionalId) =>
        Task.FromResult(_published.Concat(SampleListings).FirstOrDefault(p => p.Id == professionalId));

    public Task<PublishResult> PublishAsync(ProfessionalListing listing)
    {
        if (_published.Concat(SampleListings).Any(p => p.Id != listing.Id && p.MobileNumber == listing.MobileNumber))
        {
            return Task.FromResult(PublishResult.PhoneTaken);
        }

        _published.RemoveAll(p => p.Id == listing.Id);
        _published.Add(listing);
        return Task.FromResult(PublishResult.Published);
    }

    public Task RemoveAsync(string professionalId)
    {
        _published.RemoveAll(p => p.Id == professionalId);
        return Task.CompletedTask;
    }

    private static ProfessionalListing Sample(
        string id, string name, string categoryId, string[] serviceIds, string[] cityIds, string experienceId, string rateTierId) => new()
    {
        Id = id,
        DisplayName = name,
        CategoryIds = [categoryId],
        ServiceIds = serviceIds,
        CityIds = cityIds,
        ExperienceId = experienceId,
        RateTierId = rateTierId,
        MobileNumber = "+6390000" + id.PadLeft(5, '0'),
    };
}
