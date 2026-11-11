using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>The signed-in people's profiles, keyed by their account uid. Private to each person.</summary>
public interface IUserRepository
{
    /// <summary>The person's profile, or null before it's first saved.</summary>
    Task<UserProfile?> GetAsync(string uid);

    /// <summary>Adds the profile or replaces the saved one.</summary>
    Task SaveAsync(UserProfile profile);

    Task DeleteAsync(string uid);
}
