using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using SideGigPh.Contracts;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// New-message notifications, end to end: the app registers the phone through the real API, and a message makes the
/// API send the right notification to the other person. A customer writing to a Skilled Worker rings (a slow answer
/// loses the job); a worker answering a Seeker only sets the red dot. A broken notification never breaks the message.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class NotificationTests(ApiTestHost api)
{
    [IntegrationFact]
    public async Task CustomerWritesFirst_WorkerGetsALoudNotification_AsANewCustomer()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Rico", "Liza");
        await worker.Get<ChatAlerts>().RegisterAsync();

        await seeker.Seeker.SendAsync((await seeker.Seeker.GetProAsync(worker.Uid))!, "Pa-linis po ng aircon");

        var push = api.Pushes.To(worker.Push.Token!).Should().ContainSingle().Subject;
        push.Alert.Should().BeTrue("a customer is work: the worker must notice");
        push.AndroidChannel.Should().Be(PushData.CustomersChannel);
        push.Title.Should().Be("New customer: Liza");
        push.Body.Should().Be("Pa-linis po ng aircon");
        push.Badge.Should().Be(1);
        push.Tag.Should().Be(seeker.Seeker.ChatIdWith(worker.Uid));
        push.Data[PushData.Side].Should().Be(Wire.Professional);
        ChatAlerts.PathFor(push.Data).Should().Be($"/professional/inbox/{Uri.EscapeDataString(push.Tag)}", "a tap opens the chat");
    }

    [IntegrationFact]
    public async Task CustomerWritesAgain_TitleIsJustTheirName_AndTheBadgeCountsChats()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Mon", "Tess");
        await worker.Get<ChatAlerts>().RegisterAsync();
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;

        await seeker.Seeker.SendAsync(listing, "Hello po");
        await seeker.Seeker.SendAsync(listing, "Bukas po ba?");

        var second = api.Pushes.To(worker.Push.Token!).Should().HaveCount(2).And.Subject.Last();
        second.Title.Should().Be("Tess");
        second.Badge.Should().Be(1, "the badge counts unread chats, not messages");
    }

    [IntegrationFact]
    public async Task WorkerReplies_SeekerGetsOnlyAQuietDot()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Pia", "Odie");
        await seeker.Get<ChatAlerts>().RegisterAsync();
        await seeker.Seeker.SendAsync((await seeker.Seeker.GetProAsync(worker.Uid))!, "Hi po");
        await worker.Pro.RefreshActivityAsync();

        await worker.Pro.ReplyAsync(worker.Pro.Chats.Single(), "Available po");

        var push = api.Pushes.To(seeker.Push.Token!).Should().ContainSingle().Subject;
        push.Alert.Should().BeFalse("Seekers didn't sign up for a noisy app");
        push.AndroidChannel.Should().Be(PushData.RepliesChannel);
        push.Title.Should().Be("Pia");
        push.Badge.Should().Be(1);
        ChatAlerts.PathFor(push.Data).Should().Be($"/inbox/{Uri.EscapeDataString(worker.Uid)}");
    }

    [IntegrationFact]
    public async Task CallLogged_TellsTheWorkerTheyWereCalled_InTheirLanguage()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Toto", "Ella");
        await worker.Account.SetLanguageAsync(AppLanguage.Taglish);
        await worker.Account.SaveProfileAsync(); // the switch saves the server's copy in the background: wait for it here
        await worker.Get<ChatAlerts>().RegisterAsync();

        await seeker.Seeker.LogCallAsync((await seeker.Seeker.GetProAsync(worker.Uid))!);

        var push = api.Pushes.To(worker.Push.Token!).Should().ContainSingle().Subject;
        push.Title.Should().Be("Bagong customer: Ella");
        push.Body.Should().Be("Tumawag sa'yo");
    }

    [IntegrationFact]
    public async Task NoPhoneRegistered_MessageStillSends_NobodyIsNotified()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Nilo", "Gina");

        await seeker.Seeker.SendAsync((await seeker.Seeker.GetProAsync(worker.Uid))!, "Hello po");

        api.Pushes.To(worker.Push.Token!).Should().BeEmpty();
        await worker.Pro.RefreshActivityAsync();
        worker.Pro.UnreadCount.Should().Be(1);
    }

    [IntegrationFact]
    public async Task SameMessageSentTwice_NotifiesOnce()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Rey", "Joy");
        await worker.Get<ChatAlerts>().RegisterAsync();
        var chatId = $"{worker.Uid}_{seeker.Uid}";
        var message = new SendMessageRequest("retry-1", worker.Uid, seeker.Uid, Wire.Text, "Hello po", "Rey", "Joy", null, null, null, null);
        var client = api.Client(api.Tokens.For(seeker.Uid, seeker.PhoneNumber));

        using var first = await client.PostAsJsonAsync($"chats/{chatId}/messages", message);
        using var retry = await client.PostAsJsonAsync($"chats/{chatId}/messages", message);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        retry.StatusCode.Should().Be(HttpStatusCode.OK);
        api.Pushes.To(worker.Push.Token!).Should().ContainSingle("a retry after a dropped connection must not ring twice");
    }

    [IntegrationFact]
    public async Task TokenGoogleSaysIsDead_IsForgotten()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Dodong", "Inday");
        await worker.Get<ChatAlerts>().RegisterAsync();
        api.Pushes.Dead[worker.Push.Token!] = true;
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;

        await seeker.Seeker.SendAsync(listing, "Hello po");
        await seeker.Seeker.SendAsync(listing, "Andiyan po ba kayo?");

        api.Pushes.To(worker.Push.Token!).Should().ContainSingle("the dead token was deleted after the first send");
    }

    [IntegrationFact]
    public async Task PhoneChangesAccount_OnlyTheNewAccountIsNotified()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Lito", "Nena");
        await worker.Get<ChatAlerts>().RegisterAsync();
        var other = await TestPhone.SeekerAsync(api, "Borrower");
        other.Push.Token = worker.Push.Token;

        await other.Get<ChatAlerts>().RegisterAsync();
        await seeker.Seeker.SendAsync((await seeker.Seeker.GetProAsync(worker.Uid))!, "Hello po");

        api.Pushes.To(worker.Push.Token!).Should().BeEmpty("the phone now belongs to another account");
    }

    [IntegrationFact]
    public async Task StartOver_ForgetsThePhone()
    {
        var seeker = await TestPhone.SeekerAsync(api, "Lea");
        await seeker.Get<ChatAlerts>().RegisterAsync();
        (await DevicesOf(seeker.Uid)).Should().Be(1);

        await seeker.Account.StartOverAsync();

        (await DevicesOf(seeker.Uid)).Should().Be(0);
    }

    [IntegrationFact]
    public async Task BadDevice_Is400()
    {
        var client = api.Client(api.Tokens.For("device-tester"));

        using var noToken = await client.PutAsJsonAsync("users/me/devices", new SaveDeviceRequest("", Wire.Android));
        using var badPlatform = await client.PutAsJsonAsync("users/me/devices", new SaveDeviceRequest("abc", "windows"));
        using var spaces = await client.PutAsJsonAsync("users/me/devices", new SaveDeviceRequest("a b", Wire.Ios));

        noToken.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        badPlatform.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        spaces.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(TestPhone Worker, TestPhone Seeker)> WorkerAndSeekerAsync(string workerName, string seekerName)
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, workerName);
        await worker.Pro.PublishAsync();
        return (worker, await TestPhone.SeekerAsync(api, seekerName));
    }

    private async Task<int> DevicesOf(string uid)
    {
        await using var connection = new SqlConnection(api.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.PushDevices WHERE Uid = @uid", connection);
        command.Parameters.AddWithValue("@uid", uid);
        return (int)(await command.ExecuteScalarAsync())!;
    }
}
