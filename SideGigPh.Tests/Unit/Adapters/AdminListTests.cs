using Microsoft.AspNetCore.Http;
using SideGigPh.Api;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Tests.Unit.Adapters;

/// <summary>
/// The operator site's lists: contact details leave the server masked (enough to tell two people apart, not enough to
/// call them), and a page's query string is read safely whatever it says.
/// </summary>
public class AdminListTests
{
    [Theory]
    [InlineData("+639171234567", "+63 9•• ••• 4567")]
    [InlineData("09171234567", "•••••••4567")]
    [InlineData("12345", "•••••")]
    [InlineData(null, null)]
    [InlineData(" ", null)]
    public void Phone_IsMasked(string? number, string? shown) => AdminMask.Phone(number).Should().Be(shown);

    [Theory]
    [InlineData("juan@gmail.com", "j•••@gmail.com")]
    [InlineData("nope", "n•••")]
    [InlineData(null, null)]
    public void Email_IsMasked(string? email, string? shown) => AdminMask.Email(email).Should().Be(shown);

    [Fact]
    public void Install_IsCut() =>
        AdminMask.Install("0f8fad5b-d9cb-469f-a165-70867728950e").Should().Be("0f8fad5b");

    [Fact]
    public void Query_ReadsTheQueryString()
    {
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString("?search=%20Juan%20&sort=name&desc=false&skip=50&take=25&role=seeker");

        var query = AdminFunctions.ListQuery(http.Request, "role");

        query.Should().Be(new AdminListQuery(" Juan ", AdminLists.SortName, Desc: false, Skip: 50, Take: 25, Filter: Wire.Seeker));
        query.Words.Should().Be("Juan");
    }

    [Fact]
    public void Query_WithNothingOrNonsense_IsTheFirstPageNewestFirst()
    {
        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString("?skip=-5&take=100000&desc=maybe&role=");

        var query = AdminFunctions.ListQuery(http.Request, "role");

        (query.SafeSkip, query.SafeTake, query.Desc, query.Filter, query.Words).Should().Be((0, AdminLists.MaxTake, true, (string?)null, (string?)null));
    }

    [Fact]
    public void LongSearch_IsCut() =>
        new AdminListQuery(new string('a', 500)).Words.Should().HaveLength(100);
}
