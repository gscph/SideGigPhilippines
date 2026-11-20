using System.Net;
using System.Net.Http.Json;
using SideGigPh.Contracts;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Chat messages with rude or sexual words are refused before anything happens: no chat is started, no message is
/// saved, nobody's phone rings. The word list catches what it knows; the outside service (stand-in
/// <see cref="FakeTextModerator"/>) catches the rest. The app sees <see cref="TextNotAllowedException"/>.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class ChatModerationTests(ApiTestHost api)
{
    [IntegrationFact]
    public async Task FirstMessageWithARudeWord_IsRefused_NoChatStarts_NobodyIsNotified()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Caloy", "Mila");
        await worker.Get<ChatAlerts>().RegisterAsync();
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;

        var send = () => seeker.Seeker.SendAsync(listing, "gago ka");

        await send.Should().ThrowAsync<TextNotAllowedException>();
        api.Pushes.To(worker.Push.Token!).Should().BeEmpty("a refused message rings nobody");
        (await seeker.Seeker.GetMessagesAsync(worker.Uid)).Count.Should().Be(0);
        await worker.Pro.RefreshActivityAsync();
        worker.Pro.Chats.Should().BeEmpty("a refused first message starts no chat");
    }

    [IntegrationFact]
    public async Task MessageTheServiceFlags_IsRefused_WithTheErrorCode()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Badong", "Rosa");
        await worker.Get<ChatAlerts>().RegisterAsync();
        var chatId = $"{worker.Uid}_{seeker.Uid}";
        var message = new SendMessageRequest("flagged-1", worker.Uid, seeker.Uid, Wire.Text,
            $"Hello {FakeTextModerator.Marker}", "Badong", "Rosa", null, null, null, null);

        using var response = await api.Client(api.Tokens.For(seeker.Uid, seeker.PhoneNumber)).PostAsJsonAsync($"chats/{chatId}/messages", message);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.TextNotAllowed);
        api.Pushes.To(worker.Push.Token!).Should().BeEmpty();
        (await seeker.Seeker.GetMessagesAsync(worker.Uid)).Count.Should().Be(0);
    }

    [IntegrationFact]
    public async Task WorkerReplyWithARudeWord_IsRefused_TheChatKeepsOnlyTheCleanMessage()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Ruben", "Lorna");
        await seeker.Get<ChatAlerts>().RegisterAsync();
        await seeker.Seeker.SendAsync((await seeker.Seeker.GetProAsync(worker.Uid))!, "Pa-linis po ng aircon");
        await worker.Pro.RefreshActivityAsync();
        var chat = worker.Pro.Chats.Single();

        var reply = () => worker.Pro.ReplyAsync(chat, "gago ka");

        await reply.Should().ThrowAsync<TextNotAllowedException>();
        api.Pushes.To(seeker.Push.Token!).Should().BeEmpty();
        var messages = await worker.Pro.GetMessagesAsync(chat.Id);
        messages.Messages.Should().ContainSingle().Which.Text.Should().Be("Pa-linis po ng aircon");
    }

    [IntegrationFact]
    public async Task CleanMessage_StillSends_AndRings()
    {
        var (worker, seeker) = await WorkerAndSeekerAsync("Pido", "Nora");
        await worker.Get<ChatAlerts>().RegisterAsync();

        var sent = await seeker.Seeker.SendAsync((await seeker.Seeker.GetProAsync(worker.Uid))!, "Kailan po kayo available?");

        sent.Text.Should().Be("Kailan po kayo available?");
        api.Pushes.To(worker.Push.Token!).Should().ContainSingle();
    }

    private async Task<(TestPhone Worker, TestPhone Seeker)> WorkerAndSeekerAsync(string workerName, string seekerName)
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, workerName);
        await worker.Pro.PublishAsync();
        return (worker, await TestPhone.SeekerAsync(api, seekerName));
    }
}
