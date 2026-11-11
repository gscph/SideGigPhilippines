using Microsoft.Extensions.Logging;
using Plugin.Firebase.Firestore;

namespace SideGigPh.Maui.Services.Firestore;

/// <summary>The collection names and the few helpers every Firestore repository shares.</summary>
internal static class FirestoreAccess
{
    public const string Users = "users";
    public const string Professionals = "professionals";
    public const string Reviews = "reviews";
    public const string Chats = "chats";
    public const string Messages = "messages";

    // Firestore allows 500 writes in one batch; stay well under.
    public const int BatchSize = 400;

    // `in` queries take at most 30 values.
    public const int InQueryLimit = 30;

    /// <summary>
    /// The native SDK. Looked up when used, not when the repository is created: the SDK starts with
    /// the app's activity, which is after the DI container is built.
    /// </summary>
    public static IFirebaseFirestore Db => CrossFirebaseFirestore.Current;

    /// <summary>A read that can't reach the database (offline, rules said no) shows as "nothing there", logged.</summary>
    public static async Task<T> ReadAsync<T>(Func<Task<T>> read, T fallback, ILogger logger, string what)
    {
        try
        {
            return await read();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Firestore: couldn't read {What}.", what);
            return fallback;
        }
    }

    /// <summary>"Never read" is stored as the Unix epoch, so the model sees null.</summary>
    public static DateTimeOffset? ReadTime(DateTimeOffset stored) => stored <= DateTimeOffset.UnixEpoch ? null : stored;

    public static DateTimeOffset WriteTime(DateTimeOffset? time) => time ?? DateTimeOffset.UnixEpoch;

    /// <summary>Text the rules will accept as a name: 1 to 60 characters.</summary>
    public static string Name(string? value)
    {
        var text = value?.Trim();
        return string.IsNullOrEmpty(text) ? "—" : text.Length > 60 ? text[..60] : text;
    }

    public static string? Limit(string? value, int maxLength) =>
        value is null ? null : value.Length > maxLength ? value[..maxLength] : value;
}
