using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>Where the phone keeps its error log between launches (one JSON file, <c>FileErrorLogStore</c>).</summary>
public interface IErrorLogStore
{
    /// <summary>The saved log, or null when there is none (or it can't be read).</summary>
    Task<DiagnosticsState?> LoadAsync();

    Task SaveAsync(DiagnosticsState state);
}
