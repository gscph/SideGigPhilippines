using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Maui.Storage;

namespace MobileAppTheme.Services.Local;

/// <summary>
/// Photos as files in app-private storage (<see cref="FileSystem.AppDataDirectory"/>/photos),
/// named by the SHA-256 of their bytes. The WebView can't read those files, so photos are
/// handed to the UI as data URLs, cached in memory after the first read.
/// </summary>
public sealed class FilePhotoStore : IPhotoStore
{
    private readonly string _folder = Path.Combine(FileSystem.AppDataDirectory, "photos");
    private readonly ConcurrentDictionary<string, string> _urls = new();

    public async Task<string> SaveAsync(byte[] bytes)
    {
        var id = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var path = PathOf(id);
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(_folder);
            var tempPath = path + ".tmp";
            await File.WriteAllBytesAsync(tempPath, bytes);
            File.Move(tempPath, path, overwrite: true);
        }

        _urls[id] = ToDataUrl(bytes);
        return id;
    }

    public async Task<string?> GetUrlAsync(string photoId)
    {
        if (_urls.TryGetValue(photoId, out var url))
        {
            return url;
        }

        var path = PathOf(photoId);
        if (!File.Exists(path))
        {
            return null;
        }

        return _urls[photoId] = ToDataUrl(await File.ReadAllBytesAsync(path));
    }

    public Task DeleteAsync(string photoId)
    {
        _urls.TryRemove(photoId, out _);
        File.Delete(PathOf(photoId));
        return Task.CompletedTask;
    }

    // Ids are hex hashes, so they are always safe file names.
    private string PathOf(string photoId) => Path.Combine(_folder, photoId);

    private static string ToDataUrl(byte[] bytes) => $"data:{MimeType(bytes)};base64,{Convert.ToBase64String(bytes)}";

    private static string MimeType(byte[] bytes) => bytes switch
    {
        [0x89, 0x50, 0x4E, 0x47, ..] => "image/png",
        [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => "image/webp",
        _ => "image/jpeg",
    };
}
