using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Diagnostics;

/// <summary>
/// Feeds the app's <see cref="ILogger"/> output into the <see cref="ErrorLog"/>: every error (and critical) becomes a
/// report, and the app's own information and warnings (categories starting <c>SideGigPh</c> or <c>MobileAppTheme</c>)
/// become trail lines, so a report shows what led up to it. Registered as an <see cref="ILoggerProvider"/> service,
/// which the logging system picks up by itself.
/// </summary>
public sealed class ErrorLogLoggerProvider(ErrorLog log) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ErrorLogLogger(log, categoryName);

    public void Dispose()
    {
    }

    /// <summary>The app's own code, whose information lines are worth keeping in the trail.</summary>
    internal static bool IsAppCategory(string category) =>
        category.StartsWith("SideGigPh", StringComparison.Ordinal) || category.StartsWith("MobileAppTheme", StringComparison.Ordinal);

    private sealed class ErrorLogLogger(ErrorLog log, string category) : ILogger
    {
        private readonly bool _isApp = IsAppCategory(category);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel != LogLevel.None && (logLevel >= LogLevel.Error || (_isApp && logLevel >= LogLevel.Information));

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (logLevel >= LogLevel.Error)
            {
                log.Record(ErrorKind.Error, category, message, exception);
            }
            else
            {
                log.Step($"{logLevel}: {message}");
            }
        }
    }
}
