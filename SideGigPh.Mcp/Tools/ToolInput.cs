namespace SideGigPh.Mcp.Tools;

/// <summary>What a client may ask for, kept in range: a window in days, a row count, an optional filter.</summary>
public static class ToolInput
{
    /// <summary>The most rows one answer carries.</summary>
    public const int MaxRows = 200;

    public static int Days(int days, int max = 365) => Math.Clamp(days, 1, max);

    public static int Top(int top) => Math.Clamp(top, 1, MaxRows);

    /// <summary>An optional filter: null when left empty, else trimmed and lower case (ids and kinds are).</summary>
    public static string? Filter(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim().ToLowerInvariant();
}
