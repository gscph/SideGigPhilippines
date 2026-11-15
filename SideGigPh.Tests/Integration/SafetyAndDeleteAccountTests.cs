using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The store rules end to end: Delete account removes everything (including reviews about the person and the sign-in
/// account), and report / block hide two people from each other (App Store 1.2 and 5.1.1(v), Google Play).
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class SafetyAndDeleteAccountTests(ApiTestHost api)
{
    [IntegrationFact]
    public async Task DeleteAccount_RemovesEverything_IncludingReviewsAboutThem_AndTheSignInAccount()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Dan");
        await worker.Pro.PublishAsync();
        await worker.Account.SaveProfileAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Eva");
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;
        await seeker.Seeker.SendAsync(listing, "Hi po");
        await api.StartChatEarlierAsync(seeker.Seeker.ChatIdWith(worker.Uid), TimeSpan.FromHours(25));
        await seeker.Seeker.RefreshActivityAsync();
        (await seeker.Seeker.SubmitReviewAsync(listing, 5, "Ayos", "Eva D.")).Should().BeTrue();
        var photoIds = worker.Pro.Draft.WorkPhotoIds.Append(worker.Pro.Draft.ProfilePhotoId!).ToList();

        await worker.Account.DeleteAccountAsync();

        var viewer = new TestPhone(api, "Viewer");
        (await viewer.Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid)).Should().BeNull("the listing is gone");
        (await viewer.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Should().BeEmpty("reviews about them go too");
        photoIds.Should().OnlyContain(id => !api.Blobs.Blobs.ContainsKey($"{id}.jpg"));
        api.Accounts.Deleted.Should().Contain(worker.Uid, "the server deleted the Firebase account");
        worker.Identity.AccountDeleted.Should().BeTrue("the phone signed out");
        worker.Store.Cleared.Should().BeTrue();
        await seeker.Seeker.RefreshActivityAsync();
        seeker.Seeker.Threads.Should().BeEmpty("the chat went with the worker");
        using var profile = await api.Client(api.Tokens.For(worker.Uid)).GetAsync("users/me");
        profile.StatusCode.Should().Be(HttpStatusCode.NotFound, "the profile is gone");
    }

    [IntegrationFact]
    public async Task WorkerBlocksCustomer_TheyNoLongerSeeEachOther_AndCantWrite()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Fely");
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Gil");
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;
        await seeker.Seeker.SendAsync(listing, "Hi po");
        await worker.Pro.RefreshActivityAsync();
        var chat = worker.Pro.Chats.Should().ContainSingle().Subject;

        await worker.Pro.BlockAsync(chat.SeekerId);

        worker.Pro.Chats.Should().BeEmpty("the chat leaves the worker's Inbox at once");
        (await seeker.Seeker.SearchAsync()).InTown.Should().NotContain(h => h.Pro.Id == worker.Uid);
        using var profile = await api.Client(api.Tokens.For(seeker.Uid)).GetAsync($"professionals/{worker.Uid}");
        profile.StatusCode.Should().Be(HttpStatusCode.NotFound, "a blocked worker has no profile for them");
        var write = () => seeker.Seeker.SendAsync(listing, "Hello?");
        await write.Should().ThrowAsync<Exception>("a block stops new messages");
        await seeker.Seeker.RefreshActivityAsync();
        seeker.Seeker.Threads.Should().BeEmpty("the chat leaves the customer's Inbox too");
        await worker.Pro.RefreshActivityAsync();
        worker.Pro.Chats.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task CustomerReportsAWorker_TheReportIsSaved_AndAlsoBlockHidesThem()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Hugo");
        await worker.Pro.PublishAsync();
        var seeker = await TestPhone.SeekerAsync(api, "Ivy");

        await seeker.Seeker.ReportAsync(new UserReport(worker.Uid, ReportSubject.Listing, ReportReason.Scam, null, "Asked for a deposit", AlsoBlock: true));

        (await seeker.Seeker.SearchAsync()).InTown.Should().NotContain(h => h.Pro.Id == worker.Uid);
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(api.ConnectionString);
        await connection.OpenAsync();
        await using var command = new Microsoft.Data.SqlClient.SqlCommand(
            "SELECT COUNT(*) FROM Reports WHERE ReporterUid = @r AND TargetUid = @t AND Reason = 'scam' AND Status = 'open'", connection);
        command.Parameters.AddWithValue("@r", seeker.Uid);
        command.Parameters.AddWithValue("@t", worker.Uid);
        ((int)(await command.ExecuteScalarAsync())!).Should().Be(1);
    }

    [IntegrationFact]
    public async Task ReportingYourself_OrWithAnUnknownReason_IsRefused()
    {
        var phone = new TestPhone(api, "Jo");
        var client = api.Client(api.Tokens.For(phone.Uid));

        using var self = await client.PostAsJsonAsync("reports",
            new ReportRequest(phone.Uid, ReportKinds.User, ReportReasons.Other, null, null, false));
        using var badReason = await client.PostAsJsonAsync("reports",
            new ReportRequest("someone-else", ReportKinds.User, "because", null, null, false));

        self.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        badReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
