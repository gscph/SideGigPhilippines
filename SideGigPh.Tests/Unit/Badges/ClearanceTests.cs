using Microsoft.Extensions.Logging.Abstractions;
using SideGigPh.Api;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Unit.Badges;

/// <summary>
/// The NBI / police clearance badge: the operator types the "valid until" printed on the paper to approve it, the
/// badge lapses the day after and the worker may send a new one; the server reads the paper's QR code only as a hint
/// for the operator, and only an NBI or PNP web address counts as official.
/// </summary>
public class ClearanceTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);

    [Theory]
    [InlineData(0, true)]
    [InlineData(180, true)]
    [InlineData(372, true)] // a year and a week: an NBI issued today, with room for a date typed a few days off
    [InlineData(-1, false)] // already expired: reject it instead
    [InlineData(380, false)] // longer than any clearance is valid
    public void TheValidUntilDate_IsTodayToAYearAhead(int daysAhead, bool allowed) =>
        ClearanceRules.IsValidUntilAllowed(Today.AddDays(daysAhead), Today).Should().Be(allowed);

    [Fact]
    public void ABadge_ShowsThroughItsLastDay_AndLapsesTheDayAfter()
    {
        ClearanceRules.HasLapsed(Today, Today).Should().BeFalse();
        ClearanceRules.HasLapsed(Today.AddDays(-1), Today).Should().BeTrue();
        ClearanceRules.HasLapsed(null, Today).Should().BeFalse("every other badge has no date and never lapses");
    }

    [Theory]
    [InlineData("https://clearance.nbi.gov.ph/verify/ABC123", ClearanceIssuers.Nbi)]
    [InlineData("http://nbi.gov.ph/x", ClearanceIssuers.Nbi)]
    [InlineData("https://pnpclearance.ph/verify?id=1", ClearanceIssuers.Police)]
    [InlineData("https://verify.PNPCLEARANCE.ph/1", ClearanceIssuers.Police)]
    [InlineData("https://nbi.gov.ph.verify-clearance.com/ABC", null)] // a look-alike address
    [InlineData("https://fakenbi.gov.ph/ABC", null)]
    [InlineData("ftp://nbi.gov.ph/ABC", null)]
    [InlineData("NBI ID 12345 JUAN DELA CRUZ", null)] // plain text, not an address
    [InlineData("", null)]
    [InlineData(null, null)]
    public void OnlyAnNbiOrPnpAddress_IsOfficial(string? qr, string? issuer)
    {
        ClearanceRules.IssuerOf(qr).Should().Be(issuer);
        (ClearanceRules.OfficialLink(qr) is not null).Should().Be(issuer is not null);
    }

    [Fact]
    public void TheServer_ReadsTheQrOnAPhotoOfThePaper()
    {
        const string qr = "https://clearance.nbi.gov.ph/verify/ABC123XYZ";

        var scan = ClearanceQrReader.Read(TestImages.PaperWithQr(qr));

        scan.Status.Should().Be(ScanStatuses.Read);
        scan.Qr.Should().Be(qr);
    }

    [Fact]
    public void APhotoWithoutAQr_IsUnreadable_NeverAnError()
    {
        ClearanceQrReader.Read(TestImages.Jpeg()).Status.Should().Be(ScanStatuses.Unreadable);
        ClearanceQrReader.Read([0x01, 0x02, 0x03]).Status.Should().Be(ScanStatuses.Unreadable, "a broken file is not the operator's problem here");
    }

    [Fact]
    public void TheOperator_ApprovesAClearanceOnlyWithAnAllowedValidUntil_OtherBadgesNeedNone()
    {
        var clearance = Pending(BadgeKind.Clearance);
        var tesda = Pending(BadgeKind.Tesda);

        BadgeReview.CanApprove(clearance, null, Today).Should().BeFalse();
        BadgeReview.CanApprove(clearance, Today.AddDays(-1), Today).Should().BeFalse();
        BadgeReview.CanApprove(clearance, Today.AddMonths(6), Today).Should().BeTrue();
        BadgeReview.CanApprove(tesda, null, Today).Should().BeTrue();
    }

    [Fact]
    public async Task AnExpiredClearance_CanBeSentAgain()
    {
        var server = new MineOnly(new BadgeRequest(1, new Badge(BadgeKind.Clearance), BadgeState.Expired, null, DateTimeOffset.UtcNow, Today.AddDays(-1)));
        var badges = new ProBadges(server, NullLogger<ProBadges>.Instance);
        await badges.LoadAsync();

        badges.StateOf(BadgeKind.Clearance).Should().Be(BadgeState.Expired);
        badges.CanSend(BadgeKind.Clearance).Should().BeTrue();
        badges.ApprovedKinds.Should().Be(0, "an expired badge no longer counts");
    }

    private static PendingBadge Pending(BadgeKind kind) =>
        new(1, "uid", "Juan", null, new Badge(kind), new BadgeScan(BadgeScanResult.None), true, DateTimeOffset.UtcNow);

    private sealed class MineOnly(params BadgeRequest[] mine) : IVerificationRepository
    {
        public Task<IReadOnlyList<BadgeRequest>> GetMineAsync() => Task.FromResult<IReadOnlyList<BadgeRequest>>(mine);

        public Task<BadgeSubmitResult> SubmitAsync(Badge badge, byte[] photo) => Task.FromResult(BadgeSubmitResult.Sent);
    }
}
