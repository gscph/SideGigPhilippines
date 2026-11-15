namespace SideGigPh.Maui.Models;

/// <summary>Outcome of publishing a Professional's listing.</summary>
public enum PublishResult
{
    Published,

    /// <summary>Anti-fraud rule 1: the mobile number already has a listing.</summary>
    PhoneTaken,

    /// <summary>Anti-fraud rule 1: another account uses this mobile number as a Job Seeker.</summary>
    PhoneUsedBySeeker,

    /// <summary>Not sent: the listing is missing interview answers (<see cref="ProfessionalDraft.IsListingComplete"/>).</summary>
    Incomplete,

    /// <summary>
    /// Not sent: the setup checklist isn't done (own photo, 2 work photos, 1 reference;
    /// <see cref="ProfessionalDraft.HasProof"/>).
    /// </summary>
    MissingProof,

    /// <summary>
    /// A reference has the worker's own number: possible when the references were added before a Google
    /// account linked its phone. Caught before sending, or answered by the server as <c>invalid-references</c>.
    /// </summary>
    BadReference,

    /// <summary>Anti-fraud rule 2: one of the photos belongs to another account (the server's <c>photo-in-use</c>).</summary>
    PhotoInUse,

    /// <summary>Too many publishes in a short time (the server's <c>too-many-requests</c>).</summary>
    TooManyTries,
}
