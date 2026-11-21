using SideGigPh.Maui.Services;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.JobPosts;

/// <summary>
/// Call and Text on the chat pages (<see cref="PickedContact"/>): they show only while a pick links the two, each tap
/// reads the number then and hands it straight to the phone's app, and the page never gets the number.
/// </summary>
public class PickedContactTests
{
    private readonly FakeJobPosts _posts = new();
    private readonly FakeContactLauncher _phone = new();
    private readonly PickedContact _contact;

    public PickedContactTests() => _contact = new PickedContact(_posts, _phone);

    [Fact]
    public async Task NoPick_NoCallOrText_AndNothingOpens()
    {
        (await _contact.IsOpenAsync("worker")).Should().BeFalse();
        (await _contact.CallAsync("worker")).Should().Be(ContactOutcome.NotLinked);
        (await _contact.TextAsync("worker", "Hi")).Should().Be(ContactOutcome.NotLinked);
        _phone.Called.Should().BeEmpty();
        _phone.Texted.Should().BeEmpty();
    }

    [Fact]
    public async Task Picked_CallAndTextOpenThePhonesApps_OnTheirNumber()
    {
        _posts.Numbers["worker"] = TestData.WorkerNumber;

        (await _contact.IsOpenAsync("worker")).Should().BeTrue();
        (await _contact.CallAsync("worker")).Should().Be(ContactOutcome.Opened);
        (await _contact.TextAsync("worker", "Hi po")).Should().Be(ContactOutcome.Opened);

        _phone.Called.Should().Equal(TestData.WorkerNumber);
        _phone.Texted.Should().Equal((TestData.WorkerNumber, "Hi po"));
    }

    [Fact]
    public async Task PickCancelledSinceThePageOpened_TheTapOpensNothing()
    {
        _posts.Numbers["worker"] = TestData.WorkerNumber;
        (await _contact.IsOpenAsync("worker")).Should().BeTrue();

        _posts.Numbers.Remove("worker"); // cancelled or blocked meanwhile

        (await _contact.CallAsync("worker")).Should().Be(ContactOutcome.NotLinked);
        _phone.Called.Should().BeEmpty();
    }

    [Fact]
    public async Task PhoneAppDoesNotOpen_SaysSo()
    {
        _posts.Numbers["worker"] = TestData.WorkerNumber;
        _phone.Opens = false;

        (await _contact.CallAsync("worker")).Should().Be(ContactOutcome.Failed);
        (await _contact.TextAsync("worker", "Hi")).Should().Be(ContactOutcome.Failed);
    }
}
