using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Api;
using Microsoft.Extensions.Logging.Abstractions;
using static SideGigPh.Tests.TestSupport.TestData;

namespace SideGigPh.Tests.Unit.Badges;

/// <summary>
/// The four badges (ID, NBI or police clearance, TESDA certificate or training, a past project with a developer or any other company): what a
/// worker may send and when, that nothing leaves the phone without the consent tick, and that a badge moves a worker
/// up in search without changing anyone else's score.
/// </summary>
public class BadgeTests
{
    private sealed class FakeVerifications : IVerificationRepository
    {
        public List<BadgeRequest> Mine { get; } = [];

        public List<(Badge Badge, byte[] Photo)> Sent { get; } = [];

        public BadgeSubmitResult Answer { get; set; } = BadgeSubmitResult.Sent;

        public bool Offline { get; set; }

        public Task<IReadOnlyList<BadgeRequest>> GetMineAsync() => Task.FromResult<IReadOnlyList<BadgeRequest>>([.. Mine]);

        public Task<BadgeSubmitResult> SubmitAsync(Badge badge, byte[] photo)
        {
            if (Offline)
            {
                throw new HttpRequestException("no internet");
            }

            Sent.Add((badge, photo));
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
    public async Task AProjectBadge_NeedsADeveloperOrATypedCompany()
    {
        var (badges, server) = New();

        (await badges.SubmitAsync(new Badge(BadgeKind.Project), true, OnePhoto)).Should().Be(BadgeSubmitResult.NoCompany);
        (await badges.SubmitAsync(new Badge(BadgeKind.Project, BadgeCatalog.OtherCompany, " "), true, OnePhoto)).Should().Be(BadgeSubmitResult.NoCompany);
        (await badges.SubmitAsync(new Badge(BadgeKind.Project, BadgeCatalog.OtherCompany, " JM Builders ", " "), true, OnePhoto)).Should().Be(BadgeSubmitResult.Sent);
        (await badges.SubmitAsync(new Badge(BadgeKind.Project, "dmci", "typed by mistake", "Acacia Estates"), true, OnePhoto)).Should().Be(BadgeSubmitResult.Sent);

        // Not only the big developers: any company the worker names. A developer's own name is never typed.
        server.Sent.Select(s => s.Badge).Should().Equal(
            new Badge(BadgeKind.Project, BadgeCatalog.OtherCompany, "JM Builders"),
            new Badge(BadgeKind.Project, "dmci", null, "Acacia Estates"));
    }

    [Fact]
    public async Task NoPhotoPicked_OrNoInternet_IsSaidPlainly_AndNeverThrows()
    {
        var (badges, server) = New();

        (await badges.SubmitAsync(new Badge(BadgeKind.Tesda), true, PhotoPick.Cancelled)).Should().Be(BadgeSubmitResult.NoPhoto);
        (await badges.SubmitAsync(new Badge(BadgeKind.Tesda), true, PhotoPick.PermissionDenied)).Should().Be(BadgeSubmitResult.PhotoDenied);
        server.Offline = true;
        (await badges.SubmitAsync(new Badge(BadgeKind.Tesda), true, OnePhoto)).Should().Be(BadgeSubmitResult.Failed);
    }

    [Fact]
    public async Task EachBadgeStands_AsItsBestRequest_AndARejectedOneCanBeSentAgain()
    {
        var (badges, server) = New();
        server.Mine.AddRange(
        [
            Request(1, new Badge(BadgeKind.Id), BadgeState.Rejected, daysAgo: 5),
            Request(2, new Badge(BadgeKind.Id), BadgeState.Pending, daysAgo: 1),
            Request(3, new Badge(BadgeKind.Tesda), BadgeState.Rejected),
            Request(4, new Badge(BadgeKind.Project, "ayala-land"), BadgeState.Approved),
            Request(5, new Badge(BadgeKind.Project, BadgeCatalog.OtherCompany, "JM Builders"), BadgeState.Pending),
        ]);

        await badges.LoadAsync();

        badges.StateOf(BadgeKind.Id).Should().Be(BadgeState.Pending);
        badges.CanSend(BadgeKind.Id).Should().BeFalse("it is being checked");
        badges.StateOf(BadgeKind.Tesda).Should().Be(BadgeState.Rejected);
        badges.CanSend(BadgeKind.Tesda).Should().BeTrue();
        badges.Of(BadgeKind.Project).Select(r => r.Id).Should().Equal(4, 5);
        badges.CanSend(BadgeKind.Project).Should().BeTrue("every company is its own badge");
        badges.ApprovedKinds.Should().Be(1);
    }

    [Fact]
    public void TheSendPath_SaysConsent_AndCarriesTheCompanyAndBuilding()
    {
        ApiMapping.ToSubmitPath(new Badge(BadgeKind.Id)).Should().Be("verifications/id?consent=true");
        ApiMapping.ToSubmitPath(new Badge(BadgeKind.Tesda)).Should().Be("verifications/tesda?consent=true");
        ApiMapping.ToSubmitPath(new Badge(BadgeKind.Clearance)).Should().Be("verifications/clearance?consent=true");
        ApiMapping.ToSubmitPath(new Badge(BadgeKind.Project, BadgeCatalog.OtherCompany, "JM Builders & Sons", "Phase 2"))
            .Should().Be("verifications/developer?consent=true&badgeId=other&companyName=JM%20Builders%20%26%20Sons&projectName=Phase%202");
    }

    [Fact]
    public void AListingsBadges_ReachTheApp_AndAKindThisVersionDoesntKnowIsLeftOut()
    {
        var dto = Dto("Maria") with
        {
            Badges = [new BadgeDto(VerificationKinds.Id), new BadgeDto("future-kind"), new BadgeDto(VerificationKinds.Developer, DeveloperBadges.Other, "Phase 2", "JM Builders")],
        };

        var listing = dto.ToModel();

        listing.Badges.Should().Equal(new Badge(BadgeKind.Id), new Badge(BadgeKind.Project, "other", "JM Builders", "Phase 2"));
        BadgeCatalog.CompanyOf(listing.Badges[1]).Should().Be("JM Builders");
        BadgeCatalog.CompanyOf(new Badge(BadgeKind.Project, "sm")).Should().Be("SM");
    }

    [Fact]
    public void TheDeveloperIds_MatchTheServers()
    {
        BadgeCatalog.Developers.Select(d => d.Id).Append(BadgeCatalog.OtherCompany).Should().Equal(DeveloperBadges.All);
        (BadgeCatalog.MinCompanyName, BadgeCatalog.MaxCompanyName).Should().Be((DeveloperBadges.MinCompanyName, DeveloperBadges.MaxCompanyName));
        BadgeCatalog.MaxProjectName.Should().Be(VerificationStore.MaxProjectName);
    }

    [Fact]
    public void Ranking_BadgesAddUpTo13Points_OnTopOfTheSameScore()
    {
        var plain = Dto("Plain");
        var verified = Dto("Verified") with
        {
            Badges =
            [
                new BadgeDto(VerificationKinds.Id), new BadgeDto(VerificationKinds.Tesda), new BadgeDto(VerificationKinds.Clearance),
                new BadgeDto(VerificationKinds.Developer, DeveloperBadges.Dmci), new BadgeDto(VerificationKinds.Developer, DeveloperBadges.Other, null, "JM Builders"),
            ],
        };
        var request = new RankRequest("aircon-cleaning", "taytay", "500-1000");

        var without = SearchRanker.Score(Candidate(plain), request, Scales, RankNow);
        var with = SearchRanker.Score(Candidate(verified), request, Scales, RankNow);

        without.Trust.Should().Be(0);
        with.Trust.Should().Be(SearchRanker.IdBadgeWeight + SearchRanker.TesdaBadgeWeight + SearchRanker.ProjectBadgeWeight + SearchRanker.ClearanceBadgeWeight)
            .And.Be(13, "two project badges count once");
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
}
