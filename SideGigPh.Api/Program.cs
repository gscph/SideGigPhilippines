using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SideGigPh.Api;
using SideGigPh.AzureSQL;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

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

// Users, listings, reviews and chats in Azure SQL. Listing photos are stored as ids and shown from the public container.
builder.Services.AddSideGigPhSql(sqlDatabase, new PhotoLinks($"{photos.Uri.ToString().TrimEnd('/')}/"));

builder.Build().Run();
