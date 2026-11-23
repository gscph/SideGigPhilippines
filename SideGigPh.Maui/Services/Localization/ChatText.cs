using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>
/// The job a chat is about, from the Seeker's answers, as short lines for the Skilled Worker:
/// "Pa-linis ng aircon · Taytay" and "Ngayong araw · Budget: ₱800 – ₱1,500"; and the day dividers between messages.
/// </summary>
public static class ChatText
{
    /// <summary>The job (its name when the chat started, else today's) and the town.</summary>
    public static string WhatAndWhere(this AppText text, Chat chat) => Join(
        chat.ServiceName is { } saved ? text.Get(saved) : Label(text, ReferenceData.FindService(chat.ServiceId)),
        Label(text, ReferenceData.Find(ReferenceData.Cities, chat.CityId)));

    public static string WhenAndBudget(this AppText text, Chat chat) => Join(
        Label(text, ReferenceData.Find(ReferenceData.Urgencies, chat.UrgencyId)),
        Pricing.BudgetLabel(chat.BudgetId) is { } budget
            ? text.Format(Copy.ChatBudget, text.Get(budget))
            : null);

    /// <summary>The Seeker's name as the Skilled Worker sees it, or "Customer" if they gave none.</summary>
    public static string SeekerName(this AppText text, Chat chat) =>
        string.IsNullOrWhiteSpace(chat.SeekerName) ? text.Get(Copy.ChatCustomer) : chat.SeekerName;

    /// <summary>A day divider in an open chat: "Today", "Yesterday", "Tue, Nov 18", or "Nov 18, 2025" for another year.</summary>
    public static string ChatDayLabel(this AppText text, DateOnly day, DateOnly today) =>
        day == today ? text.Get(Copy.PostDayToday)
        : day == today.AddDays(-1) ? text.Get(CommonCopy.Yesterday)
        : day.Year == today.Year ? PostReviewText.ShortDate(day)
        : day.ToString("MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);

    private static string? Label(AppText text, CatalogOption? option) => option is null ? null : text.Get(option.Label);

    private static string Join(params string?[] parts) => string.Join(" · ", parts.OfType<string>());
}
