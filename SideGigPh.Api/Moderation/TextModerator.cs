using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace SideGigPh.Api.Moderation;

/// <summary>What an outside service said about a text.</summary>
public enum ModeratorVerdict
{
    /// <summary>Nothing found.</summary>
    Clean,

    /// <summary>Sexual, hateful, violent or self-harm content past <see cref="ModerationRules"/>.</summary>
    Flagged,

    /// <summary>The service couldn't be asked (not set up, down, slow, out of free calls): the word list alone decided.</summary>
    Unchecked,
}

/// <summary>The port to an outside moderation service. Never throws: a service that can't be reached answers <see cref="ModeratorVerdict.Unchecked"/>.</summary>
public interface ITextModerator
{
    Task<ModeratorVerdict> CheckAsync(string text, CancellationToken cancellationToken);
}

/// <summary>No service is set up (a PC, or before <c>azure/cloudshell-content-safety.sh</c>): only <see cref="RudeWords"/> checks.</summary>
public sealed class NoTextModerator : ITextModerator
{
    public Task<ModeratorVerdict> CheckAsync(string text, CancellationToken cancellationToken) => Task.FromResult(ModeratorVerdict.Unchecked);
}

/// <summary>The severities (0, 2, 4, 6) from which Azure AI Content Safety's answer blocks a text.</summary>
public static class ModerationRules
{
    /// <summary>Sexual content: blocked from "low" (2). A home-services app has no reason for any.</summary>
    public const int Sexual = 2;

    /// <summary>Hate (slurs, attacks on a group): blocked from "low" (2).</summary>
    public const int Hate = 2;

    /// <summary>Violence: blocked from "medium" (4), so "the pipe burst and hit me" passes.</summary>
    public const int Violence = 4;

    /// <summary>Self-harm: blocked from "medium" (4).</summary>
    public const int SelfHarm = 4;

    /// <summary>The longest text sent (the service takes 10,000 characters); longer text is checked in its first part.</summary>
    public const int MaxChars = 10_000;

    public static bool Blocks(string category, int severity) => category switch
    {
        "Sexual" => severity >= Sexual,
        "Hate" => severity >= Hate,
        "Violence" => severity >= Violence,
        "SelfHarm" => severity >= SelfHarm,
        _ => false,
    };
}

/// <summary>
/// Azure AI Content Safety, text analysis over its REST API (<c>text:analyze</c>, four severity levels). Settings
/// <c>ContentSafetyEndpoint</c> and, only when the Function App's managed identity has no role on the resource,
/// <c>ContentSafetyKey</c>; without a key it signs in as the managed identity (role "Cognitive Services User",
/// <c>azure/cloudshell-content-safety.sh</c>). The text is never logged.
/// </summary>
public sealed class ContentSafetyModerator(HttpClient http, string endpoint, string? key, TokenCredential? identity, ILogger<ContentSafetyModerator> logger)
    : ITextModerator
{
    public const string EndpointSetting = "ContentSafetyEndpoint";
    public const string KeySetting = "ContentSafetyKey";

    private const string ApiVersion = "2024-09-01";
    private const string KeyHeader = "Ocp-Apim-Subscription-Key";
    private static readonly string[] Scopes = ["https://cognitiveservices.azure.com/.default"];

    /// <summary>How long a write waits for the answer before going on with the word list's say only.</summary>
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(4);

    public async Task<ModeratorVerdict> CheckAsync(string text, CancellationToken cancellationToken)
    {
        using var patience = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        patience.CancelAfter(Patience);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint.TrimEnd('/')}/contentsafety/text:analyze?api-version={ApiVersion}")
            {
                Content = JsonContent.Create(new
                {
                    text = text.Length > ModerationRules.MaxChars ? text[..ModerationRules.MaxChars] : text,
                    categories = new[] { "Hate", "Sexual", "Violence", "SelfHarm" },
                    outputType = "FourSeverityLevels",
                }),
            };
            if (!string.IsNullOrEmpty(key))
            {
                request.Headers.Add(KeyHeader, key);
            }
            else if (identity is not null)
            {
                var token = await identity.GetTokenAsync(new TokenRequestContext(Scopes), patience.Token);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            }

            using var answer = await http.SendAsync(request, patience.Token);
            if (!answer.IsSuccessStatusCode)
            {
                logger.LogWarning("Moderation: Content Safety answered {Status}; the word list alone decided.", (int)answer.StatusCode);
                return ModeratorVerdict.Unchecked;
            }

            using var json = JsonDocument.Parse(await answer.Content.ReadAsStringAsync(patience.Token));
            if (!json.RootElement.TryGetProperty("categoriesAnalysis", out var categories))
            {
                return ModeratorVerdict.Unchecked;
            }

            foreach (var category in categories.EnumerateArray())
            {
                var name = category.TryGetProperty("category", out var c) ? c.GetString() : null;
                var severity = category.TryGetProperty("severity", out var s) && s.TryGetInt32(out var level) ? level : 0;
                if (name is not null && ModerationRules.Blocks(name, severity))
                {
                    logger.LogInformation("Moderation: Content Safety flagged {Category} at {Severity}.", name, severity);
                    return ModeratorVerdict.Flagged;
                }
            }

            return ModeratorVerdict.Clean;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or AuthenticationFailedException or CredentialUnavailableException or RequestFailedException)
        {
            logger.LogWarning("Moderation: Content Safety couldn't be asked ({Error}); the word list alone decided.", ex.GetType().Name);
            return ModeratorVerdict.Unchecked;
        }
    }
}
