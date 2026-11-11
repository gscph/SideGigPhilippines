using System.Diagnostics.CodeAnalysis;

namespace MobileAppTheme.Auth;

/// <summary>
/// How the app reads a typed mobile number: the hint shown in the field and the conversion to
/// E.164 (<c>+&lt;country&gt;&lt;number&gt;</c>), which every SMS provider needs. An app for one
/// country registers its own (local formats like <c>0917…</c>); the default,
/// <see cref="E164PhoneNumberFormat"/>, accepts international numbers only.
/// </summary>
public interface IPhoneNumberFormat
{
    /// <summary>Example shown in the empty field.</summary>
    string Placeholder { get; }

    bool TryNormalize(string? input, [NotNullWhen(true)] out string? e164);
}

/// <summary>Default <see cref="IPhoneNumberFormat"/>: a leading <c>+</c> and 8–15 digits; spaces and dashes are ignored.</summary>
public sealed class E164PhoneNumberFormat : IPhoneNumberFormat
{
    public string Placeholder => "+00 000 000 0000";

    public bool TryNormalize(string? input, [NotNullWhen(true)] out string? e164)
    {
        e164 = null;
        var trimmed = input?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed[0] != '+')
        {
            return false;
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length is < 8 or > 15)
        {
            return false;
        }

        e164 = "+" + digits;
        return true;
    }
}
