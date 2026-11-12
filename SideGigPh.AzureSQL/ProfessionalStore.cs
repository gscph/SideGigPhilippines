using Microsoft.EntityFrameworkCore;
using SideGigPh.Contracts;

namespace SideGigPh.AzureSQL;

public enum PublishOutcome
{
    Published,

    /// <summary>Anti-fraud rule 1: another listing has this number.</summary>
    PhoneTaken,

    /// <summary>Anti-fraud rule 2: another listing has one of these photos.</summary>
    PhotoInUse,

    /// <summary>A required field is missing or isn't a valid id.</summary>
    Invalid,
}

/// <summary>Published Skilled Worker listings (<c>Professionals</c> and its services, towns and work photos).</summary>
public sealed class ProfessionalStore(SideGigPhDb db, PhotoLinks photoLinks)
{
    /// <summary>Most work photos a listing can have (the app's <c>ProWorkspace.MaxWorkPhotos</c>).</summary>
    public const int MaxWorkPhotos = 12;

    /// <summary>One trade's listings, in one area when given. The phone ranks them (town, budget, stars).</summary>
    public async Task<IReadOnlyList<ListingDto>> SearchAsync(string categoryId, string? areaId, CancellationToken cancellationToken)
    {
        var rows = await Listings()
            .Where(p => p.CategoryId == categoryId && (areaId == null || p.AreaId == areaId))
            .ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    public async Task<ListingDto?> GetAsync(string id, CancellationToken cancellationToken) =>
        await Listings().FirstOrDefaultAsync(p => p.Id == id, cancellationToken) is { } row ? ToDto(row) : null;

    /// <summary>
    /// Makes <paramref name="uid"/>'s listing live, or replaces it. The number is the account's verified number
    /// (from the sign-in token), so one phone gets one listing (anti-fraud rule 1); a photo can be on one listing only (rule 2).
    /// </summary>
    public async Task<PublishOutcome> PublishAsync(string uid, string mobileNumber, PublishListingRequest request, CancellationToken cancellationToken)
    {
        var services = request.ServiceIds.Distinct().ToList();
        var cities = request.CityIds.Distinct().ToList();
        var workPhotos = request.WorkPhotoIds.Distinct().Take(MaxWorkPhotos).ToList();
        if (!StoreText.IsCode(request.CategoryId) || !StoreText.IsCode(request.AreaId)
            || services.Count == 0 || !services.All(StoreText.IsCode)
            || cities.Count == 0 || !cities.All(StoreText.IsCode)
            || (request.ExperienceId is not null && !StoreText.IsCode(request.ExperienceId))
            || (request.RateTierId is not null && !StoreText.IsCode(request.RateTierId))
            || (request.ProfilePhotoId is not null && !StoreText.IsPhotoId(request.ProfilePhotoId))
            || !workPhotos.All(StoreText.IsPhotoId))
        {
            return PublishOutcome.Invalid;
        }

        if (await db.Professionals.AnyAsync(p => p.MobileNumber == mobileNumber && p.Id != uid, cancellationToken))
        {
            return PublishOutcome.PhoneTaken;
        }

        try
        {
            return await db.InTransactionAsync(async ct =>
            {
                var now = DateTimeOffset.UtcNow;
                var row = await db.Professionals.FirstOrDefaultAsync(p => p.Id == uid, ct);
                if (row is null)
                {
                    row = new ProfessionalRow
                    {
                        Id = uid,
                        DisplayName = string.Empty,
                        MobileNumber = mobileNumber,
                        CategoryId = request.CategoryId,
                        AreaId = request.AreaId,
                        PublishedAt = now,
                    };
                    db.Professionals.Add(row);
                }
                else
                {
                    // Replace the lists in one go; the new rows are added below.
                    await db.ProfessionalServices.Where(s => s.ProfessionalId == uid).ExecuteDeleteAsync(ct);
                    await db.ProfessionalCities.Where(c => c.ProfessionalId == uid).ExecuteDeleteAsync(ct);
                    await db.ProfessionalWorkPhotos.Where(w => w.ProfessionalId == uid).ExecuteDeleteAsync(ct);
                }

                row.DisplayName = StoreText.Name(request.DisplayName, 60);
                row.MobileNumber = mobileNumber;
                row.CategoryId = request.CategoryId;
                row.AreaId = request.AreaId;
                row.ExperienceId = request.ExperienceId;
                row.RateTierId = request.RateTierId;
                row.ProfilePhotoId = request.ProfilePhotoId;
                row.UpdatedAt = now;

                db.ProfessionalServices.AddRange(services.Select(id => new ProfessionalServiceRow { ProfessionalId = uid, ServiceId = id }));
                db.ProfessionalCities.AddRange(cities.Select(id => new ProfessionalCityRow { ProfessionalId = uid, CityId = id }));
                db.ProfessionalWorkPhotos.AddRange(workPhotos.Select((id, i) =>
                    new WorkPhotoRow { PhotoId = id, ProfessionalId = uid, Position = (short)i }));

                await db.SaveChangesAsync(ct);
                return PublishOutcome.Published;
            }, cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlErrors.Violates(ex, Constraints.ProfessionalMobileNumber))
        {
            return PublishOutcome.PhoneTaken;
        }
        catch (DbUpdateException ex) when (SqlErrors.Violates(ex, Constraints.WorkPhotoKey) || SqlErrors.Violates(ex, Constraints.ProfessionalProfilePhoto))
        {
            return PublishOutcome.PhotoInUse;
        }
    }

    /// <summary>Takes the listing out of search (its services, towns and photos go with it). Its reviews stay.</summary>
    public Task RemoveAsync(string uid, CancellationToken cancellationToken) =>
        db.Professionals.Where(p => p.Id == uid).ExecuteDeleteAsync(cancellationToken);

    private IQueryable<ProfessionalRow> Listings() =>
        db.Professionals.AsNoTracking()
            .Include(p => p.Services)
            .Include(p => p.Cities)
            .Include(p => p.WorkPhotos)
            .AsSplitQuery();

    private ListingDto ToDto(ProfessionalRow p) => new(
        p.Id,
        p.DisplayName,
        p.MobileNumber,
        p.CategoryId,
        p.AreaId,
        p.Services.Select(s => s.ServiceId).ToList(),
        p.Cities.Select(c => c.CityId).ToList(),
        p.ExperienceId,
        p.RateTierId,
        p.ProfilePhotoId is { } photoId ? new PhotoDto(photoId, photoLinks.For(photoId)) : null,
        p.WorkPhotos.OrderBy(w => w.Position).Select(w => new PhotoDto(w.PhotoId, photoLinks.For(w.PhotoId))).ToList());
}
