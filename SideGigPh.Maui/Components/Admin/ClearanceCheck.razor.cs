using Microsoft.AspNetCore.Components;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Admin;

/// <summary>
/// The operator's clearance check on a badge request: what to look for (the 1 year / 6 months rule among it), what the
/// server read from the QR code (an NBI or PNP address to open, or a warning when it isn't one) and the "valid until"
/// date, read off the paper, which approving needs and after which the badge lapses (<see cref="ClearanceRules"/>).
/// </summary>
public partial class ClearanceCheck : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    /// <summary>The QR code as the server read it, or null when none was found.</summary>
    [Parameter] public ClearanceQr? Qr { get; set; }

    [Parameter] public DateOnly? ValidUntil { get; set; }

    [Parameter] public EventCallback<DateOnly?> ValidUntilChanged { get; set; }

    [Parameter] public bool Disabled { get; set; }

    private static readonly LocalizedText[] Checklist =
    [
        Copy.AdminClearanceCheckPerson,
        Copy.AdminClearanceCheckRecord,
        Copy.AdminClearanceCheckAge,
        Copy.AdminClearanceCheckQr,
    ];

    private static DateOnly Today => JobPostRules.PhilippineToday(DateTimeOffset.UtcNow);

    private static DateTime First => Today.ToDateTime(TimeOnly.MinValue);

    private static DateTime Last => Today.AddMonths(ClearanceRules.NbiValidMonths).AddDays(7).ToDateTime(TimeOnly.MinValue);

    private DateTime? Picked => ValidUntil?.ToDateTime(TimeOnly.MinValue);

    private bool IsOfficial => Qr?.Link is not null;

    private string QrNote => Qr switch
    {
        null => T.Get(Copy.AdminClearanceNoQr),
        { Issuer: ClearanceIssuer.Nbi } => T.Get(Copy.AdminClearanceQrNbi),
        { Issuer: ClearanceIssuer.Police } => T.Get(Copy.AdminClearanceQrPolice),
        _ => T.Get(Copy.AdminClearanceQrNotOfficial),
    };

    private Task PickAsync(DateTime? date) => ValidUntilChanged.InvokeAsync(date is { } day ? DateOnly.FromDateTime(day) : null);
}
