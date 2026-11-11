using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Firestore;

/// <summary>
/// <see cref="IProfessionalProfileRepository"/> on <c>professionals/{uid}</c>: one listing per account.
/// Search is two equality filters (<c>categoryId</c>, <c>areaId</c>), which need no composite index; towns
/// inside the area are sorted out on the phone by <c>SearchRanker</c>.
/// Anti-fraud rule 1 (one number, one listing) holds without a check here: Firebase Auth allows one account
/// per phone number and the rules make <c>mobileNumber</c> the account's verified number, so
/// <see cref="PublishResult.PhoneTaken"/> never comes back from this adapter.
/// </summary>
public sealed partial class FirestoreProfessionalProfileRepository(ILogger<FirestoreProfessionalProfileRepository> logger)
    : IProfessionalProfileRepository
{
    public Task<bool> HasProfileAsync(string uid) => FirestoreAccess.ReadAsync(async () =>
    {
        var snapshot = await FirestoreAccess.Db.GetDocument($"{FirestoreAccess.Professionals}/{uid}").GetDocumentSnapshotAsync<ProfessionalDoc>();
        return snapshot.Data is not null;
    }, false, logger, $"whether {uid} has a listing");

    public Task<IReadOnlyList<ProfessionalListing>> SearchAsync(ProfessionalSearchQuery query) => FirestoreAccess.ReadAsync<IReadOnlyList<ProfessionalListing>>(async () =>
    {
        var docs = FirestoreAccess.Db.GetCollection(FirestoreAccess.Professionals).WhereEqualsTo("categoryId", query.CategoryId);
        if (query.AreaId is not null)
        {
            docs = docs.WhereEqualsTo("areaId", query.AreaId);
        }

        var snapshot = await docs.GetDocumentsAsync<ProfessionalDoc>();
        return snapshot.Documents.Select(d => ToListing(d.Reference.Id, d.Data)).ToList();
    }, [], logger, $"pros in {query.CategoryId}/{query.AreaId}");

    public Task<ProfessionalListing?> GetByIdAsync(string professionalId) => FirestoreAccess.ReadAsync(async () =>
    {
        var snapshot = await FirestoreAccess.Db.GetDocument($"{FirestoreAccess.Professionals}/{professionalId}").GetDocumentSnapshotAsync<ProfessionalDoc>();
        return snapshot.Data is { } doc ? ToListing(professionalId, doc) : null;
    }, null, logger, $"the listing of {professionalId}");

    public async Task<PublishResult> PublishAsync(ProfessionalListing listing)
    {
        try
        {
            var path = $"{FirestoreAccess.Professionals}/{listing.Id}";
            var document = FirestoreAccess.Db.GetDocument(path);
            var now = DateTimeOffset.Now;

            // publishedAt is when it first went live: keep it when the pro edits a live listing.
            var existing = (await document.GetDocumentSnapshotAsync<ProfessionalDoc>()).Data;
            await document.SetDataAsync(ToDoc(listing, existing?.PublishedAt ?? now, now));
            return PublishResult.Published;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Firestore: couldn't publish the listing of {Id}.", listing.Id);
            throw;
        }
    }

    public async Task RemoveAsync(string professionalId)
    {
        try
        {
            await FirestoreAccess.Db.GetDocument($"{FirestoreAccess.Professionals}/{professionalId}").DeleteDocumentAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Firestore: couldn't remove the listing of {Id}.", professionalId);
            throw;
        }
    }

    private ProfessionalDoc ToDoc(ProfessionalListing listing, DateTimeOffset publishedAt, DateTimeOffset now)
    {
        var photos = listing.WorkPhotoUrls.Select(ToPhoto).OfType<PhotoDoc>().Take(ProWorkspace.MaxWorkPhotos).ToList();
        return new ProfessionalDoc
        {
            DisplayName = FirestoreAccess.Name(listing.DisplayName),
            MobileNumber = listing.MobileNumber,
            CategoryId = listing.CategoryIds.FirstOrDefault() ?? string.Empty,
            ServiceIds = [.. listing.ServiceIds],
            AreaId = ReferenceData.AreaOf(listing.CityIds.FirstOrDefault()) ?? string.Empty,
            CityIds = [.. listing.CityIds],
            ExperienceId = listing.ExperienceId,
            RateTierId = listing.RateTierId,
            ProfilePhoto = ToPhoto(listing.PhotoUrl),
            WorkPhotos = photos,
            PublishedAt = publishedAt,
            UpdatedAt = now,
        };
    }

    private static ProfessionalListing ToListing(string id, ProfessionalDoc doc) => new()
    {
        Id = id,
        DisplayName = doc.DisplayName,
        CategoryIds = [doc.CategoryId],
        ServiceIds = [.. doc.ServiceIds],
        CityIds = [.. doc.CityIds],
        ExperienceId = doc.ExperienceId,
        RateTierId = doc.RateTierId,
        MobileNumber = doc.MobileNumber,
        PhotoUrl = doc.ProfilePhoto?.Url,
        WorkPhotoUrls = doc.WorkPhotos.Select(photo => photo.Url).ToList(),
    };

    /// <summary>
    /// A photo URL from the photo store, as the <c>{id, url}</c> the rules want. The id is the file name, the
    /// SHA-256 of the stored JPEG. A URL that isn't one of ours (a leftover on-device data URL) is left out.
    /// </summary>
    private PhotoDoc? ToPhoto(string? url)
    {
        if (url is null)
        {
            return null;
        }

        var match = PhotoUrl().Match(url);
        if (!match.Success)
        {
            logger.LogWarning("Firestore: a photo URL isn't a stored photo and was left out of the listing.");
            return null;
        }

        return new PhotoDoc { Id = match.Groups["id"].Value, Url = url };
    }

    [GeneratedRegex(@"^https://[^/]+/photos/(?<id>[0-9a-f]{64})\.jpg$")]
    private static partial Regex PhotoUrl();
}
