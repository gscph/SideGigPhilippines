using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>How far one part of the worker's profile is, from "needed to publish" to "as good as it gets".</summary>
public enum ProfileMark
{
    /// <summary>Red: publishing needs it and it isn't there yet.</summary>
    Missing,

    /// <summary>Green: enough to publish.</summary>
    Done,

    /// <summary>Blue: as full as it gets (all 6 work photos, all 4 badges).</summary>
    Full,

    /// <summary>Amber: not needed to publish, but worth doing (work photos and badges: both rank higher in search).</summary>
    Recommended,

    /// <summary>Gray: sent and waiting for a person to check (badges).</summary>
    Waiting,
}

/// <summary>The profile as a whole: red until published, green once live, blue once every part is full.</summary>
public enum ProfileLevel
{
    NotLive,
    Live,
    Complete,
}

/// <summary>The next thing between the worker and a published listing, in the order the setup asks for them.</summary>
public enum ProfileNextStep
{
    Photo,
    Phone,
    Publish,
}

/// <summary>
/// The worker's profile as a game to finish: a mark for each part (photo, work photos, number, badges)
/// and an overall level. Red parts block publishing; green is enough to be live; blue is a fully set-up profile;
/// amber is optional but worth doing: work photos (fewer than <see cref="ProfessionalDraft.GoodWorkPhotos"/>) and a
/// badge not asked for yet. Neither blocks publishing; both rank higher in search (<c>SearchRanker</c>).
/// </summary>
public sealed record ProfileProgress(
    bool IsPublished,
    ProfileMark Photo,
    ProfileMark WorkPhotos,
    ProfileMark Phone,
    ProfileMark Badges,
    int WorkPhotoCount,
    int BadgeCount)
{
    /// <summary>Red until published; blue when published and every part is full; green in between.</summary>
    public ProfileLevel Level =>
        !IsPublished ? ProfileLevel.NotLive
        : new[] { Photo, WorkPhotos, Phone, Badges }.All(mark => mark == ProfileMark.Full) ? ProfileLevel.Complete
        : ProfileLevel.Live;

    /// <summary>What to do next to get published; null once the listing is live.</summary>
    public ProfileNextStep? NextStep =>
        IsPublished ? null
        : Photo == ProfileMark.Missing ? ProfileNextStep.Photo
        : Phone == ProfileMark.Missing ? ProfileNextStep.Phone
        : ProfileNextStep.Publish;

    /// <summary>Live, with fewer than <see cref="ProfessionalDraft.GoodWorkPhotos"/> work photos: the "be seen more" reminder shows.</summary>
    public bool ShowWorkPhotosNudge => IsPublished && WorkPhotos == ProfileMark.Recommended;

    /// <summary>True while a badge can still be asked for: the amber reminder shows.</summary>
    public bool ShowBadgeNudge => Badges == ProfileMark.Recommended;

    public static ProfileProgress From(
        bool isPublished,
        bool hasPhoto,
        int workPhotoCount,
        bool hasVerifiedPhone,
        IEnumerable<BadgeState> badgeStates)
    {
        var states = badgeStates.ToList();
        var approved = states.Count(state => state == BadgeState.Approved);
        return new ProfileProgress(
            isPublished,
            Photo: hasPhoto ? ProfileMark.Full : ProfileMark.Missing,
            WorkPhotos: workPhotoCount >= ProWorkspace.MaxWorkPhotos ? ProfileMark.Full
                : workPhotoCount >= ProfessionalDraft.GoodWorkPhotos ? ProfileMark.Done
                : ProfileMark.Recommended,
            Phone: hasVerifiedPhone ? ProfileMark.Full : ProfileMark.Missing,
            Badges: approved >= ProBadges.Kinds ? ProfileMark.Full
                : states.Count < ProBadges.Kinds || states.Any(state => state is BadgeState.None or BadgeState.Rejected or BadgeState.Expired) ? ProfileMark.Recommended
                : ProfileMark.Waiting,
            WorkPhotoCount: workPhotoCount,
            BadgeCount: approved);
    }

    /// <summary>The worker's profile as it stands now.</summary>
    public static ProfileProgress Of(ProWorkspace workspace, ProBadges badges) => From(
        workspace.IsPublished,
        workspace.ProfilePhotoUrl is not null,
        workspace.WorkPhotos.Count,
        workspace.HasVerifiedPhone,
        Enum.GetValues<BadgeKind>().Select(badges.StateOf));
}
