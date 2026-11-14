using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// The pro's references (<see cref="ReferencesEditor"/>): private, never shown to customers; at least one is needed to publish.
/// </summary>
public partial class ReferencesPage : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
}
