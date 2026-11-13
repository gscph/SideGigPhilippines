using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Api;
using static SideGigPh.Tests.TestSupport.TestData;

namespace SideGigPh.Tests.Unit.Search;

/// <summary>
/// Results come a page at a time (<see cref="Paging.Search"/>): the server cuts the ranked list, the app adds each page
/// under the last. Every page must keep the ranked order (your town first, then nearby towns), the group titles must
/// count everyone, and nobody may show twice.
/// </summary>
public class SearchResultPagingTests
{
    private static SearchHitDto HitDto(string name, bool inTown = true) =>
        new(Dto(name, cities: [inTown ? "taytay" : "antipolo"]), true, inTown, true);

    private static readonly RankedResult Ranked = new(
        [HitDto("Town 1"), HitDto("Town 2"), HitDto("Town 3")],
        [HitDto("Near 1", false), HitDto("Near 2", false), HitDto("Near 3", false)],
        ServiceFallback: true);

    private static string[] Names(IEnumerable<SearchHit> hits) => hits.Select(h => h.Pro.DisplayName).ToArray();

    [Fact]
    public void Page_First_FillsInTownThenOtherTowns_AndCountsEveryone()
    {
        var page = Ranked.Page(0, Paging.Search);

        page.Hits.Select(h => h.Listing.DisplayName).Should().Equal("Town 1", "Town 2", "Town 3", "Near 1");
        page.InTownCount.Should().Be(3);
        page.OtherTownsCount.Should().Be(3);
        page.ServiceFallback.Should().BeTrue();
    }

    [Fact]
    public void Page_PastTheEnd_IsEmpty_ButStillCounts()
    {
        var page = Ranked.Page(6, Paging.Search);

        page.Hits.Should().BeEmpty();
        page.InTownCount.Should().Be(3);
    }

    [Fact]
    public void ToModel_SplitsAPageIntoGroups_ByItsPlaceInTheWholeList()
    {
        // The second page starts at result 4: "Near 2" and "Near 3" are both past the 3 in-town results.
        var second = Ranked.Page(4, Paging.Search).ToModel(skip: 4);
        var first = Ranked.Page(0, Paging.Search).ToModel(skip: 0);

        Names(first.InTown).Should().Equal("Town 1", "Town 2", "Town 3");
        Names(first.OtherTowns).Should().Equal("Near 1");
        second.InTown.Should().BeEmpty();
        Names(second.OtherTowns).Should().Equal("Near 2", "Near 3");
    }

    [Fact]
    public void Append_AddsTheNextPage_UntilEveryoneIsShown()
    {
        var first = Ranked.Page(0, Paging.Search).ToModel(0);
        first.HasMore.Should().BeTrue();

        var all = first.Append(Ranked.Page(first.Count, Paging.Search).ToModel(first.Count));

        Names(all.InTown).Should().Equal("Town 1", "Town 2", "Town 3");
        Names(all.OtherTowns).Should().Equal("Near 1", "Near 2", "Near 3");
        all.Total.Should().Be(6);
        all.HasMore.Should().BeFalse();
    }

    [Fact]
    public void Append_SomeoneAlreadyShown_IsNotShownTwice()
    {
        // The ranking moved between pages (a new review): the next page repeats a worker.
        var first = Ranked.Page(0, Paging.Search).ToModel(0);

        var all = first.Append(Ranked.Page(3, Paging.Search).ToModel(3));

        Names(all.OtherTowns).Should().Equal("Near 1", "Near 2", "Near 3");
    }
}
