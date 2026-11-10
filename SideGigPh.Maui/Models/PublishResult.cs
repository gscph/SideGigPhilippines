namespace SideGigPh.Maui.Models;

/// <summary>Outcome of publishing a Professional's listing.</summary>
public enum PublishResult
{
    Published,

    /// <summary>Anti-fraud rule 1: the mobile number already has a listing.</summary>
    PhoneTaken,
}
