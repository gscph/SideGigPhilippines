using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>In-app chat. Every call is about the caller's own chats; nobody else's can be read.</summary>
public sealed class ChatFunctions(FirebaseTokenValidator auth, ChatStore chats, ChatNotifier notifier, UserStore users, CallerLimits limits)
{
    /// <summary>
    /// <c>GET chats?side=seeker&amp;take=30&amp;before={chatId}</c> (or <c>side=professional</c>): one page of the caller's
    /// Inbox on that side, newest first; without <c>take</c>, all of it (app versions before Inbox paging). A
    /// Skilled Worker reading theirs is active on the app (<see cref="UserStore.TouchAsync"/>, ranked in search).
    /// </summary>
    [Function("GetInbox")]
    public async Task<IActionResult> Inbox(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chats")] HttpRequest request,
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

        if (Take(request) is not { } take)
        {
            return ApiResults.BadRequest(ApiErrors.Invalid("take"));
        }

        if (!asSeeker)
        {
            await users.TouchAsync(uid, cancellationToken);
        }

        string? before = request.Query["before"];
        return new OkObjectResult(await chats.InboxAsync(uid, asSeeker, take.Count, string.IsNullOrEmpty(before) ? null : before, cancellationToken));
    }

    /// <summary>A page size from the query: <see cref="Count"/> is null when <c>take</c> was left out (old apps: everything).</summary>
    private readonly record struct PageSize(int? Count);

    /// <summary>The <c>take</c> query value, or null when it's there but isn't a positive whole number (a bad request).</summary>
    private static PageSize? Take(HttpRequest request) =>
        !request.Query.ContainsKey("take") ? new PageSize(null)
        : int.TryParse(request.Query["take"], out var count) && count >= 1 ? new PageSize(count)
        : null;

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

    /// <summary>
    /// <c>GET chats/{chatId}/messages?take=20&amp;before={messageId}</c>: one page of messages, oldest first: the newest
    /// <c>take</c> before message <c>before</c> (or the latest without it). Without <c>take</c>, every message.
    /// </summary>
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

        if (Take(request) is not { } take)
        {
            return ApiResults.BadRequest(ApiErrors.Invalid("take"));
        }

        string? before = request.Query["before"];
        return await chats.MessagesAsync(chatId, uid, take.Count, string.IsNullOrEmpty(before) ? null : before, cancellationToken) is { } messages
            ? new OkObjectResult(messages)
            : new NotFoundResult();
    }

    /// <summary>
    /// Sends a message; the first one creates the chat. Tells the other person (<see cref="ChatNotifier"/>). Answers with
    /// the chat as saved, or <c>429</c> past <see cref="Limited.Message"/> / <see cref="Limited.NewChat"/>.
    /// </summary>
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

        if (!limits.TryTake(Limited.Message, uid))
        {
            return ApiResults.TooMany();
        }

        if (await ApiResults.ReadAsync<SendMessageRequest>(request, cancellationToken) is not { } message)
        {
            return ApiResults.BadRequest();
        }

        var result = await chats.SendAsync(chatId, uid, message, () => limits.TryTake(Limited.NewChat, uid), cancellationToken);
        if (result is { Outcome: SendOutcome.Sent, IsNew: true, Chat: { } sent })
        {
            await notifier.NotifyAsync(sent, uid, cancellationToken);
        }

        return result.Outcome switch
        {
            SendOutcome.Sent => new OkObjectResult(result.Chat),
            SendOutcome.Forbidden => new StatusCodeResult(StatusCodes.Status403Forbidden),
            SendOutcome.NoListing => new NotFoundResult(),
            SendOutcome.Blocked => ApiResults.Forbidden(ApiErrors.Blocked),
            SendOutcome.TooManyChats => ApiResults.TooMany(),
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
