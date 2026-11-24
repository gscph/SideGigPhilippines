namespace SideGigPh.Maui.Models;

/// <summary>Outcome of publishing a Professional's listing.</summary>
public enum PublishResult
{
    Published,

    /// <summary>Anti-fraud rule 1: the mobile number already has a listing.</summary>
    PhoneTaken,

    /// <summary>Not sent: the listing is missing interview answers (<see cref="ProfessionalDraft.IsListingComplete"/>).</summary>
    Incomplete,

    /// <summary>
    /// Not sent: the setup checklist isn't done (own photo;
    /// <see cref="ProfessionalDraft.HasProof"/>).
    /// </summary>
    MissingProof,

    /// <summary>Anti-fraud rule 2: one of the photos belongs to another account (the server's <c>photo-in-use</c>).</summary>
    PhotoInUse,

    /// <summary>
    /// The server refused the profile photo: a new one must be a selfie taken in the app (the server's
    /// <c>selfie-required</c>). The worker takes one on the interview's selfie step.
    /// </summary>
    SelfieNeeded,

    /// <summary>Too many publishes in a short time (the server's <c>too-many-requests</c>).</summary>
    TooManyTries,
}
