using Microsoft.Extensions.Logging;
using AndroidLog = Android.Util.Log;

namespace SideGigPh.Maui;

/// <summary>
/// Writes the app's <see cref="ILogger"/> output to Android logcat under the tag <see cref="Tag"/>,
/// with the full exception (type + stack). The Debug logger only prints while a debugger is
/// attached, so without this an API or picker failure leaves no trace. Read it with
/// <c>adb logcat -s SideGigPh</c>.
/// </summary>
public sealed class LogcatLoggerProvider : ILoggerProvider
{
    public const string Tag = "SideGigPh";

    public ILogger CreateLogger(string categoryName) => new LogcatLogger(categoryName);

    public void Dispose()
    {
    }

    private sealed class LogcatLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = $"{category}: {formatter(state, exception)}";
            if (exception is not null)
            {
                message += Environment.NewLine + exception;
            }

            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    AndroidLog.Error(Tag, message);
                    break;
                case LogLevel.Warning:
                    AndroidLog.Warn(Tag, message);
                    break;
                default:
                    AndroidLog.Info(Tag, message);
                    break;
            }
        }
    }
}
