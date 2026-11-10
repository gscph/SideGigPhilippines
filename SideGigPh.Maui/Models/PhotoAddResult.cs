namespace SideGigPh.Maui.Models;

/// <summary>
/// What happened when the pro picked photos. <see cref="Duplicates"/> counts photos skipped
/// because they were already added (anti-fraud rule 2: SHA-256 duplicate check).
/// </summary>
public sealed record PhotoAddResult(int Added, int Duplicates, bool Failed)
{
    public static PhotoAddResult Failure { get; } = new(0, 0, true);
}
