using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Api;
using Microsoft.Extensions.Logging.Abstractions;
using static SideGigPh.Tests.TestSupport.TestData;

namespace SideGigPh.Tests.Unit.Badges;

/// <summary>
/// The two badges (ID, NBI or police clearance): what a
/// worker may send and when, that nothing leaves the phone without the consent tick, and that a badge moves a worker
/// up in search without changing anyone else's score.
/// </summary>
public class BadgeTests
{
    private sealed class FakeVerifications : IVerificationRepository
    {
        public List<BadgeRequest> Mine { get; } = [];

        public List<(Badge Badge, byte[] Photo, bool Retake)> Sent { get; } = [];

        public BadgeSubmitResult Answer { get; set; } = BadgeSubmitResult.Sent;

        public bool Offline { get; set; }

        public Task<IReadOnlyList<BadgeRequest>> GetMineAsync() => Task.FromResult<IReadOnlyList<BadgeRequest>>([.. Mine]);

        public Task<BadgeSubmitResult> SubmitAsync(Badge badge, byte[] photo, bool retake)
        {
            if (Offline)
            {
                throw new HttpRequestException("no internet");
            }

            Sent.Add((badge, photo, retake));
            return Task.FromResult(Answer);
        }
    }

    private static readonly PhotoPick OnePhoto = new(PhotoPickStatus.Picked, [[0xFF, 0xD8]]);

    private static (ProBadges Badges, FakeVerifications Server) New()
    {
        var server = new FakeVerifications();
        return (new ProBadges(server, NullLogger<ProBadges>.Instance), server);
    }

    private static BadgeRequest Request(long id, Badge badge, BadgeState state, int daysAgo = 0) =>
        new(id, badge, state, state == BadgeState.Rejected ? "Blurry" : null, RankNow.AddDays(-daysAgo));

    [Fact]
    public async Task WithoutTheConsentTick_NothingLeavesThePhone()
    {
        var (badges, server) = New();

        (await badges.SubmitAsync(new Badge(BadgeKind.Id), consent: false, OnePhoto)).Should().Be(BadgeSubmitResult.NoConsent);

        server.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task NoPhotoPicked_OrNoInternet_IsSaidPlainly_AndNeverThrows()
    {
        var (badges, server) = New();

        (await badges.SubmitAsync(new Badge(BadgeKind.Clearance), true, PhotoPick.Cancelled)).Should().Be(BadgeSubmitResult.NoPhoto);
        (await badges.SubmitAsync(new Badge(BadgeKind.Clearance), true, PhotoPick.PermissionDenied)).Should().Be(BadgeSubmitResult.PhotoDenied);
        server.Offline = true;
        (await badges.SubmitAsync(new Badge(BadgeKind.Clearance), true, OnePhoto)).Should().Be(BadgeSubmitResult.Failed);
    }

    [Fact]
    public async Task EachBadgeStands_AsItsBestRequest_AndARejectedOneCanBeSentAgain()
    {
        var (badges, server) = New();
        server.Mine.AddRange(
        [
            Request(1, new Badge(BadgeKind.Id), BadgeState.Rejected, daysAgo: 5),
            Request(2, new Badge(BadgeKind.Id), BadgeState.Pending, daysAgo: 1),
            Request(3, new Badge(BadgeKind.Clearance), BadgeState.Rejected),
        ]);

        await badges.LoadAsync();

        badges.StateOf(BadgeKind.Id).Should().Be(BadgeState.Pending);
        badges.CanSend(BadgeKind.Id).Should().BeFalse("it is being checked");
        badges.Of(BadgeKind.Id)!.Id.Should().Be(2);
        badges.StateOf(BadgeKind.Clearance).Should().Be(BadgeState.Rejected);
        badges.CanSend(BadgeKind.Clearance).Should().BeTrue();
        badges.ApprovedKinds.Should().Be(0);
    }

    [Fact]
    public void TheSendPath_SaysConsent()
    {
        ApiMapping.ToSubmitPath(new Badge(BadgeKind.Id)).Should().Be("verifications/id?consent=true");
        ApiMapping.ToSubmitPath(new Badge(BadgeKind.Clearance), retake: true).Should().Be("verifications/clearance?consent=true&retake=true");
    }

    [Fact]
    public void AListingsBadges_ReachTheApp_AndAKindThisVersionDoesntKnowIsLeftOut()
    {
        var dto = Dto("Maria") with
        {
            Badges = [new BadgeDto(VerificationKinds.Id), new BadgeDto("tesda"), new BadgeDto(VerificationKinds.Clearance)],
        };

        var listing = dto.ToModel();

        listing.Badges.Should().Equal(new Badge(BadgeKind.Id), new Badge(BadgeKind.Clearance));
    }

    [Fact]
    public void Ranking_BadgesAddUpTo7Points_OnTopOfTheSameScore()
    {
        var plain = Dto("Plain");
        var verified = Dto("Verified") with
        {
            Badges =
            [
                new BadgeDto(VerificationKinds.Id), new BadgeDto(VerificationKinds.Clearance), new BadgeDto("tesda"), new BadgeDto("developer"),
            ],
        };
        var request = new RankRequest("aircon-cleaning", "taytay", "500-1000");

        var without = SearchRanker.Score(Candidate(plain), request, Scales, RankNow);
        var with = SearchRanker.Score(Candidate(verified), request, Scales, RankNow);

        without.Trust.Should().Be(0);
        with.Trust.Should().Be(SearchRanker.IdBadgeWeight + SearchRanker.ClearanceBadgeWeight)
            .And.Be(7, "a dropped kind (TESDA, past project) earns nothing");
        (with.Total - with.Trust).Should().Be(without.Total, "a badge is a bonus: nobody else's score changes");
    }

    [Fact]
    public void Ranking_AmongEqualWorkers_TheOneWithABadgeComesFirst()
    {
        // "Zeny" sorts last by name, so only her badge can put her first.
        var pros = new[] { Candidate(Dto("Abel")), Candidate(Dto("Zeny") with { Badges = [new BadgeDto(VerificationKinds.Id)] }) };

        var result = SearchRanker.Rank(pros, new RankRequest("aircon-cleaning", "taytay", null), Scales, RankNow);

        result.InTown.Select(h => h.Listing.DisplayName).Should().Equal("Zeny", "Abel");
    }

    [Fact]
    public async Task WhatThePhotoCheckFound_IsAskedAbout_OnlyWhileThatProofWaits()
    {
        var (badges, server) = New();
        server.Mine.Add(new BadgeRequest(1, new Badge(BadgeKind.Id), BadgeState.Pending, null, RankNow, [ProofProblem.Blurry]));
        server.Mine.Add(new BadgeRequest(2, new Badge(BadgeKind.Clearance), BadgeState.Rejected, "Blurry", RankNow, [ProofProblem.Dark]));

        (await badges.SubmitAsync(new Badge(BadgeKind.Id), true, OnePhoto)).Should().Be(BadgeSubmitResult.Sent);

        badges.ProblemsOf(new Badge(BadgeKind.Id)).Should().Equal(ProofProblem.Blurry);
        badges.ProblemsOf(new Badge(BadgeKind.Clearance)).Should().BeEmpty("a decided proof has nothing to retake");
    }

    [Fact]
    public async Task ARetake_IsSentAsOne()
    {
        var (badges, server) = New();

        await badges.SubmitAsync(new Badge(BadgeKind.Id), true, OnePhoto);
        await badges.SubmitAsync(new Badge(BadgeKind.Id), true, OnePhoto, retake: true);

        server.Sent.Select(s => s.Retake).Should().Equal(false, true);
    }
}
