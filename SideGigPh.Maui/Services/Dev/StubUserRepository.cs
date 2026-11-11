using Microsoft.Extensions.Logging;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Dev;

/// <summary>
/// Stand-in until Firestore is wired. Keeps the profiles in a file on this phone
/// (<c>dev-users.json</c>). No samples: a profile is only ever the real signed-in person.
/// </summary>
public sealed class StubUserRepository(ILogger<StubUserRepository> logger) : IUserRepository
{
    private readonly JsonFile<List<UserProfile>> _file = new("dev-users.json", DevJsonContext.Default.ListUserProfile, logger);
    private List<UserProfile>? _users;

    public async Task<UserProfile?> GetAsync(string uid) => (await AllAsync()).FirstOrDefault(u => u.Uid == uid);

    public async Task SaveAsync(UserProfile profile)
    {
        var users = await AllAsync();
        users.RemoveAll(u => u.Uid == profile.Uid);
        users.Add(profile);
        await _file.WriteAsync(users);
    }

    public async Task DeleteAsync(string uid)
    {
        var users = await AllAsync();
        if (users.RemoveAll(u => u.Uid == uid) > 0)
        {
            await _file.WriteAsync(users);
        }
    }

    private async Task<List<UserProfile>> AllAsync() => _users ??= await _file.ReadAsync() ?? [];
}
