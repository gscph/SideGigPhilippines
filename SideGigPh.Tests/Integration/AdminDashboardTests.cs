using System.Net;
using System.Net.Http.Json;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The operator site's reads on the real API and database (<c>admin/stats</c>, <c>admin/users</c>, <c>admin/seekers</c>,
/// <c>admin/workers</c>, <c>admin/error-reports</c>): new people show up in the lists and the counts, contact details
/// come masked, and nobody but an operator gets anything.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class AdminDashboardTests(ApiTestHost api)
{
    private HttpClient Operator => api.Client(api.Tokens.For(ApiTestHost.AdminUid));

    private async Task<string> SeekerProfileAsync(string name)
    {
        var uid = $"admin-seeker-{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        using var saved = await api.Client(api.Tokens.For(uid)).PutAsJsonAsync("users/me",
            new UserDto(uid, name, null, $"{name.ToLowerInvariant()}@example.com", Wire.Taglish, Wire.Seeker, null, now, now), ApiJson.Default.UserDto);
        saved.IsSuccessStatusCode.Should().BeTrue();
        return uid;
    }

    [IntegrationFact]
    public async Task APublishedWorker_IsInTheWorkersList_WithTheNumberMasked()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Ramil");
        await worker.Pro.PublishAsync();

        var page = (await Operator.GetFromJsonAsync($"admin/workers?search={worker.Uid}", ApiJson.Default.AdminWorkerPageDto))!;

        var row = page.Items.Should().ContainSingle().Subject;
        page.Total.Should().Be(1);
        (row.Live, row.TradeId, row.RatePesos, row.WorkPhotos, row.HasPhoto).Should().Be((true, "aircon", 800, 2, true));
        row.Trade.Should().NotBeNullOrEmpty();
        row.Towns.Should().NotBeNullOrEmpty();
        row.Phone.Should().NotBe(worker.PhoneNumber).And.EndWith(worker.PhoneNumber![^4..]);
        (await Operator.GetFromJsonAsync($"admin/workers?search={worker.Uid}&filter=not-live", ApiJson.Default.AdminWorkerPageDto))!
            .Items.Should().BeEmpty();
        (await Operator.GetFromJsonAsync($"admin/workers?search={worker.Uid}&filter=aircon", ApiJson.Default.AdminWorkerPageDto))!
            .Items.Should().ContainSingle();
    }

    [IntegrationFact]
    public async Task ANewHousehold_IsInTheUsersAndSeekersLists_WithTheEmailMasked()
    {
        var uid = await SeekerProfileAsync("Nena");

        var users = (await Operator.GetFromJsonAsync($"admin/users?search={uid}&role=seeker", ApiJson.Default.AdminUserPageDto))!;
        var user = users.Items.Should().ContainSingle().Subject;
        (user.Role, user.Language, user.HasListing).Should().Be((Wire.Seeker, Wire.Taglish, false));
        user.Email.Should().Be("n•••@example.com");
        (await Operator.GetFromJsonAsync($"admin/users?search={uid}&role=professional", ApiJson.Default.AdminUserPageDto))!.Items.Should().BeEmpty();

        var seekers = (await Operator.GetFromJsonAsync($"admin/seekers?search={uid}", ApiJson.Default.AdminSeekerPageDto))!;
        var seeker = seekers.Items.Should().ContainSingle().Subject;
        (seeker.Name, seeker.Posts, seeker.Chats).Should().Be(("Nena", 0, 0));
    }

    [IntegrationFact]
    public async Task Pages_AreKeptWithinTheLimit()
    {
        await SeekerProfileAsync("Paging");

        var page = (await Operator.GetFromJsonAsync($"admin/users?take={AdminLists.MaxTake + 50}&skip=-3", ApiJson.Default.AdminUserPageDto))!;

        page.Items.Count.Should().BeInRange(1, AdminLists.MaxTake);
        page.Total.Should().BeGreaterThanOrEqualTo(page.Items.Count);
    }

    [IntegrationFact]
    public async Task Stats_CountTodaysSignUps_ByPhilippineDay()
    {
        var before = (await Operator.GetFromJsonAsync("admin/stats", ApiJson.Default.AdminStatsDto))!;
        await SeekerProfileAsync("Counted");

        var after = (await Operator.GetFromJsonAsync("admin/stats", ApiJson.Default.AdminStatsDto))!;

        after.Users.Total.Should().Be(before.Users.Total + 1);
        after.Users.Seekers.Should().Be(before.Users.Seekers + 1);
        after.SignUps.Should().HaveCount(AdminStore.SignUpDays);
        after.SignUps[^1].Day.Should().Be(DateOnly.FromDateTime(after.At.ToOffset(AdminStore.Manila).Date));
        after.SignUps[^1].Seekers.Should().BeGreaterThanOrEqualTo(1);
        after.Languages.Should().Contain(l => l.Id == Wire.Taglish && l.Label == "Taglish");
    }

    [IntegrationFact]
    public async Task ErrorReports_AreListedNewestFirst_WithTheInstallCut()
    {
        var marker = $"admin-list-{Guid.NewGuid():N}";
        var installId = Guid.NewGuid().ToString();
        var sent = await new TestPhone(api, "Crashy").Get<IErrorReportRepository>().SendAsync(installId,
            new DeviceStamp("1.0 (7)", "48466c9", "beta", "android 14", "samsung SM-A155F"),
            [new ErrorReport(ErrorKind.Crash, "SideGigPh.Maui.Test", marker, "System.Exception: boom", "/seeker", null, 2,
                DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow)]);
        sent.Should().BeTrue();

        var page = (await Operator.GetFromJsonAsync($"admin/error-reports?search={marker}", ApiJson.Default.AdminErrorReportPageDto))!;

        var report = page.Items.Should().ContainSingle().Subject;
        (report.Kind, report.Count, report.Channel, report.Install).Should().Be((ErrorKinds.Crash, 2, AppChannels.Beta, installId[..8]));
        (await Operator.GetFromJsonAsync($"admin/error-reports?search={marker}&filter=prod", ApiJson.Default.AdminErrorReportPageDto))!.Items.Should().BeEmpty();
        (await Operator.GetFromJsonAsync($"admin/error-reports?search={marker}&filter=crash", ApiJson.Default.AdminErrorReportPageDto))!.Items.Should().ContainSingle();
    }

    [IntegrationTheory]
    [InlineData("admin/stats")]
    [InlineData("admin/users")]
    [InlineData("admin/seekers")]
    [InlineData("admin/workers")]
    [InlineData("admin/error-reports")]
    public async Task OnlyOperators_GetAnything(string route)
    {
        using var signedOut = await api.Client().GetAsync(route);
        signedOut.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var someone = await api.Client(api.Tokens.For($"not-admin-{Guid.NewGuid():N}")).GetAsync(route);
        someone.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await someone.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.NotAdmin);
    }
}
