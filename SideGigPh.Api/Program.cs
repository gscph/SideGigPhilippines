using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SideGigPh.Api;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Settings come from the Function App's "Environment variables" in Azure, or local.settings.json.
var photoStorage = builder.Configuration["PhotoStorage"]
    ?? throw new InvalidOperationException("Missing setting PhotoStorage (the storage account connection string).");
var firebaseProjectId = builder.Configuration["FirebaseProjectId"]
    ?? throw new InvalidOperationException("Missing setting FirebaseProjectId.");

// PhotoFunctions creates the container on the first upload if it's missing.
builder.Services.AddSingleton(new BlobServiceClient(photoStorage).GetBlobContainerClient(PhotoFunctions.Container));
builder.Services.AddSingleton(new FirebaseTokenValidator(firebaseProjectId));

builder.Build().Run();
