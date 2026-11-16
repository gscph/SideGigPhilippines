using Microsoft.Extensions.Logging;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// Notifications for job posts (brief item 10). A new post rings the workers of its trade who travel that far (the
/// same match as their Requests feed: <see cref="JobPostStore.WorkersToAlertAsync"/>), at most <see cref="MaxWorkers"/>,
/// a service they offer first, then nearest. An offer rings the household that posted: offers are what it waits for.
/// The household's point and the worker's number never go into a notification. A failed notification never fails
/// the post or the offer.
/// </summary>
public sealed class JobAlerts(JobPostStore posts, PushDeviceStore devices, IPushSender sender, ILogger<JobAlerts> logger)
{
    /// <summary>The most workers one new post rings (the best matches first): enough to get offers, not a broadcast.</summary>
    public const int MaxWorkers = 30;

    public async Task NewPostAsync(JobPostDto post, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var uid in await posts.WorkersToAlertAsync(post.Id, MaxWorkers, cancellationToken))
            {
                if (await devices.TargetAsync(uid, asSeeker: false, cancellationToken) is { } target)
                {
                    await devices.RemoveAsync(await sender.SendAsync(PostMessage(post, target), cancellationToken), cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't notify workers about job post {PostId}", post.Id);
        }
    }

    public async Task NewOfferAsync(long postId, string workerUid, CancellationToken cancellationToken)
    {
        try
        {
            if (await posts.OfferAlertAsync(postId, workerUid, cancellationToken) is { } offer
                && await devices.TargetAsync(offer.SeekerUid, asSeeker: true, cancellationToken) is { } target)
            {
                await devices.RemoveAsync(await sender.SendAsync(OfferMessage(postId, offer, target), cancellationToken), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't notify about an offer on job post {PostId}", postId);
        }
    }

    internal static PushMessage PostMessage(JobPostDto post, PushTarget target)
    {
        var taglish = target.Language == Wire.Taglish;
        return new PushMessage(
            target.Tokens,
            taglish ? "May bagong request malapit sa'yo" : "New request near you",
            $"₱{post.BudgetPesos:N0} · {ChatNotifier.Preview(post.Description)}",
            Alert: true,
            Tag: $"post-{post.Id}",
            Badge: target.UnreadChats,
            AndroidChannel: PushData.RequestsChannel,
            new Dictionary<string, string>
            {
                [PushData.PostId] = post.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [PushData.Side] = Wire.Professional,
                ["is_silent_in_foreground"] = "true",
            });
    }

    internal static PushMessage OfferMessage(long postId, OfferAlert offer, PushTarget target)
    {
        var taglish = target.Language == Wire.Taglish;
        return new PushMessage(
            target.Tokens,
            taglish ? $"Bagong offer: {offer.WorkerName}" : $"New offer: {offer.WorkerName}",
            taglish ? $"₱{offer.PricePesos:N0} para sa post mo" : $"₱{offer.PricePesos:N0} for your post",
            Alert: true,
            // One line per post: a second offer replaces the first notification instead of piling up.
            Tag: $"offers-{postId}",
            Badge: target.UnreadChats,
            AndroidChannel: PushData.OffersChannel,
            new Dictionary<string, string>
            {
                [PushData.PostId] = postId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [PushData.Side] = Wire.Seeker,
                ["is_silent_in_foreground"] = "true",
            });
    }
}
