using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// The worker's profile levels: red parts block publishing, green is enough to be live, blue is a fully set-up
/// profile, amber is a badge not asked for yet. A wrong mark sends a worker to the wrong page, or hides the reminder
/// that would bring them more customers.
/// </summary>
public class ProfileProgressTests
{
    private static readonly BadgeState[] NoBadges = [BadgeState.None, BadgeState.None, BadgeState.None];
    private static readonly BadgeState[] AllBadges = [BadgeState.Approved, BadgeState.Approved, BadgeState.Approved];

    private static ProfileProgress Progress(
        bool published = false,
        bool photo = true,
        int work = 2,
        int references = 1,
        bool phone = true,
        BadgeState[]? badges = null) =>
        ProfileProgress.From(published, photo, work, references, phone, badges ?? NoBadges);

    [Fact]
    public void NewWorker_IsRedAndStartsWithTheirPhoto()
    {
        var progress = Progress(photo: false, work: 0, references: 0, phone: false);

        progress.Level.Should().Be(ProfileLevel.NotLive);
        progress.Photo.Should().Be(ProfileMark.Missing);
        progress.WorkPhotos.Should().Be(ProfileMark.Missing);
        progress.References.Should().Be(ProfileMark.Missing);
        progress.Phone.Should().Be(ProfileMark.Missing);
        progress.NextStep.Should().Be(ProfileNextStep.Photo);
    }

    [Theory]
    [InlineData(false, 2, 1, true, ProfileNextStep.Photo)]
    [InlineData(true, 1, 1, true, ProfileNextStep.WorkPhotos)]
    [InlineData(true, 2, 0, true, ProfileNextStep.References)]
    [InlineData(true, 2, 1, false, ProfileNextStep.Phone)]
    [InlineData(true, 2, 1, true, ProfileNextStep.Publish)]
    public void NextStep_IsTheFirstMissingRequirement(bool photo, int work, int references, bool phone, ProfileNextStep expected) =>
        Progress(photo: photo, work: work, references: references, phone: phone).NextStep.Should().Be(expected);

    [Fact]
    public void Published_HasNoNextStepAndIsGreen()
    {
        var progress = Progress(published: true);

        progress.NextStep.Should().BeNull();
        progress.Level.Should().Be(ProfileLevel.Live);
    }

    [Theory]
    [InlineData(0, ProfileMark.Missing)]
    [InlineData(1, ProfileMark.Missing)]
    [InlineData(2, ProfileMark.Done)]
    [InlineData(5, ProfileMark.Done)]
    [InlineData(6, ProfileMark.Full)]
    public void WorkPhotos_RedUnderTwo_BlueAtSix(int count, ProfileMark expected) =>
        Progress(work: count).WorkPhotos.Should().Be(expected);

    [Theory]
    [InlineData(0, ProfileMark.Missing)]
    [InlineData(1, ProfileMark.Done)]
    [InlineData(2, ProfileMark.Done)]
    [InlineData(3, ProfileMark.Full)]
    public void References_RedAtZero_BlueAtThree(int count, ProfileMark expected) =>
        Progress(references: count).References.Should().Be(expected);

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
        Progress(badges: [BadgeState.Approved, BadgeState.Pending, BadgeState.Rejected]).Badges.Should().Be(ProfileMark.Recommended);

    [Fact]
    public void EveryMissingBadgeSent_IsWaitingNotNagged()
    {
        var progress = Progress(badges: [BadgeState.Approved, BadgeState.Pending, BadgeState.Pending]);

        progress.Badges.Should().Be(ProfileMark.Waiting);
        progress.ShowBadgeNudge.Should().BeFalse("everything they could send is with the checker");
        progress.BadgeCount.Should().Be(1);
    }

    [Fact]
    public void AllBadges_AreBlue() =>
        Progress(badges: AllBadges).Badges.Should().Be(ProfileMark.Full);

    [Fact]
    public void PublishedAndEverythingFull_IsComplete() =>
        Progress(published: true, work: 6, references: 3, badges: AllBadges).Level.Should().Be(ProfileLevel.Complete);

    [Fact]
    public void EverythingFullButNotPublished_IsStillRed() =>
        Progress(published: false, work: 6, references: 3, badges: AllBadges).Level.Should().Be(ProfileLevel.NotLive);

    [Theory]
    [InlineData(5, 3, false)]
    [InlineData(6, 2, false)]
    [InlineData(6, 3, true)]
    public void PublishedButOnePartShort_StaysGreen(int work, int references, bool missingBadge)
    {
        BadgeState[] badges = missingBadge ? [BadgeState.Approved, BadgeState.Approved, BadgeState.None] : AllBadges;

        Progress(published: true, work: work, references: references, badges: badges).Level.Should().Be(ProfileLevel.Live);
    }
}
