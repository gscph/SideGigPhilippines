using System.Text.RegularExpressions;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary>Between the app's models and the API's contracts. Only <c>Services/Api/</c> uses the contracts.</summary>
internal static partial class ApiMapping
{
    public static ReportRequest ToDto(this UserReport report) => new(
        report.PersonId,
        report.Subject switch
        {
            ReportSubject.Listing => ReportKinds.Listing,
            ReportSubject.Review => ReportKinds.Review,
            ReportSubject.Chat => ReportKinds.Chat,
            _ => ReportKinds.User,
        },
        report.Reason switch
        {
            ReportReason.Scam => ReportReasons.Scam,
            ReportReason.Harassment => ReportReasons.Harassment,
            ReportReason.Fake => ReportReasons.Fake,
            ReportReason.Inappropriate => ReportReasons.Inappropriate,
            ReportReason.Employment => ReportReasons.Employment,
            _ => ReportReasons.Other,
        },
        report.ChatId,
        report.Note,
        report.AlsoBlock);

    public static FeedbackPost ToModel(this FeedbackPostDto dto) => new(
        dto.Id,
        dto.Title,
        dto.Details,
        dto.Votes,
        dto.Status switch
        {
            FeedbackStatuses.Planned => FeedbackStatus.Planned,
            FeedbackStatuses.Done => FeedbackStatus.Done,
            _ => FeedbackStatus.Open,
        },
        dto.VotedByMe,
        dto.Mine,
        dto.CreatedAt);

    public static FeedbackPage ToModel(this FeedbackPageDto dto) => new([.. dto.Posts.Select(post => post.ToModel())], dto.HasMore);

    public static NewFeedbackRequest ToFeedbackRequest(string title, string? details, UserRole? role) => new(
        title,
        details,
        role switch { UserRole.Seeker => Wire.Seeker, UserRole.Professional => Wire.Professional, _ => null });

    public static JobPost ToModel(this JobPostDto dto) => new(
        dto.Id,
        dto.CategoryId,
        dto.ServiceId,
        dto.Description,
        dto.BudgetPesos,
        dto.CityId,
        [.. dto.Photos.Select(photo => photo.Url)],
        IsOpen: dto.Status == JobPostStatuses.Open,
        dto.Mine,
        dto.OffersService,
        dto.DistanceKm,
        dto.CreatedAt,
        dto.OfferCount,
        dto.MyOfferPesos,
        dto.PickedProId,
        dto.BudgetMinPesos,
        dto.HouseholdReviews is { } stars ? new ReviewSummary(stars.Average, stars.Count) : null,
        dto.BarangayId);

    public static PickedJob ToModel(this PickedJobDto dto) =>
        new(dto.PostId, dto.ServiceId, dto.PricePesos, dto.PickedAt, dto.IAmTheHousehold, dto.DoneByHousehold, dto.DoneByWorker, dto.MyRating);

    public static JobOffer ToModel(this JobOfferDto dto) => new(
        dto.Id,
        dto.PostId,
        dto.Pro.ToModel(),
        dto.PricePesos,
        dto.Note,
        IsPicked: dto.Status == JobOfferStatuses.Picked,
        dto.TakesBudget,
        dto.DistanceKm,
        dto.CreatedAt);

    public static MyOffer ToModel(this MyOfferDto dto) => new(
        dto.Post.ToModel(),
        dto.PricePesos,
        dto.Note,
        dto.State switch
        {
            MyOfferStates.Picked => MyOfferState.Picked,
            MyOfferStates.NotPicked => MyOfferState.NotPicked,
            _ => MyOfferState.Waiting,
        },
        dto.SentAt,
        dto.ChatId);

    public static JobPostFeed ToModel(this JobPostFeedDto dto) => new([.. dto.Posts.Select(post => post.ToModel())], dto.HasMore, dto.HasListing);

    public static NewJobPostRequest ToRequest(this JobPostDraft draft) => new(
        draft.CategoryId ?? string.Empty,
        draft.ServiceId ?? string.Empty,
        draft.Description?.Trim() ?? string.Empty,
        draft.BudgetPesos ?? 0,
        draft.CityId ?? string.Empty,
        [.. draft.Photos.Select(photo => photo.Id)],
        draft.Point?.Latitude,
        draft.Point?.Longitude,
        BudgetMinPesos: draft.BudgetMinPesos,
        BarangayId: ReferenceData.FindBarangay(draft.CityId, draft.BarangayId)?.Id);

    public static UserProfile ToModel(this UserDto dto) => new(
        dto.Uid,
        dto.DisplayName,
        dto.PhoneNumber,
        dto.Email,
        dto.Language == Wire.Taglish ? AppLanguage.Taglish : AppLanguage.English,
        dto.Role switch { Wire.Seeker => UserRole.Seeker, Wire.Professional => UserRole.Professional, _ => null },
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
        null, // IncomeGoalId: no longer asked (older apps still send theirs)
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
        OtherWork = dto.OtherWork,
        MobileNumber = dto.MobileNumber,
        PhotoUrl = dto.ProfilePhoto?.Url,
        WorkPhotoUrls = dto.WorkPhotos.Select(photo => photo.Url).ToList(),
        Reviews = dto.Reviews is { } reviews ? new ReviewSummary(reviews.Average, reviews.Count) : null,
        ReferenceCount = dto.ReferenceCount,
        Badges = [.. (dto.Badges ?? []).Select(ToModel).OfType<Badge>()],
        CompletedJobs = dto.CompletedJobs,
    };

    /// <summary>A badge on a listing, or null for a kind this app version doesn't know yet.</summary>
    public static Badge? ToModel(this BadgeDto dto) => KindOf(dto.Kind) is { } kind
        ? new Badge(kind, kind == BadgeKind.Project ? dto.BadgeId : null, dto.CompanyName, dto.ProjectName)
        : null;

    /// <summary>A request waiting for the operator, or null for a kind this app version doesn't know yet.</summary>
    public static PendingBadge? ToModel(this AdminVerificationDto dto) => KindOf(dto.Kind) is { } kind
        ? new PendingBadge(
            dto.Id,
            dto.Uid,
            dto.WorkerName ?? string.Empty,
            dto.ProfilePhotoUrl,
            new Badge(kind, kind == BadgeKind.Project ? dto.BadgeId : null, dto.CompanyName, dto.ProjectName),
            new BadgeScan(
                dto.ScanStatus switch
                {
                    ScanStatuses.Read => BadgeScanResult.Read,
                    ScanStatuses.Unreadable => BadgeScanResult.Unreadable,
                    ScanStatuses.Failed => BadgeScanResult.Failed,
                    _ => BadgeScanResult.None,
                },
                dto.ScannedType, dto.ScannedName, dto.ScannedNumber, dto.ScannedExpiry),
            dto.HasDocument,
            dto.CreatedAt)
        : null;

    /// <summary>One of the worker's own requests, or null for a kind this app version doesn't know yet.</summary>
    public static BadgeRequest? ToModel(this VerificationDto dto) => KindOf(dto.Kind) is { } kind
        ? new BadgeRequest(
            dto.Id,
            new Badge(kind, kind == BadgeKind.Project ? dto.BadgeId : null, dto.CompanyName, dto.ProjectName),
            dto.Status switch
            {
                VerificationStatuses.Approved => BadgeState.Approved,
                VerificationStatuses.Rejected => BadgeState.Rejected,
                _ => BadgeState.Pending,
            },
            dto.RejectReason,
            dto.CreatedAt)
        : null;

    /// <summary>
    /// <c>verifications/{kind}?consent=true…</c> for sending a proof. <c>consent=true</c> because the port is only
    /// called after the worker ticked the consent box (<see cref="ProBadges.SubmitAsync"/>).
    /// </summary>
    public static string ToSubmitPath(Badge badge)
    {
        var kind = badge.Kind switch
        {
            BadgeKind.Tesda => VerificationKinds.Tesda,
            BadgeKind.Project => VerificationKinds.Developer,
            _ => VerificationKinds.Id,
        };
        var path = $"verifications/{kind}?consent=true";
        if (badge.Kind != BadgeKind.Project)
        {
            return path;
        }

        path += $"&badgeId={ApiClient.Escape(badge.DeveloperId ?? string.Empty)}";
        if (!string.IsNullOrWhiteSpace(badge.CompanyName))
        {
            path += $"&companyName={ApiClient.Escape(badge.CompanyName)}";
        }

        return string.IsNullOrWhiteSpace(badge.ProjectName) ? path : path + $"&projectName={ApiClient.Escape(badge.ProjectName)}";
    }

    private static BadgeKind? KindOf(string kind) => kind switch
    {
        VerificationKinds.Id => BadgeKind.Id,
        VerificationKinds.Tesda => BadgeKind.Tesda,
        VerificationKinds.Developer => BadgeKind.Project,
        _ => null,
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
        references.Select(r => new ReferenceDto(r.Name, r.MobileNumber)).ToList(),
        listing.BasePoint?.Latitude,
        listing.BasePoint?.Longitude,
        listing.RadiusKm,
        OtherWork: listing.ServiceIds.Any(ReferenceData.IsOtherJob) ? listing.OtherWork : null);

    public static Review ToModel(this ReviewDto dto) => new(
        Chat.IdFor(dto.ProfessionalId, dto.SeekerId),
        dto.ProfessionalId,
        dto.SeekerId,
        dto.ReviewerName,
        dto.Rating,
        dto.Comment,
        dto.ServiceId,
        dto.UpdatedAt,
        dto.VerifiedJob);

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

    // The pictures are the app's own, picked by id (AppImages); the server's Icon (an emoji, for app versions before
    // the pictures) only says whether an answer has one.
    public static AppConfig ToModel(this ConfigDto dto) => new(
        dto.Catalog.Select(c => new CatalogOption(c.Id, new(c.English, c.Taglish), AppImages.For(c.Id), Hint(c.HintEnglish, c.HintTaglish))).ToList(),
        dto.Catalog.ToDictionary(
            c => c.Id,
            c => (IReadOnlyList<CatalogOption>)c.Services.Select(s => new CatalogOption(s.Id, new(s.English, s.Taglish), AppImages.For(s.Id))).ToList()),
        Options(dto.Towns),
        Options(dto.PriceTiers),
        Options(dto.Experience),
        Options(dto.Urgencies),
        Options(dto.IncomeGoals),
        dto.Catalog.SelectMany(c => c.Services)
            .Where(s => s.Price is not null)
            .ToDictionary(s => s.Id, s => new PriceGuide(s.Price!.Min, s.Price.Average, s.Price.High)),
        dto.MinimumWage,
        dto.DefaultTownId,
        // A server before barangays sends none (null): no town has a list then.
        dto.Barangays?.GroupBy(b => b.TownId).ToDictionary(
            town => town.Key,
            town => (IReadOnlyList<CatalogOption>)[.. town.Select(b => new CatalogOption(b.Id, new(b.English, b.Taglish)))]));

    private static IReadOnlyList<CatalogOption> Options(List<OptionDto>? options) =>
        (options ?? []).Select(o => new CatalogOption(o.Id, new(o.English, o.Taglish), o.Icon is null ? null : AppImages.For(o.Id), Amount: o.Amount)).ToList();

    public static OtherJobRequest ToRequest(this OtherJobNote note) =>
        new(note.CategoryId, note.Side == UserRole.Seeker ? Wire.Seeker : Wire.Professional, note.Text);

    public static ErrorReportBatch ToBatch(this IReadOnlyList<ErrorReport> reports, string installId, DeviceStamp device) =>
        new(installId, device.AppVersion, device.CodeVersion, device.Channel, device.Platform, device.Device, [.. reports.Select(r => new ErrorReportDto(
            r.Kind switch { ErrorKind.Crash => ErrorKinds.Crash, ErrorKind.Slow => ErrorKinds.Slow, _ => ErrorKinds.Error },
            r.Source, r.Message, r.Detail, r.Route, r.Trail, r.Count, r.FirstAt, r.LastAt))]);

    private static LocalizedText? Hint(string? english, string? taglish) =>
        english is null && taglish is null ? null : new(english ?? taglish!, taglish ?? english!);

    private static string? PhotoId(string? url) =>
        url is not null && PhotoUrl().Match(url) is { Success: true } match ? match.Groups["id"].Value : null;

    [GeneratedRegex(@"^https://[^/]+/photos/(?<id>[0-9a-f]{64})\.jpg$")]
    private static partial Regex PhotoUrl();
}
