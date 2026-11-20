using FluentAssertions;
using MobileAppTheme.Auth;

namespace SideGigPh.Tests.Unit.PhoneNumbers;

/// <summary>
/// An SMS code can be late or never come, so the code step offers Google / Apple again after a minute without one, or
/// at once when today's codes are used up: nobody is stuck waiting on a code.
/// </summary>
public class CodeFallbackTests
{
    [Fact]
    public void Right_after_the_code_is_sent_only_the_code_is_asked_for() =>
        CodeFallback.ShowOtherSignIn(TimeSpan.FromSeconds(5), codesUsedUp: false).Should().BeFalse();

    [Fact]
    public void Just_before_a_minute_it_still_waits() =>
        CodeFallback.ShowOtherSignIn(CodeFallback.ShowAfter - TimeSpan.FromSeconds(1), codesUsedUp: false).Should().BeFalse();

    [Fact]
    public void After_a_minute_without_a_code_the_other_ways_in_show() =>
        CodeFallback.ShowOtherSignIn(CodeFallback.ShowAfter, codesUsedUp: false).Should().BeTrue();

    [Fact]
    public void With_todays_codes_used_up_they_show_at_once() =>
        CodeFallback.ShowOtherSignIn(TimeSpan.Zero, codesUsedUp: true).Should().BeTrue();
}
