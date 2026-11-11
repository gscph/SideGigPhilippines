using Microsoft.AspNetCore.Components;
using MobileAppTheme.Localization;

namespace MobileAppTheme.Components.Pages;

/// <summary>
/// Shown for a route no page matches. The app's <c>Router</c> points <c>NotFoundPage</c> here and
/// lists this assembly in <c>AdditionalAssemblies</c>.
/// </summary>
public partial class NotFound : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
}
