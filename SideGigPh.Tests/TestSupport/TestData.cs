using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.TestSupport;

/// <summary>Ready-made people, listings and workspaces, so each test only spells out what it is about.</summary>
public static class TestData
{
    public const string WorkerUid = "worker-uid";
    public const string WorkerNumber = "+639171234567";
    public const string ReferenceNumber = "+639181111111";

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
        RateTierId = "500-1000",
        ProfilePhotoId = PhotoId(1),
        WorkPhotoIds = [PhotoId(2), PhotoId(3)],
        References = [new WorkReference("Aling Nena", ReferenceNumber)],
    };

    /// <summary>A Professional who finished the interview, with <paramref name="draft"/> as their listing.</summary>
    public static OnboardingState FinishedPro(ProfessionalDraft draft) => new()
    {
        Language = AppLanguage.Taglish,
        Role = UserRole.Professional,
        CompletedAt = DateTimeOffset.Now,
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
        CompletedAt = DateTimeOffset.Now,
        Seeker = answers,
    };

    /// <summary>A chat between a pro and the test Seeker whose last message came from <paramref name="lastSenderId"/>.</summary>
    /// <summary>A chat whose last message was an hour ago; it started then too unless <paramref name="startedAt"/> says earlier.</summary>
    public static Chat ChatWith(
        string proId, string lastSenderId, DateTimeOffset? seekerReadAt = null, DateTimeOffset? proReadAt = null, DateTimeOffset? startedAt = null)
    {
        var id = Chat.IdFor(proId, SeekerUid);
        var at = DateTimeOffset.Now.AddHours(-1);
        return new Chat(id, proId, SeekerUid, proId, "Maricel D.", "aircon-cleaning", "taytay", ReferenceData.UrgencyToday, "500-1000",
            new ChatMessage("m-" + proId, id, lastSenderId, ChatMessageKind.Text, "Hi po", at), seekerReadAt, proReadAt, startedAt ?? at);
    }

    /// <summary>A search result as the API would send it.</summary>
    public static ProfessionalListing Listing(
        string name,
        string[]? cities = null,
        string[]? services = null,
        string? rate = "500-1000",
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
        RateTierId = rate,
        MobileNumber = WorkerNumber,
        PhotoUrl = photo ? "https://example/p.jpg" : null,
        WorkPhotoUrls = photo ? ["https://example/w.jpg"] : [],
        Reviews = reviews,
    };
}

