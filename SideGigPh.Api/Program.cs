using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SideGigPh.Api;
using SideGigPh.Api.Moderation;
using SideGigPh.AzureSQL;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Exceptions come back as JSON, not the host's empty 500; with the stack trace unless ShowErrorDetails is "false".
builder.UseMiddleware<ErrorDetailsMiddleware>();

// JSON answers go out gzipped to callers that accept it (the app does); the Function App doesn't compress by itself.
builder.UseMiddleware<GzipMiddleware>();
ApiResults.ShowDetails = !string.Equals(builder.Configuration[ApiResults.ShowDetailsSetting], "false", StringComparison.OrdinalIgnoreCase);

// Settings come from the Function App's "Environment variables" in Azure, or local.settings.json.
var photoStorage = builder.Configuration["PhotoStorage"]
    ?? throw new InvalidOperationException("Missing setting PhotoStorage (the storage account connection string).");
var sqlDatabase = builder.Configuration["SqlDatabase"]
    ?? throw new InvalidOperationException("Missing setting SqlDatabase (the Azure SQL connection string).");
var firebaseProjectId = builder.Configuration["FirebaseProjectId"]
    ?? throw new InvalidOperationException("Missing setting FirebaseProjectId.");

// PhotoFunctions creates the container on the first upload if it's missing.
var photos = new BlobServiceClient(photoStorage).GetBlobContainerClient(PhotoFunctions.Container);
builder.Services.AddSingleton(photos);
builder.Services.AddSingleton(new FirebaseTokenValidator(firebaseProjectId));
builder.Services.AddSingleton<CallerLimits>();

// Users, listings, reviews and chats in Azure SQL. Listing photos are stored as ids and shown from the public container.
builder.Services.AddSideGigPhSql(sqlDatabase, new PhotoLinks($"{photos.Uri.ToString().TrimEnd('/')}/"));

// New-message notifications through Firebase Cloud Messaging, and deleting sign-in accounts (Delete account). Without the
// service account (a PC), messages still send and nobody is notified, and the app deletes its own sign-in account.
if (builder.Configuration["FirebaseServiceAccount"] is { Length: > 0 } serviceAccount)
{
    builder.Services.AddSingleton<IPushSender>(new FcmPushSender(serviceAccount, firebaseProjectId));
    builder.Services.AddSingleton<IAccountDeleter>(new FirebaseAccountDeleter(serviceAccount, firebaseProjectId));
}
else
{
    builder.Services.AddSingleton<IPushSender, NoPushSender>();
    builder.Services.AddSingleton<IAccountDeleter, NoAccountDeleter>();
}

builder.Services.AddScoped<ChatNotifier>();
builder.Services.AddScoped<JobAlerts>();

// Worker verification: proofs go to a private container; an ID is read by Azure AI Document Intelligence when it's set
// up (azure/cloudshell-document-intelligence.sh), else the operator checks by eye only. Operators are named in AdminUids.
builder.Services.AddSingleton<IPrivateFiles>(new BlobPrivateFiles(new BlobServiceClient(photoStorage).GetBlobContainerClient(BlobPrivateFiles.Container)));
builder.Services.AddSingleton(new AdminList(builder.Configuration[AdminList.Setting]));
if (builder.Configuration[DocumentIntelligenceIdReader.EndpointSetting] is { Length: > 0 } readerEndpoint
    && builder.Configuration[DocumentIntelligenceIdReader.KeySetting] is { Length: > 0 } readerKey)
{
    builder.Services.AddSingleton<IIdDocumentReader>(services => new DocumentIntelligenceIdReader(
        new HttpClient(), readerEndpoint, readerKey, services.GetRequiredService<ILogger<DocumentIntelligenceIdReader>>()));
}
else
{
    builder.Services.AddSingleton<IIdDocumentReader, NoIdDocumentReader>();
}

// Moderation of what people write: Tarbaho's word list always, Azure AI Content Safety when it's set up
// (azure/cloudshell-content-safety.sh: the endpoint, signed in as the Function App's managed identity; a key only if given).
if (builder.Configuration[ContentSafetyModerator.EndpointSetting] is { Length: > 0 } safetyEndpoint)
{
    var safetyKey = builder.Configuration[ContentSafetyModerator.KeySetting];
    builder.Services.AddSingleton<ITextModerator>(services => new ContentSafetyModerator(
        new HttpClient(), safetyEndpoint, safetyKey, string.IsNullOrEmpty(safetyKey) ? new DefaultAzureCredential() : null,
        services.GetRequiredService<ILogger<ContentSafetyModerator>>()));
}
else
{
    builder.Services.AddSingleton<ITextModerator, NoTextModerator>();
}

builder.Services.AddSingleton<TextModeration>();

builder.Build().Run();
