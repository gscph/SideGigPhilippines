using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Search;

/// <summary>
/// "Why you can trust them": the listing's number was verified by SMS (every listing's is, anti-fraud rule 1)
/// and how many references vouch for the worker. Only the count shows; reference names and numbers stay private.
/// </summary>
public partial class ProfileTrust : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public ProfessionalListing Listing { get; set; } = default!;

    private string ReferencesTitle => Listing.ReferenceCount == 1
        ? T.Get(Copy.TrustOneReference)
        : T.Format(Copy.TrustReferences, Listing.ReferenceCount);
}
