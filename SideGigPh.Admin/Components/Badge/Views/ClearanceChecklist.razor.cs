using Microsoft.AspNetCore.Components;
using SideGigPh.Contracts;

namespace SideGigPh.Admin.Components.Badge.Views;

/// <summary>
/// A clearance request's check, like the app's <c>ClearanceCheck</c>: what to look for (the 1 year / 6 months rule
/// among it), what the server read from the QR code (a link when it is an NBI or PNP address, a warning when not) and
/// the "valid until" date read off the paper, which approving needs; the badge lapses after it.
/// </summary>
public partial class ClearanceChecklist
{
    /// <summary>The QR code's text as the server read it, or null when none was found.</summary>
    [Parameter] public string? Qr { get; set; }

    [Parameter] public DateOnly? ValidUntil { get; set; }

    [Parameter] public EventCallback<DateOnly?> ValidUntilChanged { get; set; }

    [Parameter] public bool Disabled { get; set; }

    private Uri? Link => ClearanceRules.OfficialLink(Qr);

    private static DateOnly Today => JobPostRules.PhilippineToday(DateTimeOffset.UtcNow);

    private static DateTime First => Today.ToDateTime(TimeOnly.MinValue);

    private static DateTime Last => Today.AddMonths(ClearanceRules.NbiValidMonths).AddDays(7).ToDateTime(TimeOnly.MinValue);

    private DateTime? Picked => ValidUntil?.ToDateTime(TimeOnly.MinValue);

    private Task PickAsync(DateTime? date) => ValidUntilChanged.InvokeAsync(date is { } day ? DateOnly.FromDateTime(day) : null);
}
