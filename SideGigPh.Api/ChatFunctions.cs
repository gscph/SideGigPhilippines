using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>In-app chat. Every call is about the caller's own chats; nobody else's can be read.</summary>
public sealed class ChatFunctions(FirebaseTokenValidator auth, ChatStore chats, ChatNotifier notifier)
{
    /// <summary><c>GET chats?side=seeker</c> or <c>?side=professional</c>: the caller's Inbox on that side, newest first.</summary>
    [Function("GetInbox")]
    public async Task<IActionResult> Inbox(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chats")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return Side(request) is { } asSeeker
            ? new OkObjectResult(await chats.InboxAsync(uid, asSeeker, cancellationToken))
            : ApiResults.BadRequest("side-required");
    }

    [Function("GetChat")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chats/{chatId}")] HttpRequest request,
        string chatId,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return await chats.GetAsync(chatId, uid, cancellationToken) is { } chat ? new OkObjectResult(chat) : new NotFoundResult();
    }

    [Function("GetChatMessages")]
    public async Task<IActionResult> Messages(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chats/{chatId}/messages")] HttpRequest request,
        string chatId,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        return await chats.MessagesAsync(chatId, uid, cancellationToken) is { } messages ? new OkObjectResult(messages) : new NotFoundResult();
    }

    /// <summary>Sends a message; the first one creates the chat. Tells the other person (<see cref="ChatNotifier"/>). Answers with the chat as saved.</summary>
    [Function("SendChatMessage")]
    public async Task<IActionResult> Send(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chats/{chatId}/messages")] HttpRequest request,
        string chatId,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (await ApiResults.ReadAsync<SendMessageRequest>(request, cancellationToken) is not { } message)
        {
            return ApiResults.BadRequest();
        }

        var result = await chats.SendAsync(chatId, uid, message, cancellationToken);
        if (result is { Outcome: SendOutcome.Sent, IsNew: true, Chat: { } sent })
        {
            await notifier.NotifyAsync(sent, uid, cancellationToken);
        }

        return result.Outcome switch
        {
            SendOutcome.Sent => new OkObjectResult(result.Chat),
            SendOutcome.Forbidden => new StatusCodeResult(StatusCodes.Status403Forbidden),
            SendOutcome.NoListing => new NotFoundResult(),
            _ => ApiResults.BadRequest(),
        };
    }

    /// <summary><c>POST chats/{chatId}/read?side=…</c>: the caller has read the chat (clears their unread badge).</summary>
    [Function("MarkChatRead")]
    public async Task<IActionResult> MarkRead(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chats/{chatId}/read")] HttpRequest request,
        string chatId,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        if (Side(request) is not { } asSeeker)
        {
            return ApiResults.BadRequest("side-required");
        }

        return await chats.MarkReadAsync(chatId, uid, asSeeker, cancellationToken) ? new NoContentResult() : new NotFoundResult();
    }

    /// <summary>Deletes every chat the caller is in (Start over).</summary>
    [Function("DeleteMyChats")]
    public async Task<IActionResult> DeleteMine(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "chats/mine")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await auth.GetUidAsync(request, cancellationToken) is not { } uid)
        {
            return new UnauthorizedResult();
        }

        await chats.DeleteForPersonAsync(uid, cancellationToken);
        return new NoContentResult();
    }

    /// <summary>The <c>side</c> query value: true for the Seeker side, false for the Skilled Worker side, null when missing.</summary>
    private static bool? Side(HttpRequest request) => request.Query["side"].ToString() switch
    {
        Wire.Seeker => true,
        Wire.Professional => false,
        _ => null,
    };
}
