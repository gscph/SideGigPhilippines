using Microsoft.AspNetCore.Components;

namespace SideGigPh.Admin.Components.Shared;

/// <summary>A green check or a gray dash, for yes/no columns.</summary>
public partial class YesNoIcon
{
    [Parameter] public bool Value { get; set; }
}
