using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SideGigPh.Api;
using SideGigPh.Api.Telemetry;
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
builder.Services.AddSingleton(TimeProvider.System);

// Error reports and "Others" notes in Table Storage (same account as the photos), so they never wake the database.
builder.Services.AddSingleton(TelemetryTables.In(photoStorage));
builder.Services.AddSingleton<ErrorReportTable>();
builder.Services.AddSingleton<OtherJobNoteTable>();

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

builder.Build().Run();
