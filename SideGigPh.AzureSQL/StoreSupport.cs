using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace SideGigPh.AzureSQL;

/// <summary>Where photos are readable: the public blob container, e.g. <c>https://….blob.core.windows.net/photos/</c>.</summary>
public sealed record PhotoLinks(string BaseUrl)
{
    public string For(string photoId) => $"{BaseUrl}{photoId}.jpg";
}

public static class AzureSqlServiceCollectionExtensions
{
    /// <summary>The database, the stores the API's functions use, and the search cache they share.</summary>
    public static IServiceCollection AddSideGigPhSql(this IServiceCollection services, string connectionString, PhotoLinks photoLinks)
    {
        services.AddDbContext<SideGigPhDb>(options => options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        services.AddSingleton(photoLinks);
        services.AddMemoryCache();
        services.AddSingleton<ListingCache>();
        services.AddScoped<UserStore>();
        services.AddScoped<ProfessionalStore>();
        services.AddScoped<ReviewStore>();
        services.AddScoped<ChatStore>();
        services.AddScoped<PushDeviceStore>();
        services.AddScoped<ConfigStore>();
        services.AddScoped<OtherJobNoteStore>();
        services.AddScoped<ErrorReportStore>();
        services.AddScoped<SafetyStore>();
        services.AddScoped<FeedbackStore>();
        services.AddScoped<WarmupStore>();
        services.AddScoped<VerificationStore>();
        services.AddScoped<JobPostStore>();
        services.AddScoped<PickedJobStore>();
        return services;
    }
}

internal static partial class StoreText
{
    /// <summary>Trimmed text cut to the column's length, or null when empty.</summary>
    public static string? Limit(string? value, int maxLength)
    {
        var text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text.Length > maxLength ? text[..maxLength] : text;
    }

    /// <summary>A name for a NOT NULL column: never empty.</summary>
    public static string Name(string? value, int maxLength) => Limit(value, maxLength) ?? "—";

    public static bool IsPhotoId(string? value) => value is not null && PhotoId().IsMatch(value);

    /// <summary>A ReferenceData-style id: lower case letters, digits, '-' and '_'.</summary>
    public static bool IsCode(string? value) => value is not null && Code().IsMatch(value);

    /// <summary>A Philippine mobile number in E.164, e.g. +639171234567 (the schema's mobile number checks).</summary>
    public static bool IsMobileNumber(string? value) => value is not null && MobileNumber().IsMatch(value);

    [GeneratedRegex(@"^\+639[0-9]{9}$")]
    private static partial Regex MobileNumber();

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex PhotoId();

    [GeneratedRegex("^[a-z0-9_-]{1,40}$")]
    private static partial Regex Code();
}

internal static class SqlErrors
{
    /// <summary>A save refused by the named unique key, primary key or unique index.</summary>
    public static bool Violates(DbUpdateException ex, string constraint) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 } sql && sql.Message.Contains(constraint, StringComparison.Ordinal);
}

internal static class PhotoUse
{
    /// <summary>
    /// Anti-fraud rule 2 across tables: true when another account already uses one of <paramref name="photoIds"/> as
    /// its profile photo (<c>Users</c>, either side), its listing photo or a work photo. Each table's unique index
    /// only covers itself, and photo ids are public (they're in the photo URLs), so a stolen id is caught here.
    /// </summary>
    public static async Task<bool> UsedByAnotherAsync(this SideGigPhDb db, string uid, IReadOnlyCollection<string> photoIds, CancellationToken cancellationToken) =>
        photoIds.Count > 0
        && (await db.Users.AnyAsync(u => u.Uid != uid && u.ProfilePhotoId != null && photoIds.Contains(u.ProfilePhotoId), cancellationToken)
            || await db.Professionals.AnyAsync(p => p.Id != uid && p.ProfilePhotoId != null && photoIds.Contains(p.ProfilePhotoId), cancellationToken)
            || await db.ProfessionalWorkPhotos.AnyAsync(w => w.ProfessionalId != uid && photoIds.Contains(w.PhotoId), cancellationToken));
}

internal static class Transactions
{
    /// <summary>Runs <paramref name="work"/> in one transaction, retried as a whole on a transient Azure SQL error.</summary>
    public static Task<T> InTransactionAsync<T>(this SideGigPhDb db, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async ct =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var result = await work(ct);
            await transaction.CommitAsync(ct);
            return result;
        }, cancellationToken);
}
