namespace MobileAppTheme.Auth;

/// <summary>
/// The sign-in screens' routes. <c>then</c> is where to go once the step is done; only a path
/// inside the app is accepted (<see cref="SafeReturn"/>).
/// </summary>
public static class AuthRoutes
{
    public const string SignIn = "/verify";

    public const string NamePath = "/verify/name";

    public const string PhonePath = "/verify/phone";

    public static string Name(string? then) => WithReturn(NamePath, then);

    public static string AddPhone(string? then) => WithReturn(PhonePath, then);

    /// <summary><paramref name="then"/> if it's a path inside the app, else null.</summary>
    public static string? SafeReturn(string? then) =>
        then is { Length: > 0 } && then[0] == '/' && !then.StartsWith("//") && !then.Contains('\\') ? then : null;

    private static string WithReturn(string path, string? then) =>
        SafeReturn(then) is { } safe ? $"{path}?then={Uri.EscapeDataString(safe)}" : path;
}
