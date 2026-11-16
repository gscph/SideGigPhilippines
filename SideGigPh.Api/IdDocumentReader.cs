using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// Reads the name, number and expiry off a photo of a government ID, to help the operator who approves it. It never
/// decides anything: a person compares the photo with the worker and approves or rejects.
/// </summary>
public interface IIdDocumentReader
{
    /// <summary>What was read. Never throws: a reader that can't be reached answers <see cref="IdScan.Failed"/>.</summary>
    Task<IdScan> ReadAsync(byte[] jpeg, CancellationToken cancellationToken);
}

/// <summary>No reader is set up (a PC, or before <c>azure/cloudshell-document-intelligence.sh</c>): the operator checks by eye only.</summary>
public sealed class NoIdDocumentReader : IIdDocumentReader
{
    public Task<IdScan> ReadAsync(byte[] jpeg, CancellationToken cancellationToken) => Task.FromResult(IdScan.None);
}

/// <summary>
/// Azure AI Document Intelligence, prebuilt ID model (<c>prebuilt-idDocument</c>), over its REST API: send the image,
/// then ask for the result until it's ready. Settings <c>DocumentIntelligenceEndpoint</c> and
/// <c>DocumentIntelligenceKey</c>. The free tier reads 500 pages a month; past that it answers 429/403 and the scan is
/// <see cref="ScanStatuses.Failed"/> (the request still reaches the operator).
/// </summary>
public sealed class DocumentIntelligenceIdReader(HttpClient http, string endpoint, string key, ILogger<DocumentIntelligenceIdReader> logger) : IIdDocumentReader
{
    public const string EndpointSetting = "DocumentIntelligenceEndpoint";
    public const string KeySetting = "DocumentIntelligenceKey";

    private const string ApiVersion = "2024-11-30";
    private const string KeyHeader = "Ocp-Apim-Subscription-Key";

    /// <summary>How long an upload waits for the read before going to the operator without it.</summary>
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(25);

    private static readonly TimeSpan PollEvery = TimeSpan.FromSeconds(1);

    public async Task<IdScan> ReadAsync(byte[] jpeg, CancellationToken cancellationToken)
    {
        using var patience = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        patience.CancelAfter(Patience);
        try
        {
            using var start = new HttpRequestMessage(
                HttpMethod.Post, $"{endpoint.TrimEnd('/')}/documentintelligence/documentModels/prebuilt-idDocument:analyze?api-version={ApiVersion}")
            {
                Content = new ByteArrayContent(jpeg) { Headers = { ContentType = new MediaTypeHeaderValue("image/jpeg") } },
            };
            start.Headers.Add(KeyHeader, key);
            using var started = await http.SendAsync(start, patience.Token);
            if (!started.IsSuccessStatusCode || !started.Headers.TryGetValues("Operation-Location", out var locations))
            {
                logger.LogWarning("ID scan: the reader refused the image ({Status}).", (int)started.StatusCode);
                return IdScan.Failed;
            }

            var resultUrl = locations.First();
            while (true)
            {
                await Task.Delay(PollEvery, patience.Token);
                using var poll = new HttpRequestMessage(HttpMethod.Get, resultUrl);
                poll.Headers.Add(KeyHeader, key);
                using var answer = await http.SendAsync(poll, patience.Token);
                if (!answer.IsSuccessStatusCode)
                {
                    logger.LogWarning("ID scan: the reader's result answered {Status}.", (int)answer.StatusCode);
                    return IdScan.Failed;
                }

                using var json = JsonDocument.Parse(await answer.Content.ReadAsStringAsync(patience.Token));
                switch (json.RootElement.TryGetProperty("status", out var status) ? status.GetString() : null)
                {
                    case "succeeded":
                        return FromResult(json.RootElement);
                    case "failed":
                        logger.LogWarning("ID scan: the reader couldn't analyze the image.");
                        return IdScan.Failed;
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            // Only the kind of failure: the image and anything read from it never go into the logs.
            logger.LogWarning("ID scan: {Failure}.", ex.GetType().Name);
            return IdScan.Failed;
        }
    }

    /// <summary>The fields of the first document in a finished analysis (<c>analyzeResult.documents[0].fields</c>).</summary>
    public static IdScan FromResult(JsonElement result)
    {
        if (!result.TryGetProperty("analyzeResult", out var analysis)
            || !analysis.TryGetProperty("documents", out var documents) || documents.ValueKind != JsonValueKind.Array || documents.GetArrayLength() == 0
            || !documents[0].TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Object)
        {
            return new IdScan(ScanStatuses.Unreadable);
        }

        var name = string.Join(' ', new[] { Text(fields, "FirstName"), Text(fields, "MiddleName"), Text(fields, "LastName") }.Where(part => part is not null));
        var number = Text(fields, "DocumentNumber") ?? Text(fields, "PersonalNumber");
        var expiry = fields.TryGetProperty("DateOfExpiration", out var date) && date.TryGetProperty("valueDate", out var value)
            && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : (DateOnly?)null;
        var type = documents[0].TryGetProperty("docType", out var docType) ? docType.GetString() : null;

        return name.Length == 0 && number is null && expiry is null
            ? new IdScan(ScanStatuses.Unreadable, type)
            : new IdScan(ScanStatuses.Read, type, name.Length == 0 ? null : name, number, expiry);
    }

    private static string? Text(JsonElement fields, string name) =>
        fields.TryGetProperty(name, out var field)
        && (field.TryGetProperty("valueString", out var text) || field.TryGetProperty("content", out text))
        && text.GetString() is { Length: > 0 } value ? value.Trim() : null;
}
