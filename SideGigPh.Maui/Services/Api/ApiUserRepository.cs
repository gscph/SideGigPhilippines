using SideGigPh.Contracts;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Api;

/// <summary><see cref="IUserRepository"/> on SideGigPh.Api (<c>users/me</c>). The server only lets a person reach their own profile.</summary>
public sealed class ApiUserRepository(ApiClient api) : IUserRepository
{
    public Task<UserProfile?> GetAsync(string uid) => api.ReadAsync(async () =>
        (await api.GetAsync("users/me", ApiJson.Default.UserDto))?.ToModel(),
        null, $"the profile of {uid}");

    public async Task SaveAsync(UserProfile profile)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Put, "users/me", profile.ToDto(), ApiJson.Default.UserDto);
        await api.EnsureWrittenAsync(response, "save the profile");
    }

    public async Task DeleteAsync(string uid)
    {
        using var response = await api.SendAsync(HttpMethod.Delete, "users/me");
        await api.EnsureWrittenAsync(response, "delete the profile");
    }
}
