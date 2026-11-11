using System.Diagnostics.CodeAnalysis;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The sign-in screen reads Philippine mobile numbers the way people type them (0917…, 917…,
/// +63 917…) through <see cref="PhilippineMobile"/>.
/// </summary>
public sealed class PhilippinePhoneNumberFormat : IPhoneNumberFormat
{
    public string Placeholder => "0917 123 4567";

    public string Region => "Philippines (+63)";

    public bool TryNormalize(string? input, [NotNullWhen(true)] out string? e164) =>
        PhilippineMobile.TryNormalize(input, out e164);
}
