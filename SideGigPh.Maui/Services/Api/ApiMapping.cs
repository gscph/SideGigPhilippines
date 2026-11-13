using System.Text.RegularExpressions;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>Between the app's models and the API's contracts. Only <c>Services/Api/</c> uses the contracts.</summary>
internal static partial class ApiMapping
{
    public static UserProfile ToModel(this UserDto dto) => new(
        dto.Uid,
        dto.DisplayName,
        dto.PhoneNumber,
        dto.Email,
        dto.Language == Wire.Taglish ? AppLanguage.Taglish : AppLanguage.English,
        dto.Role switch { Wire.Seeker => UserRole.Seeker, Wire.Professional => UserRole.Professional, _ => null },
        dto.IncomeGoalId,
        dto.CreatedAt,
        dto.UpdatedAt,
        dto.ProfilePhotoId);

    public static UserDto ToDto(this UserProfile profile) => new(
        profile.Uid,
        profile.DisplayName,
        profile.PhoneNumber,
        profile.Email,
        profile.Language == AppLanguage.Taglish ? Wire.Taglish : Wire.English,
        profile.Role switch { UserRole.Seeker => Wire.Seeker, UserRole.Professional => Wire.Professional, _ => null },
        profile.IncomeGoalId,
        profile.CreatedAt,
        profile.UpdatedAt,
        profile.ProfilePhotoId);

    public static ProfessionalListing ToModel(this ListingDto dto) => new()
    {
        Id = dto.Id,
        DisplayName = dto.DisplayName,
        CategoryIds = [dto.CategoryId],
        ServiceIds = dto.ServiceIds,
        CityIds = dto.CityIds,
        ExperienceId = dto.ExperienceId,
        RateTierId = dto.RateTierId,
        MobileNumber = dto.MobileNumber,
        PhotoUrl = dto.ProfilePhoto?.Url,
        WorkPhotoUrls = dto.WorkPhotos.Select(photo => photo.Url).ToList(),
        Reviews = dto.Reviews is { } reviews ? new ReviewSummary(reviews.Average, reviews.Count) : null,
        ReferenceCount = dto.ReferenceCount,
    };

    /// <summary>
    /// A page of ranked results that starts at result <paramref name="skip"/> of the whole list. Results run in-town
    /// first, so a result's place in the whole list says its group: the first <c>InTownCount</c> go to the Seeker's town.
    /// </summary>
    public static SearchResult ToModel(this SearchPageDto page, int skip)
    {
        var hits = page.Hits.Select((hit, i) =>
        {
            var pro = hit.Listing.ToModel();
            return (Place: skip + i, Hit: new SearchHit(pro, pro.Reviews, hit.OffersService, hit.ServesTown, hit.FitsBudget));
        }).ToList();
        return new SearchResult(
            [.. hits.Where(h => h.Place < page.InTownCount).Select(h => h.Hit)],
            [.. hits.Where(h => h.Place >= page.InTownCount).Select(h => h.Hit)],
            page.InTownCount,
            page.OtherTownsCount);
    }

    /// <summary>
    /// The listing as the API takes it. Photos go by id, read from their URLs (the file name is the id); a URL that
    /// isn't a stored photo (a leftover on-device data URL) is left out. The number comes from the sign-in token.
    /// </summary>
    public static PublishListingRequest ToPublishRequest(this ProfessionalListing listing, IReadOnlyList<WorkReference> references) => new(
        listing.DisplayName,
        listing.CategoryIds.FirstOrDefault() ?? string.Empty,
        ReferenceData.AreaOf(listing.CityIds.FirstOrDefault()) ?? string.Empty,
        listing.ServiceIds,
        listing.CityIds,
        listing.ExperienceId,
        listing.RateTierId,
        PhotoId(listing.PhotoUrl),
        listing.WorkPhotoUrls.Select(PhotoId).OfType<string>().Take(ProWorkspace.MaxWorkPhotos).ToList(),
        references.Select(r => new ReferenceDto(r.Name, r.MobileNumber)).ToList());

    public static Review ToModel(this ReviewDto dto) => new(
        Chat.IdFor(dto.ProfessionalId, dto.SeekerId),
        dto.ProfessionalId,
        dto.SeekerId,
        dto.ReviewerName,
        dto.Rating,
        dto.Comment,
        dto.ServiceId,
        dto.UpdatedAt);

    public static SaveReviewRequest ToSaveRequest(this Review review) =>
        new(review.ProfessionalId, review.ReviewerName, review.Rating, review.Comment, review.ServiceId);

    public static ChatMessage ToModel(this MessageDto dto) =>
        new(dto.Id, dto.ChatId, dto.SenderId, dto.Kind == Wire.Call ? ChatMessageKind.Call : ChatMessageKind.Text, dto.Text, dto.At);

    public static Chat ToModel(this ChatDto dto) => new(
        dto.Id,
        dto.ProfessionalId,
        dto.SeekerId,
        dto.ProfessionalName,
        dto.SeekerName,
        dto.ServiceId,
        dto.CityId,
        dto.UrgencyId,
        dto.BudgetId,
        dto.LastMessage.ToModel(),
        dto.SeekerReadAt,
        dto.ProfessionalReadAt,
        dto.CreatedAt);

    public static SendMessageRequest ToSendRequest(this Chat chat, ChatMessage message) => new(
        message.Id,
        chat.ProfessionalId,
        chat.SeekerId,
        message.Kind == ChatMessageKind.Call ? Wire.Call : Wire.Text,
        message.Kind == ChatMessageKind.Call ? null : message.Text,
        chat.ProfessionalName,
        chat.SeekerName,
        chat.ServiceId,
        chat.CityId,
        chat.UrgencyId,
        chat.BudgetId);

    public static string ToWire(this ChatSide side) => side == ChatSide.Seeker ? Wire.Seeker : Wire.Professional;

    public static AppConfig ToModel(this ConfigDto dto) => new(
        dto.Catalog.Select(c => new CatalogOption(c.Id, new(c.English, c.Taglish), c.Icon, Hint(c.HintEnglish, c.HintTaglish))).ToList(),
        dto.Catalog.ToDictionary(
            c => c.Id,
            c => (IReadOnlyList<CatalogOption>)c.Services.Select(s => new CatalogOption(s.Id, new(s.English, s.Taglish), s.Icon)).ToList()),
        Options(dto.Towns),
        Options(dto.PriceTiers),
        Options(dto.Experience),
        Options(dto.Urgencies),
        Options(dto.IncomeGoals));

    private static IReadOnlyList<CatalogOption> Options(List<OptionDto>? options) =>
        (options ?? []).Select(o => new CatalogOption(o.Id, new(o.English, o.Taglish), o.Icon, Amount: o.Amount)).ToList();

    public static OtherJobRequest ToRequest(this OtherJobNote note) =>
        new(note.CategoryId, note.Side == UserRole.Seeker ? Wire.Seeker : Wire.Professional, note.Text);

    private static LocalizedText? Hint(string? english, string? taglish) =>
        english is null && taglish is null ? null : new(english ?? taglish!, taglish ?? english!);

    private static string? PhotoId(string? url) =>
        url is not null && PhotoUrl().Match(url) is { Success: true } match ? match.Groups["id"].Value : null;

    [GeneratedRegex(@"^https://[^/]+/photos/(?<id>[0-9a-f]{64})\.jpg$")]
    private static partial Regex PhotoUrl();
}
