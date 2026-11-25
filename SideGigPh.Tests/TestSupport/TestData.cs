using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.TestSupport;

/// <summary>Ready-made people, listings and workspaces, so each test only spells out what it is about.</summary>
public static class TestData
{
    public const string WorkerUid = "worker-uid";
    public const string WorkerNumber = "+639171234567";

    /// <summary>A valid photo id (64 hex characters, like a SHA-256), different for each <paramref name="n"/>.</summary>
    public static string PhotoId(int n) => n.ToString("x").PadLeft(64, '0');

    /// <summary>A signed-in worker with a verified number.</summary>
    public static AuthUser Worker(string? phone = WorkerNumber) =>
        new() { Uid = WorkerUid, DisplayName = "Juan Dela Cruz", PhoneNumber = phone };

    /// <summary>A listing that has every interview answer and the whole setup checklist: ready to publish.</summary>
    public static ProfessionalDraft ReadyDraft() => new()
    {
        CategoryId = "aircon",
        ServiceIds = ["aircon-cleaning", "aircon-repair"],
        AreaId = ReferenceData.ServiceAreaId,
        CityIds = ["taytay", "angono"],
        ExperienceId = "3-10",
        RatePesos = 800,
        ProfilePhotoId = PhotoId(1),
        WorkPhotoIds = [PhotoId(2), PhotoId(3)],
    };

    /// <summary>A Professional who finished the interview, with <paramref name="draft"/> as their listing.</summary>
    public static OnboardingState FinishedPro(ProfessionalDraft draft) => new()
    {
        Language = AppLanguage.Taglish,
        Role = UserRole.Professional,
        ProFinishedAt = DateTimeOffset.Now,
        Professional = draft,
    };

    public const string SeekerUid = "seeker-uid";

    /// <summary>A signed-in Seeker (Google account, so no phone yet).</summary>
    public static AuthUser SeekerUser() => new() { Uid = SeekerUid, DisplayName = "Maricel Dela Cruz" };

    /// <summary>A Seeker who needs an aircon cleaned in Taytay today, for ₱500–1,000.</summary>
    public static SeekerAnswers AirconCleaningInTaytay() => new()
    {
        CategoryId = "aircon",
        ServiceId = "aircon-cleaning",
        CityId = "taytay",
        AreaId = ReferenceData.ServiceAreaId,
        UrgencyId = ReferenceData.UrgencyToday,
        BudgetId = "500-1000",
    };

    public static OnboardingState FinishedSeeker(SeekerAnswers answers) => new()
    {
        Language = AppLanguage.English,
        Role = UserRole.Seeker,
        SeekerFinishedAt = DateTimeOffset.Now,
        Seeker = answers,
    };

    /// <summary>
    /// A chat between a pro and the test Seeker whose last message came from <paramref name="lastSenderId"/> an hour ago;
    /// it started then too unless <paramref name="startedAt"/> says earlier.
    /// </summary>
    public static Chat ChatWith(
        string proId, string lastSenderId, DateTimeOffset? seekerReadAt = null, DateTimeOffset? proReadAt = null, DateTimeOffset? startedAt = null)
    {
        var id = Chat.IdFor(proId, SeekerUid);
        var at = DateTimeOffset.Now.AddHours(-1);
        return new Chat(id, proId, SeekerUid, proId, "Maricel D.", "aircon-cleaning", "taytay", ReferenceData.UrgencyToday, "500-1000",
            new ChatMessage("m-" + proId, id, lastSenderId, ChatMessageKind.Text, "Hi po", at), seekerReadAt, proReadAt, startedAt ?? at);
    }

    /// <summary>A listing as the server stores and ranks it (same defaults as <see cref="Listing"/>).</summary>
    public static ListingDto Dto(
        string name,
        string[]? cities = null,
        string[]? services = null,
        int rate = 800,
        string? experience = "1-3",
        ReviewSummary? reviews = null,
        bool photo = true) => new(
        name.ToLowerInvariant(),
        name,
        WorkerNumber,
        "aircon",
        ReferenceData.ServiceAreaId,
        services ?? ["aircon-cleaning"],
        cities ?? ["taytay"],
        experience,
        rate,
        photo ? new PhotoDto("p", "https://example/p.jpg") : null,
        photo ? [new PhotoDto("w", "https://example/w.jpg")] : [],
        reviews is null ? null : new ReviewSummaryDto(reviews.Average, reviews.Count));

    /// <summary>The server's ranking scales, from the app's built-in config (the same lists as <c>004-config.sql</c>).</summary>
    public static RankScales Scales => new(
        ReferenceData.Prices.ToDictionary(p => p.Key, p => new PriceGuideDto(p.Value.Min, p.Value.Average, p.Value.High)),
        [.. ReferenceData.Experience.Select(e => e.Id)],
        ReferenceData.Cities.Select(c => c.Id).ToHashSet());

    /// <summary>The clock ranking tests run at, so activity and tenure don't drift with the real date.</summary>
    public static readonly DateTimeOffset RankNow = new(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

    /// <summary>A worker's app behaviour for ranking; the defaults are an ordinary worker (no chats yet, active yesterday, 3 months in).</summary>
    public static WorkerSignals Signals(
        int received = 0,
        int answered = 0,
        double? replyMinutes = null,
        double activeDaysAgo = 1,
        double joinedDaysAgo = 90) =>
        new(received, answered, replyMinutes, RankNow.AddDays(-activeDaysAgo), RankNow.AddDays(-joinedDaysAgo));

    /// <summary>A listing as the ranker gets it, with <see cref="Signals"/> (ordinary ones unless given).</summary>
    public static RankCandidate Candidate(ListingDto listing, WorkerSignals? signals = null) => new(listing, signals ?? Signals());

    /// <summary>A search result as the API would send it.</summary>
    public static ProfessionalListing Listing(
        string name,
        string[]? cities = null,
        string[]? services = null,
        int? rate = 800,
        string? experience = "1-3",
        ReviewSummary? reviews = null,
        bool photo = true) => new()
    {
        Id = name.ToLowerInvariant(),
        DisplayName = name,
        CategoryIds = ["aircon"],
        ServiceIds = services ?? ["aircon-cleaning"],
        CityIds = cities ?? ["taytay"],
        ExperienceId = experience,
        RatePesos = rate,
        MobileNumber = WorkerNumber,
        PhotoUrl = photo ? "https://example/p.jpg" : null,
        WorkPhotoUrls = photo ? ["https://example/w.jpg"] : [],
        Reviews = reviews,
    };
}

