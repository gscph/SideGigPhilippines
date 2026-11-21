using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.JobPosts;

/// <summary>
/// The household's exact place for the worker it picked (<see cref="MeetLocation"/>): who sends and who sees it, the
/// straight-line distance and direction the worker reads, the words for it, and the map links.
/// </summary>
public class MeetLocationTests
{
    private static readonly GeoPoint AngonoCentre = new(14.5266, 121.1536);
    private static readonly GeoPoint AntipoloCentre = new(14.5865, 121.1763);

    private static PickedJob Job(bool household, bool completed = false, GeoPoint? place = null) =>
        new(7, "construction-plumbing", 900, DateTimeOffset.UtcNow.AddHours(-1), household,
            DoneByHousehold: completed, DoneByWorker: completed, MeetPoint: place, MeetSharedAt: place is null ? null : DateTimeOffset.UtcNow);

    private static AppText English()
    {
        var text = new AppText();
        text.Use(AppLanguage.English);
        return text;
    }

    [Fact]
    public void OnlyTheHousehold_Sends_AndOnlyThePickedWorker_SeesIt_UntilTheJobIsDone()
    {
        MeetLocation.CanSend(Job(household: true)).Should().BeTrue();
        MeetLocation.CanSend(Job(household: true, place: AngonoCentre)).Should().BeTrue("sending again replaces it");
        MeetLocation.CanSend(Job(household: false)).Should().BeFalse();
        MeetLocation.CanSend(Job(household: true, completed: true)).Should().BeFalse();

        MeetLocation.IsWaiting(Job(household: false)).Should().BeTrue();
        MeetLocation.CanSee(Job(household: false, place: AngonoCentre)).Should().BeTrue();
        MeetLocation.CanSee(Job(household: true, place: AngonoCentre)).Should().BeFalse("the household knows where it lives");
        MeetLocation.CanSee(Job(household: false, completed: true, place: AngonoCentre)).Should().BeFalse();
    }

    [Fact]
    public void TheDistance_IsAStraightLine_AndTheDirection_AnEightPointCompass()
    {
        MeetLocation.DistanceKm(AngonoCentre, AntipoloCentre).Should().BeApproximately(7.0, 0.3);
        MeetLocation.DistanceKm(AngonoCentre, AngonoCentre).Should().Be(0);
        MeetLocation.Direction(AngonoCentre, AntipoloCentre).Should().Be(CompassPoint.North, "Antipolo is mostly north of Angono");
        MeetLocation.Direction(AntipoloCentre, AngonoCentre).Should().Be(CompassPoint.South);
        MeetLocation.Direction(AngonoCentre, new GeoPoint(14.5266, 121.1636)).Should().Be(CompassPoint.East);
        MeetLocation.Direction(AngonoCentre, new GeoPoint(14.5366, 121.1436)).Should().Be(CompassPoint.NorthWest);
    }

    [Fact]
    public void TheDistance_IsRoundedTheWayPeopleSayIt()
    {
        MeetLocation.Rounded(0.01).Should().Be((50, true), "never 0 m");
        MeetLocation.Rounded(0.34).Should().Be((350, true));
        MeetLocation.Rounded(4.24).Should().Be((4.2, false));
        MeetLocation.Rounded(12.6).Should().Be((13, false));
    }

    [Fact]
    public void TheWorkerReads_HowFarAndWhichWay()
    {
        English().MeetDistance(AngonoCentre, AntipoloCentre).Should().Be("About 7.1 km to the north of you, in a straight line.");
        English().MeetDistance(AngonoCentre, new GeoPoint(14.5266, 121.1566)).Should().Be("About 300 m to the east of you, in a straight line.");
    }

    [Fact]
    public void TheMapLinks_TryTheInstalledApps_ThenTheWeb_AndShowAPin()
    {
        var place = new GeoPoint(14.529123456, 121.15);

        MapLinks.Geo(place, "Rosa (customer)").Should().Be("geo:0,0?q=14.529123,121.15(Rosa%20%28customer%29)");
        MapLinks.Waze(place).Should().Be("waze://?ll=14.529123,121.15&navigate=yes");
        MapLinks.GoogleMapsApp(place).Should().Be("comgooglemaps://?daddr=14.529123,121.15&directionsmode=driving");
        MapLinks.Web(place).Should().Be("https://www.google.com/maps/dir/?api=1&destination=14.529123,121.15&travelmode=driving");
        MapLinks.Frame(place).Should().StartWith("https://www.openstreetmap.org/export/embed.html?bbox=121.146,14.525123,121.154,14.533123")
            .And.EndWith("&marker=14.529123,121.15");
    }

    [Fact]
    public async Task TheBoard_SendsThePlace_OnlyFromTheHousehold_AndOnlyInThePhilippines()
    {
        var posts = new FakeJobPosts();
        var board = new JobBoard(posts, new FakePhotoStore());
        posts.PickedWith["worker"] = [Job(household: true)];

        (await board.ShareMeetLocationAsync(Job(household: false), AngonoCentre)).Should().BeNull();
        (await board.ShareMeetLocationAsync(Job(household: true), new GeoPoint(35.68, 139.69))).Should().BeNull("abroad");
        posts.MeetLocations.Should().BeEmpty();

        var sent = await board.ShareMeetLocationAsync(Job(household: true), AngonoCentre);

        sent!.MeetPoint.Should().Be(AngonoCentre);
        posts.MeetLocations.Should().Equal((7L, AngonoCentre));
    }
}
