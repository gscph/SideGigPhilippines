namespace SideGigPh.Maui.Models;

/// <summary>Which side of the directory the user is on, chosen at the onboarding fork.</summary>
public enum UserRole
{
    /// <summary>A household looking for someone to do a job.</summary>
    Seeker,

    /// <summary>A side-gig provider looking for customers.</summary>
    Professional,
}
