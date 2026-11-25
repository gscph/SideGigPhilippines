using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// The worker's profile levels: red parts block publishing, green is enough to be live, blue is a fully set-up
/// profile, amber is optional but worth doing (work photos, a badge not asked for yet). A wrong mark sends a worker to the wrong page, or hides the reminder
/// that would bring them more customers.
/// </summary>
public class ProfileProgressTests
{
    private static readonly BadgeState[] NoBadges = [BadgeState.None, BadgeState.None];
    private static readonly BadgeState[] AllBadges = [BadgeState.Approved, BadgeState.Approved];

    private static ProfileProgress Progress(
        bool published = false,
        bool photo = true,
        int work = 2,
        bool phone = true,
        BadgeState[]? badges = null) =>
        ProfileProgress.From(published, photo, work, phone, badges ?? NoBadges);

    [Fact]
    public void NewWorker_IsRedAndStartsWithTheirPhoto()
    {
        var progress = Progress(photo: false, work: 0, phone: false);

        progress.Level.Should().Be(ProfileLevel.NotLive);
        progress.Photo.Should().Be(ProfileMark.Missing);
        progress.WorkPhotos.Should().Be(ProfileMark.Recommended, "work photos are optional, never red");
        progress.Phone.Should().Be(ProfileMark.Missing);
        progress.NextStep.Should().Be(ProfileNextStep.Photo);
    }

    [Theory]
    [InlineData(false, 2, true, ProfileNextStep.Photo)]
    [InlineData(true, 2, false, ProfileNextStep.Phone)]
    [InlineData(true, 0, false, ProfileNextStep.Phone)] // no work photos: never a step
    [InlineData(true, 2, true, ProfileNextStep.Publish)]
    [InlineData(true, 0, true, ProfileNextStep.Publish)] // no work photos: still ready to publish
    [InlineData(true, 1, true, ProfileNextStep.Publish)]
    public void NextStep_IsTheFirstMissingRequirement(bool photo, int work, bool phone, ProfileNextStep expected) =>
        Progress(photo: photo, work: work, phone: phone).NextStep.Should().Be(expected);

    [Fact]
    public void Published_HasNoNextStepAndIsGreen()
    {
        var progress = Progress(published: true);

        progress.NextStep.Should().BeNull();
        progress.Level.Should().Be(ProfileLevel.Live);
    }

    [Theory]
    [InlineData(0, ProfileMark.Recommended)]
    [InlineData(1, ProfileMark.Recommended)]
    [InlineData(2, ProfileMark.Done)]
    [InlineData(5, ProfileMark.Done)]
    [InlineData(6, ProfileMark.Full)]
    public void WorkPhotos_AmberUnderTwo_NeverRed_BlueAtSix(int count, ProfileMark expected) =>
        Progress(work: count).WorkPhotos.Should().Be(expected);

    [Theory]
    [InlineData(true, 0, true)]
    [InlineData(true, 1, true)]
    [InlineData(true, 2, false)]
    [InlineData(false, 0, false)] // before publishing the setup card shows the optional line instead
    public void WorkPhotosNudge_OnlyLiveWithFewerThanTwo(bool published, int work, bool shows) =>
        Progress(published: published, work: work).ShowWorkPhotosNudge.Should().Be(shows);

    [Fact]
    public void NoBadges_AreRecommended()
    {
        var progress = Progress();

        progress.Badges.Should().Be(ProfileMark.Recommended);
        progress.ShowBadgeNudge.Should().BeTrue();
        progress.BadgeCount.Should().Be(0);
    }

    [Fact]
    public void RejectedBadge_IsRecommendedAgain() =>
        Progress(badges: [BadgeState.Approved, BadgeState.Rejected]).Badges.Should().Be(ProfileMark.Recommended);

    [Fact]
    public void EveryMissingBadgeSent_IsWaitingNotNagged()
    {
        var progress = Progress(badges: [BadgeState.Approved, BadgeState.Pending]);

        progress.Badges.Should().Be(ProfileMark.Waiting);
        progress.ShowBadgeNudge.Should().BeFalse("everything they could send is with the checker");
        progress.BadgeCount.Should().Be(1);
    }

    [Fact]
    public void AllBadges_AreBlue() =>
        Progress(badges: AllBadges).Badges.Should().Be(ProfileMark.Full);

    [Fact]
    public void PublishedAndEverythingFull_IsComplete() =>
        Progress(published: true, work: 6, badges: AllBadges).Level.Should().Be(ProfileLevel.Complete);

    [Fact]
    public void EverythingFullButNotPublished_IsStillRed() =>
        Progress(published: false, work: 6, badges: AllBadges).Level.Should().Be(ProfileLevel.NotLive);

    [Theory]
    [InlineData(5, false)]
    [InlineData(6, true)]
    public void PublishedButOnePartShort_StaysGreen(int work, bool missingBadge)
    {
        BadgeState[] badges = missingBadge ? [BadgeState.Approved, BadgeState.None] : AllBadges;

        Progress(published: true, work: work, badges: badges).Level.Should().Be(ProfileLevel.Live);
    }
}
