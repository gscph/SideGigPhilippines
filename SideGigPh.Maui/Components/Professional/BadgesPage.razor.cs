using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Shared;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The pro's badges, under Me: a government ID and an NBI or police clearance. Each line says how it stands and opens <see cref="BadgeProofPage"/> when a proof can be sent.
/// </summary>
public partial class BadgesPage : ComponentBase, IDisposable
{
    [Inject] private ProBadges Badges { get; set; } = default!;
    [Inject] private AppText T { get; set; } = default!;

    private sealed record Row(string Icon, string Title, string Note, BadgeState State, string? Href);

    private IReadOnlyList<Row> Rows =>
    [
        One(BadgeKind.Id, Copy.BadgeIdTitle, Copy.BadgeIdHint),
        One(BadgeKind.Clearance, Copy.BadgeClearanceTitle, Copy.BadgeClearanceHint),
    ];

    protected override async Task OnInitializedAsync()
    {
        Badges.Changed += OnChanged;
        await Badges.LoadAsync();
    }

    public void Dispose() => Badges.Changed -= OnChanged;

    private void OnChanged() => InvokeAsync(StateHasChanged);

    /// <summary>The badge's line: what to send, or how the request stands.</summary>
    private Row One(BadgeKind kind, LocalizedText title, LocalizedText hint) =>
        Badges.Of(kind) is { } request
            ? Sent(request) with { Title = request.State == BadgeState.Approved ? T.Badge(request.Badge) : T.Get(title) }
            : new Row(BadgeIcons.Of(kind), T.Get(title), T.Get(hint), BadgeState.None, ProRoutes.BadgeProof(kind));

    /// <summary>
    /// A request already sent. A rejected or expired one can be sent again from its line; an approved clearance says
    /// until when it shows.
    /// </summary>
    private Row Sent(BadgeRequest request) => new(
        BadgeIcons.Of(request.Badge.Kind),
        T.Badge(request.Badge),
        request.State switch
        {
            BadgeState.Approved when request.ValidUntil is { } until => T.Format(Copy.BadgeValidUntilNote, BadgeDate(until)),
            BadgeState.Approved => T.BadgeNote(request.Badge),
            BadgeState.Expired => T.Format(Copy.BadgeExpiredNote, BadgeDate(request.ValidUntil)),
            BadgeState.Rejected => string.IsNullOrWhiteSpace(request.RejectReason)
                ? T.Get(Copy.BadgeRejectedNoReason)
                : T.Format(Copy.BadgeRejectedNote, request.RejectReason.Trim().TrimEnd('.')),
            _ => T.Get(Copy.BadgePendingNote),
        },
        request.State,
        request.State is BadgeState.Rejected or BadgeState.Expired ? ProRoutes.BadgeProof(request.Badge.Kind) : null);

    /// <summary>A clearance's last day as people write it ("3 Mar 2027"), the same in both languages.</summary>
    private static string BadgeDate(DateOnly? day) => day?.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
}
