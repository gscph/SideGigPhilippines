using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.PhoneNumbers;

/// <summary>
/// Every SMS code costs money and a person should need one in their lifetime: a new code only 6 minutes after the
/// last, at most 3 in any 24 hours, and the history survives a restart (it lives in the phone's preferences).
/// </summary>
public class SmsSendLimitTests
{
    private readonly ManualClock _clock = new();
    private readonly MemoryHistory _history = new();

    private const string Number = "+639171234567";

    private SmsSendLimit Limit { get; }

    public SmsSendLimitTests() => Limit = new SmsSendLimit(_history, _clock);

    [Fact]
    public void A_first_code_may_be_sent_at_once()
    {
        Limit.WaitLeft().Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void A_new_code_waits_six_minutes()
    {
        Limit.Record(Number);
        _clock.Advance(TimeSpan.FromSeconds(60));

        Limit.WaitLeft().Should().Be(TimeSpan.FromSeconds(300));

        _clock.Advance(TimeSpan.FromSeconds(300));
        Limit.WaitLeft().Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void A_fixed_typo_gets_its_code_at_once_but_counts_toward_the_day()
    {
        Limit.Record(Number);

        Limit.WaitLeft("+639181234567").Should().Be(TimeSpan.Zero);
        Limit.WaitLeft(Number).Should().Be(SmsSendLimit.Wait);

        Limit.Record("+639181234567");
        Limit.Record("+639191234567");
        Limit.WaitLeft("+639201234567").Should().BeNull();
    }

    [Fact]
    public void Three_codes_use_up_the_day_and_the_oldest_frees_one_after_24_hours()
    {
        for (var i = 0; i < SmsSendLimit.PerDay; i++)
        {
            Limit.Record(Number);
            _clock.Advance(SmsSendLimit.Wait);
        }

        Limit.WaitLeft().Should().BeNull();

        _clock.Advance(TimeSpan.FromHours(24) - 3 * SmsSendLimit.Wait + TimeSpan.FromSeconds(1));
        Limit.WaitLeft().Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void The_wait_survives_a_restart_and_old_sends_are_forgotten()
    {
        _history.Save([_clock.GetUtcNow() - TimeSpan.FromDays(3)]);
        new SmsSendLimit(_history, _clock).Record(Number);

        _history.Load().Should().ContainSingle();
        new SmsSendLimit(_history, _clock).WaitLeft().Should().Be(SmsSendLimit.Wait);
        new SmsSendLimit(_history, _clock).WaitLeft("+639181234567").Should().Be(SmsSendLimit.Wait, "after a restart the last number is unknown");
    }

    [Fact]
    public void A_send_that_timed_out_while_online_counts_since_the_sms_may_still_come()
    {
        SmsSendLimit.FailedSendCounts(new TimeoutException(), online: true).Should().BeTrue();
    }

    [Fact]
    public void A_send_that_timed_out_with_no_network_does_not_count()
    {
        SmsSendLimit.FailedSendCounts(new TimeoutException(), online: false).Should().BeFalse("nothing went out from a phone with no network");
    }

    [Fact]
    public void A_send_the_provider_refused_does_not_count()
    {
        SmsSendLimit.FailedSendCounts(new AuthException(AuthError.Network, "network"), online: true).Should().BeFalse();
        SmsSendLimit.FailedSendCounts(new AuthException(AuthError.TooManyAttempts, "too many"), online: true).Should().BeFalse();
    }

    [Theory]
    [InlineData(AuthError.Network)]
    [InlineData(AuthError.TooManyAttempts)]
    public void A_failed_send_shows_the_adapters_own_reason(AuthError error)
    {
        AuthException.ErrorOf(new AuthException(error, "x")).Should().Be(error);
    }

    [Fact]
    public void A_failed_send_with_no_answer_is_a_network_error_not_sign_in_failed()
    {
        AuthException.ErrorOf(new TimeoutException()).Should().Be(AuthError.Network);
        AuthException.ErrorOf(new HttpRequestException("No such host")).Should().Be(AuthError.Network);
        AuthException.ErrorOf(new InvalidOperationException("bug")).Should().Be(AuthError.Unknown);
    }

    private sealed class MemoryHistory : ISmsSendHistory
    {
        private IReadOnlyList<DateTimeOffset> _sends = [];

        public IReadOnlyList<DateTimeOffset> Load() => _sends;

        public void Save(IReadOnlyList<DateTimeOffset> sends) => _sends = sends;
    }
}
