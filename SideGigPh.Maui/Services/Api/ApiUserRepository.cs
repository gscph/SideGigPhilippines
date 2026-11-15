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

    public async Task SaveDeviceAsync(string token, string platform)
    {
        using var response = await api.SendJsonAsync(HttpMethod.Put, "users/me/devices", new SaveDeviceRequest(token, platform), ApiJson.Default.SaveDeviceRequest);
        await api.EnsureWrittenAsync(response, "register this phone for notifications");
    }

    public async Task<bool> DeleteAccountAsync()
    {
        using var response = await api.SendAsync(HttpMethod.Delete, "users/me/account");
        await api.EnsureWrittenAsync(response, "delete the account");
        return response.StatusCode != System.Net.HttpStatusCode.Accepted;
    }

    public async Task DeleteAsync(string uid)
    {
        using var response = await api.SendAsync(HttpMethod.Delete, "users/me");
        await api.EnsureWrittenAsync(response, "delete the profile");
    }
}
