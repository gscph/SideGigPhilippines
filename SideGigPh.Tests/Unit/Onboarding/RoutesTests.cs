using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;

namespace SideGigPh.Tests.Unit.Onboarding;

/// <summary>
/// Links to a worker, a chat or a review page carry ids from the server. An id with odd characters must still make
/// a working link (escaped), or the page opens "Not Found".
/// </summary>
public class RoutesTests
{
    [Fact]
    public void WorkerSideLinks_EscapeTheChatId() =>
        ProRoutes.Chat("pro 1_seeker/2").Should().Be("/professional/inbox/pro%201_seeker%2F2");

    [Fact]
    public void SeekerSideLinks_EscapeTheWorkerId()
    {
        SeekerRoutes.Pro("a b").Should().Be("/pro/a%20b");
        SeekerRoutes.Thread("a/b").Should().Be("/inbox/a%2Fb");
        SeekerRoutes.Rate("a b").Should().Be("/me/reviews/rate/a%20b");
    }
}
