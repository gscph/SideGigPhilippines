using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Tests.Unit.JobPosts;

/// <summary>
/// A post's "When": an optional day on a calendar, in the Philippines' dates, from today to three months ahead. A wrong
/// rule here lets a household post for a day that has passed, or blocks an edit of a post whose day went by.
/// </summary>
public class PostDayTests
{
    private static readonly DateOnly Today = new(2026, 11, 19);

    private static AppText English()
    {
        var text = new AppText();
        text.Use(AppLanguage.English);
        return text;
    }

    private static JobPostDraft Filled(DateOnly? day = null) => new()
    {
        CategoryId = "aircon", ServiceId = "aircon-cleaning", CityId = "angono", Title = "Hindi lumalamig", Description = "Hindi lumalamig ang aircon",
        BudgetPesos = 1500, NeededOn = day,
    };

    [Fact]
    public void TodayIsThePhilippinesDate_WhateverThePhonesZone()
    {
        // 23:30 UTC on the 18th is 07:30 on the 19th in Manila.
        JobPostRules.PhilippineToday(new DateTimeOffset(2026, 11, 18, 23, 30, 0, TimeSpan.Zero)).Should().Be(Today);
        JobPostRules.PhilippineToday(new DateTimeOffset(2026, 11, 19, 15, 59, 0, TimeSpan.Zero)).Should().Be(Today);
        JobPostRules.PhilippineToday(new DateTimeOffset(2026, 11, 19, 16, 0, 0, TimeSpan.Zero)).Should().Be(Today.AddDays(1));
    }

    [Fact]
    public void ADay_IsOptional_AndRunsFromToday_ToThreeMonthsAhead()
    {
        JobPostRules.IsNeededOn(null, Today).Should().BeTrue("any day is fine");
        JobPostRules.IsNeededOn(Today, Today).Should().BeTrue();
        JobPostRules.IsNeededOn(PostDay.LastDay(Today), Today).Should().BeTrue();
        JobPostRules.IsNeededOn(Today.AddDays(-1), Today).Should().BeFalse("that day has passed");
        JobPostRules.IsNeededOn(PostDay.LastDay(Today).AddDays(1), Today).Should().BeFalse();
    }

    [Fact]
    public void APassedDay_StopsPosting_UnlessThePostAlreadyHadIt()
    {
        JobBoard.CanPost(Filled(), Today).Should().BeTrue();
        JobBoard.CanPost(Filled(Today.AddDays(3)), Today).Should().BeTrue();

        var stale = Filled(Today.AddDays(-2));
        JobBoard.FirstMissing(stale, Today).Should().Be(JobPostField.When);
        JobBoard.MissingText(JobPostField.When, English()).Should().Be("That day has passed. Pick another day.");
        English().PostRowMissing(stale, PostPart.When, Today).Should().Be("That day has passed. Pick another day.", "the When row says so");
        English().PostStillNeeded(stale, Today).Should().Be("Still needed: another day");
        PostReview.PartOf(JobPostField.When).Should().Be(PostPart.When, "a tap on Post opens the When sheet");

        stale.SavedNeededOn = stale.NeededOn; // Edit post: the day it already had may stay
        JobBoard.CanPost(stale, Today).Should().BeTrue();
    }

    [Fact]
    public void TheWhenRow_SaysTheDay_OrAnyDay_AndCancelPutsItBack()
    {
        var text = English();
        var draft = Filled();
        text.PostWhen(draft, Today).Should().Be("Any day");
        text.PostAction(draft, PostPart.When).Should().Be("Add");

        var before = PostReview.Take(draft);
        draft.NeededOn = Today.AddDays(1);
        text.PostWhen(draft, Today).Should().Be("Tomorrow");
        text.PostAction(draft, PostPart.When).Should().Be("Change");
        PostReview.Restore(draft, before);
        draft.NeededOn.Should().BeNull();

        draft.NeededOn = new DateOnly(2026, 12, 5);
        text.PostWhen(draft, Today).Should().Be("Sat, Dec 5");
        text.Needed(draft.NeededOn, Today).Should().Be("Needed Sat, Dec 5");
        text.Needed(null, Today).Should().BeNull();
    }

    [Fact]
    public void EditPost_StartsFromThePostsDay()
    {
        var post = new JobPost(5, "aircon", "aircon-cleaning", "Hindi lumalamig ang aircon", 1500, "angono", [], IsOpen: true, Mine: true,
            OffersService: false, DistanceKm: null, DateTimeOffset.Now, NeededOn: Today.AddDays(2));

        var draft = JobBoard.EditDraft(post);

        draft.NeededOn.Should().Be(Today.AddDays(2));
        draft.SavedNeededOn.Should().Be(Today.AddDays(2));
    }
}
