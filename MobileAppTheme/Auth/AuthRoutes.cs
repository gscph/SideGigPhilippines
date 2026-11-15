namespace MobileAppTheme.Auth;

/// <summary>
/// The sign-in screens' routes. <c>then</c> is where to go once the step is done, <c>back</c> where its Back
/// button goes (the app's home when not given); only a path inside the app is accepted (<see cref="SafeReturn"/>).
/// </summary>
public static class AuthRoutes
{
    public const string SignIn = "/verify";

    public const string NamePath = "/verify/name";

    public const string PhonePath = "/verify/phone";

    public static string Name(string? then) => WithReturn(NamePath, then);

    /// <summary>The name page in edit mode: shows the account's current name to change, then returns to <paramref name="then"/>.</summary>
    public static string EditName(string then) =>
        SafeReturn(then) is { } safe ? $"{NamePath}?edit=true&then={Uri.EscapeDataString(safe)}" : $"{NamePath}?edit=true";

    /// <param name="back">
    /// Where Back goes, for a page that would send them straight here again (the app's home resumes it).
    /// </param>
    public static string AddPhone(string? then, string? back = null)
    {
        var path = WithReturn(PhonePath, then);
        return SafeReturn(back) is { } safe ? $"{path}{(path.Contains('?') ? '&' : '?')}back={Uri.EscapeDataString(safe)}" : path;
    }

    /// <summary><paramref name="then"/> if it's a path inside the app, else null.</summary>
    public static string? SafeReturn(string? then) =>
        then is { Length: > 0 } && then[0] == '/' && !then.StartsWith("//") && !then.Contains('\\') ? then : null;

    private static string WithReturn(string path, string? then) =>
        SafeReturn(then) is { } safe ? $"{path}?then={Uri.EscapeDataString(safe)}" : path;
}
