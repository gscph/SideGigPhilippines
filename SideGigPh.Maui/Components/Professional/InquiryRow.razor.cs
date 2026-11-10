using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Professional;

/// <summary>
/// One customer who tapped Call or Text: what they need, where, how soon and their budget,
/// all from their own interview answers.
/// </summary>
public partial class InquiryRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public Inquiry Inquiry { get; set; } = default!;

    [Parameter] public bool IsNew { get; set; }

    private string What => T.Get(Inquiry.Channel == InquiryChannel.Call ? Copy.InquiryCalled : Copy.InquiryTexted);

    private string JobAndTown => string.Join(" · ", new[]
    {
        Label(ReferenceData.FindService(Inquiry.ServiceId)),
        Label(ReferenceData.Find(ReferenceData.Cities, Inquiry.CityId)),
    }.OfType<string>());

    private string Details => string.Join(" · ", new[]
    {
        Label(ReferenceData.Find(ReferenceData.Urgencies, Inquiry.UrgencyId)),
        Label(ReferenceData.Find(ReferenceData.BudgetOptions, Inquiry.BudgetId)) is { } budget
            ? T.Format(Copy.InquiryBudget, budget)
            : null,
    }.OfType<string>());

    private string? Label(CatalogOption? option) => option is null ? null : T.Get(option.Label);
}
