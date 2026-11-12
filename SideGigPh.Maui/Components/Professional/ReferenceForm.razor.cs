using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>What was typed into <see cref="ReferenceForm"/>; the workspace checks and normalizes it.</summary>
public sealed record ReferenceInput(string? Name, string? Number);

/// <summary>
/// Name and mobile number fields with an Add button. Reports what was typed; the page decides whether it's valid
/// and passes back a <see cref="Problem"/> to show. Give it a new <c>@key</c> to clear it after a successful add.
/// </summary>
public partial class ReferenceForm : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;
    [Inject] private IPhoneNumberFormat PhoneFormat { get; set; } = default!;

    [Parameter] public bool Busy { get; set; }

    /// <summary>Why the last add didn't go through, already in the user's language, or null.</summary>
    [Parameter] public string? Problem { get; set; }

    [Parameter] public EventCallback<ReferenceInput> OnAdd { get; set; }

    private string? _name;
    private string? _number;

    private bool CanAdd => !string.IsNullOrWhiteSpace(_name) && !string.IsNullOrWhiteSpace(_number);

    private Task AddAsync() => OnAdd.InvokeAsync(new ReferenceInput(_name, _number));
}
