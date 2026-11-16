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
    /// <param name="canSkip">
    /// Show "Do this later", which goes on to <paramref name="then"/> without a number: for an early ask that a later
    /// page asks again, so a code that never arrives is never a dead end.
    /// </param>
    /// <param name="later">
    /// Show "Do this later", which goes here without a number: for a page that needs the number (<paramref name="then"/>
    /// would only ask again), so someone whose code never comes, or who used up today's codes, can leave and come back.
    /// </param>
    public static string AddPhone(string? then, string? back = null, bool canSkip = false, string? later = null)
    {
        var path = WithReturn(PhonePath, then);
        path = WithQuery(path, "back", back);
        path = WithQuery(path, "later", later);
        return canSkip && SafeReturn(then) is not null ? $"{path}{(path.Contains('?') ? '&' : '?')}skip=true" : path;
    }

    private static string WithQuery(string path, string name, string? value) =>
        SafeReturn(value) is { } safe ? $"{path}{(path.Contains('?') ? '&' : '?')}{name}={Uri.EscapeDataString(safe)}" : path;

    /// <summary><paramref name="then"/> if it's a path inside the app, else null.</summary>
    public static string? SafeReturn(string? then) =>
        then is { Length: > 0 } && then[0] == '/' && !then.StartsWith("//") && !then.Contains('\\') ? then : null;

    private static string WithReturn(string path, string? then) =>
        SafeReturn(then) is { } safe ? $"{path}?then={Uri.EscapeDataString(safe)}" : path;
}
