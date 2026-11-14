using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Onboarding;

/// <summary>
/// The interview's Done never leaves the person on the loading screen (the Galaxy A15 freeze): the app opens when the
/// saves finish, after <see cref="OnboardingFinish.WaitLimit"/> when they hang, and when they fail, and the last two are
/// logged as errors so they reach the error reports.
/// </summary>
public class OnboardingFinishTests
{
    private readonly ManualClock _clock = new();
    private readonly RecordingLogger _logger = new();

    [Fact]
    public async Task SavesInTime_Opens()
    {
        (await OnboardingFinish.WaitAsync(Task.CompletedTask, _logger, _clock)).Should().BeTrue();
        _logger.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task SavesHang_OpensAfterTheLimit_AndLogsAnError()
    {
        var never = new TaskCompletionSource();
        var waiting = OnboardingFinish.WaitAsync(never.Task, _logger, _clock);

        _clock.Advance(OnboardingFinish.WaitLimit - TimeSpan.FromSeconds(1));
        waiting.IsCompleted.Should().BeFalse();
        _clock.Advance(TimeSpan.FromSeconds(1));

        (await waiting).Should().BeFalse();
        _logger.Errors.Should().ContainSingle().Which.Should().Contain("took over");
    }

    [Fact]
    public async Task SavesFail_Opens_AndLogsAnError()
    {
        var failed = Task.FromException(new HttpRequestException("offline"));

        (await OnboardingFinish.WaitAsync(failed, _logger, _clock)).Should().BeFalse();
        _logger.Errors.Should().ContainSingle().Which.Should().Contain("failed");
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<string> Errors { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                Errors.Add(formatter(state, exception));
            }
        }
    }
}
