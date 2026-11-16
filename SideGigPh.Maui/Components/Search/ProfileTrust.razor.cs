using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Components.Shared;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// "What we checked": the badges a person at Tarbaho approved (ID, TESDA, past projects), the listing's number
/// verified by SMS (every listing's is, anti-fraud rule 1) and how many references vouch for the worker. Only the count shows; reference names and numbers stay private.
/// </summary>
public partial class ProfileTrust : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    /// <summary>Every badge a worker could reasonably hold fits; more would bury the rest of the section.</summary>
    private const int MaxBadges = 8;

    private string ReferencesTitle => Listing.ReferenceCount == 1
        ? T.Get(Copy.TrustOneReference)
        : T.Format(Copy.TrustReferences, Listing.ReferenceCount);
}
