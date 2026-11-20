using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace SideGigPh.Api.Insights;

/// <summary>
/// The port to an embeddings model: texts in, one vector each out, so texts that mean the same can be found
/// (<see cref="SideGigPh.AzureSQL.OtherJobGrouping"/>). Never throws: null when the model can't be reached.
/// </summary>
public interface ITextEmbedder
{
    /// <summary>
    /// What makes the vectors (deployment and size): vectors from different models can't be compared, so the cache
    /// (<c>OtherJobTextVectors.Model</c>) keeps them apart.
    /// </summary>
    string Model { get; }

    /// <summary>One vector per text, in order; null when the model couldn't be asked.</summary>
    Task<IReadOnlyList<float[]>?> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}

/// <summary>No model is set up (a PC, or before the embeddings deployment exists): texts are grouped only when they match exactly.</summary>
public sealed class NoTextEmbedder : ITextEmbedder
{
    public string Model => "none";

    public Task<IReadOnlyList<float[]>?> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<float[]>?>(null);
}

/// <summary>
/// An Azure OpenAI embeddings deployment over its REST API. Settings <c>EmbeddingsEndpoint</c>, optional
/// <c>EmbeddingsDeployment</c> (default <see cref="DefaultDeployment"/>) and, only when the Function App's managed
/// identity has no role on the resource, <c>EmbeddingsKey</c>; without a key it signs in as the managed identity
/// (role "Cognitive Services OpenAI User"). Vectors are cut to <see cref="Dimensions"/> numbers by the model itself
/// (text-embedding-3 models only), small enough to cache thousands. Texts are never logged.
/// </summary>
public sealed class AzureOpenAiEmbedder(HttpClient http, string endpoint, string deployment, string? key, TokenCredential? identity, ILogger<AzureOpenAiEmbedder> logger)
    : ITextEmbedder
{
    public const string EndpointSetting = "EmbeddingsEndpoint";
    public const string DeploymentSetting = "EmbeddingsDeployment";
    public const string KeySetting = "EmbeddingsKey";
    public const string DefaultDeployment = "text-embedding-3-small";

    /// <summary>Numbers per vector: 1 KB each, and enough to tell short job names apart.</summary>
    public const int Dimensions = 256;

    /// <summary>The most texts one call sends (the service takes more; smaller calls fail smaller).</summary>
    public const int Batch = 100;

    private const string ApiVersion = "2024-10-21";
    private const string KeyHeader = "api-key";
    private static readonly string[] Scopes = ["https://cognitiveservices.azure.com/.default"];

    /// <summary>How long one call of <see cref="Batch"/> texts may take before the page groups exact matches only.</summary>
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(20);

    public string Model => $"{deployment}/{Dimensions}";

    public async Task<IReadOnlyList<float[]>?> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        var vectors = new List<float[]>(texts.Count);
        foreach (var batch in texts.Chunk(Batch))
        {
            if (await EmbedBatchAsync(batch, cancellationToken) is not { } part)
            {
                return null;
            }

            vectors.AddRange(part);
        }

        return vectors;
    }

    private async Task<float[][]?> EmbedBatchAsync(string[] texts, CancellationToken cancellationToken)
    {
        using var patience = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        patience.CancelAfter(Patience);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{endpoint.TrimEnd('/')}/openai/deployments/{Uri.EscapeDataString(deployment)}/embeddings?api-version={ApiVersion}")
            {
                Content = JsonContent.Create(new { input = texts, dimensions = Dimensions }),
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
                logger.LogWarning("Embeddings: Azure OpenAI answered {Status}; grouping exact matches only.", (int)answer.StatusCode);
                return null;
            }

            using var json = JsonDocument.Parse(await answer.Content.ReadAsStringAsync(patience.Token));
            var vectors = new float[texts.Length][];
            foreach (var item in json.RootElement.GetProperty("data").EnumerateArray())
            {
                var index = item.GetProperty("index").GetInt32();
                if (index >= 0 && index < vectors.Length)
                {
                    vectors[index] = item.GetProperty("embedding").EnumerateArray().Select(n => n.GetSingle()).ToArray();
                }
            }

            return vectors.All(v => v is not null) ? vectors : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or KeyNotFoundException
                                       or InvalidOperationException or FormatException or AuthenticationFailedException
                                       or CredentialUnavailableException or RequestFailedException)
        {
            logger.LogWarning("Embeddings: Azure OpenAI couldn't be asked ({Error}); grouping exact matches only.", ex.GetType().Name);
            return null;
        }
    }
}
