using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Local;

namespace SideGigPh.Maui.Services.Dev;

/// <summary>
/// Stand-in until Firestore is wired. Keeps every inquiry in a file on this phone
/// (<c>dev-inquiries.json</c>), so a Seeker's Inbox survives a restart. When
/// <see cref="DevSamples.Enabled"/>, the pro's own Inbox also gets SAMPLE inquiries.
/// </summary>
public sealed class StubInquiryRepository(ILogger<StubInquiryRepository> logger) : IInquiryRepository
{
    private readonly JsonFile<List<Inquiry>> _file = new("dev-inquiries.json", DevJsonContext.Default.ListInquiry, logger);
    private List<Inquiry>? _inquiries;

    public async Task RecordAsync(Inquiry inquiry)
    {
        var inquiries = await AllAsync();
        inquiries.Add(inquiry);
        await _file.WriteAsync(inquiries);
    }

    public async Task<IReadOnlyList<Inquiry>> GetForProfessionalAsync(string professionalId)
    {
        var samples = DevSamples.Enabled && professionalId == ListingMapper.OwnListingId
            ? Samples(professionalId)
            : Enumerable.Empty<Inquiry>();
        return (await AllAsync())
            .Where(i => i.ProfessionalId == professionalId)
            .Concat(samples)
            .OrderByDescending(i => i.At)
            .ToList();
    }

    public async Task<IReadOnlyList<Inquiry>> GetForSeekerAsync(string seekerId) =>
        (await AllAsync())
            .Where(i => i.SeekerId == seekerId)
            .OrderBy(i => i.At)
            .ToList();

    private async Task<List<Inquiry>> AllAsync() => _inquiries ??= await _file.ReadAsync() ?? [];

    private static IEnumerable<Inquiry> Samples(string professionalId)
    {
        var today = DateTimeOffset.Now.Date;
        return
        [
            new("sample-1", professionalId, "sample-seeker-1", InquiryChannel.Call, DateTimeOffset.Now.AddMinutes(-25), null,
                "aircon-cleaning", "taytay", ReferenceData.UrgencyToday, "500-1000"),
            new("sample-2", professionalId, "sample-seeker-2", InquiryChannel.Text, DateTimeOffset.Now.AddHours(-3),
                "Hi po! Nakita ko kayo sa SideGigPh. Kailangan ko po: Hindi lumalamig / pa-repair ng aircon, dito sa Angono, ngayong linggo. Available po ba kayo?",
                "aircon-repair", "angono", ReferenceData.UrgencyThisWeek, ReferenceData.Unsure),
            new("sample-3", professionalId, "sample-seeker-3", InquiryChannel.Text, today.AddDays(-2).AddHours(10),
                "Hi po! Nakita ko kayo sa SideGigPh. Kailangan ko po: Pa-freon ng aircon, dito sa Taytay. Magkano po ang singil ninyo?",
                "aircon-freon", "taytay", ReferenceData.UrgencyCanvassing, "1000-2000"),
        ];
    }
}
