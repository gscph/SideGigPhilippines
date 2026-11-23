using System.Text.RegularExpressions;

namespace SideGigPh.Contracts;

/// <summary>
/// The landing page's Early Access sign-up (<c>POST early-access</c>, public). The email is the one the invite goes to:
/// the Gmail on the Play Store for an Android phone (the Play testing list), the Apple ID for an iPhone (TestFlight).
/// <see cref="Website"/> is the hidden bot trap: people never see it, so a filled one is a bot (answered "ok", nothing kept).
/// </summary>
public sealed record EarlyAccessRequest(string Email, string Phone, string? Role, string? Language, string? Website = null);

/// <summary>The sign-up rules, shared by the landing page (instant feedback) and the API (the real check).</summary>
public static partial class EarlyAccessRules
{
    /// <summary>The longest address the mail standards allow.</summary>
    public const int MaxEmailLength = 254;

    public const string Android = "android";
    public const string IPhone = "iphone";

    public const string Household = "household";
    public const string Worker = "worker";

    /// <summary>Domains people mistype on a phone keyboard, and what they meant.</summary>
    private static readonly Dictionary<string, string> DomainTypos = new()
    {
        ["gmial.com"] = "gmail.com", ["gmai.com"] = "gmail.com", ["gamil.com"] = "gmail.com", ["gmal.com"] = "gmail.com",
        ["gmail.co"] = "gmail.com", ["gmail.con"] = "gmail.com", ["gmail.cm"] = "gmail.com", ["gmail.om"] = "gmail.com",
        ["gmailcom"] = "gmail.com", ["gnail.com"] = "gmail.com", ["yaho.com"] = "yahoo.com", ["yahoo.con"] = "yahoo.com",
        ["yhoo.com"] = "yahoo.com", ["hotmail.con"] = "hotmail.com", ["hotmal.com"] = "hotmail.com",
        ["outlook.con"] = "outlook.com", ["icloud.con"] = "icloud.com", ["iclod.com"] = "icloud.com",
    };

    /// <summary>Trimmed and lower-cased, so the same person typed twice is one line in the list.</summary>
    public static string Normalize(string? email) => (email ?? "").Trim().ToLowerInvariant();

    /// <summary>A plain shape check (something@something.tld, no spaces); the real proof is the invite reaching it.</summary>
    public static bool IsValidEmail(string? email)
    {
        var e = Normalize(email);
        return e.Length is > 0 and <= MaxEmailLength && EmailShape().IsMatch(e);
    }

    /// <summary>"juan@gmial.com" → "juan@gmail.com"; null when the domain looks fine.</summary>
    public static string? SuggestFix(string? email)
    {
        var e = Normalize(email);
        var at = e.LastIndexOf('@');
        if (at <= 0)
        {
            return null;
        }

        return DomainTypos.TryGetValue(e[(at + 1)..], out var meant) ? e[..(at + 1)] + meant : null;
    }

    /// <summary>"android" or "iphone"; null for anything else (the sign-up needs one).</summary>
    public static string? CleanPhone(string? phone) => phone is Android or IPhone ? phone : null;

    /// <summary>The side the person picked, or null for none or anything unknown (optional).</summary>
    public static string? CleanRole(string? role) => role is Household or Worker ? role : null;

    /// <summary>"en" or "tl", or null for anything else.</summary>
    public static string? CleanLanguage(string? language) => language is "en" or "tl" ? language : null;

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$")]
    private static partial Regex EmailShape();
}
