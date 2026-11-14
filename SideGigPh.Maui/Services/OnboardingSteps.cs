using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>The Skilled Worker interview's questions, in order; the number is the step in its URL (<c>/start/pro/{n}</c>).</summary>
public enum ProStep
{
    Skills = 1,
    Services,
    Towns,
    Experience,
    Rate,
    Preview,
}

/// <summary>
/// The Skilled Worker's setup after the interview, in order (<c>/start/pro/setup/{n}</c>): the proof a listing needs
/// before Publish (<see cref="ProfessionalDraft.HasProof"/>). Publish, with its verified-number check, comes after.
/// </summary>
public enum ProSetupStep
{
    Photo = 1,
    WorkPhotos,
    References,
}

/// <summary>The Seeker interview's questions, in order; the number is the step in its URL (<c>/start/seeker/{n}</c>).</summary>
public enum SeekerStep
{
    Category = 1,
    Service,
    City,
    When,
    Budget,
    Finding,
}

/// <summary>
/// The onboarding rules, kept out of the pages so they can be tested: which question to show, what an answer
/// changes, where Back goes, and where the app opens at launch. The interview pages (<c>ProfessionalInterview</c>,
/// <c>SeekerInterview</c>) and <c>LaunchRouter</c> call these and only add saving and navigation.
/// </summary>
public static class OnboardingSteps
{
    public const string LanguagePath = "/start";
    public const string RolePath = "/start/role";

    public static string PathOf(ProStep step) => $"/start/pro/{(int)step}";

    public static string PathOf(SeekerStep step) => $"/start/seeker/{(int)step}";

    /// <summary>Opens the first unanswered Skilled Worker question: a step past it is never shown, so the last step lands there.</summary>
    public static string ProFirstUnansweredPath => PathOf(ProStep.Preview);

    /// <summary>The Seeker's home-town question (reopened when a saved town is no longer served).</summary>
    public static string SeekerTownPath => PathOf(SeekerStep.City);

    // --- Skilled Worker ------------------------------------------------------

    public static ProStep FirstUnanswered(this ProfessionalDraft draft)
    {
        if (draft.CategoryId is null) return ProStep.Skills;
        if (draft.ServiceIds.Count == 0) return ProStep.Services;
        if (draft.CityIds.Count == 0) return ProStep.Towns;
        if (draft.ExperienceId is null) return ProStep.Experience;
        if (draft.RateTierId is null) return ProStep.Rate;
        return ProStep.Preview;
    }

    /// <summary>The step a URL asks for, clamped to the real steps and never past the first unanswered one (e.g. a stale resume path).</summary>
    public static ProStep StepToShow(this ProfessionalDraft draft, int? requested)
    {
        var step = (ProStep)Math.Clamp(requested ?? 1, 1, (int)ProStep.Preview);
        var firstUnanswered = draft.FirstUnanswered();
        return step > firstUnanswered ? firstUnanswered : step;
    }

    /// <summary>Picks the one trade. A different trade clears the jobs, which belonged to the previous trade.</summary>
    public static void PickTrade(this ProfessionalDraft draft, string categoryId)
    {
        if (draft.CategoryId != categoryId)
        {
            draft.CategoryId = categoryId;
            draft.ServiceIds.Clear();
            draft.OtherJobText = null;
        }
    }

    /// <summary>Ticks or unticks a job. Unticking "Others" forgets what they typed for it.</summary>
    public static void ToggleService(this ProfessionalDraft draft, string serviceId)
    {
        Toggle(draft.ServiceIds, serviceId);
        if (!draft.ServiceIds.Any(ReferenceData.IsOtherJob))
        {
            draft.OtherJobText = null;
        }
    }

    public static void ToggleTown(this ProfessionalDraft draft, string cityId)
    {
        Toggle(draft.CityIds, cityId);
        draft.SetAreaFromTowns();
    }

    /// <summary>"Select all" adds every town we serve; when all are already picked it removes them.</summary>
    public static void ToggleAllTowns(this ProfessionalDraft draft)
    {
        var towns = ReferenceData.Cities.Select(town => town.Id).ToList();
        if (towns.All(draft.CityIds.Contains))
        {
            draft.CityIds.RemoveAll(towns.Contains);
        }
        else
        {
            draft.CityIds.AddRange(towns.Except(draft.CityIds));
        }

        draft.SetAreaFromTowns();
    }

    /// <summary>Back from a Skilled Worker question: the previous one, else Pro home (finished) or the role fork.</summary>
    public static string BackPath(ProStep step, bool finished) =>
        step != ProStep.Skills ? PathOf(step - 1)
        : finished ? ProRoutes.Home
        : RolePath;

    // --- Skilled Worker setup (proof, then Publish) ---------------------------

    public static string PathOf(ProSetupStep step) => $"/start/pro/setup/{(int)step}";

    /// <summary>The first proof still missing (own photo, work photos, a reference), or null when all are there.</summary>
    public static ProSetupStep? FirstUnfinished(this ProfessionalDraft draft)
    {
        if (!draft.HasProfilePhoto) return ProSetupStep.Photo;
        if (!draft.HasEnoughWorkPhotos) return ProSetupStep.WorkPhotos;
        if (!draft.HasReference) return ProSetupStep.References;
        return null;
    }

    /// <summary>The setup step a URL asks for, clamped to the real steps and never past the first unfinished one.</summary>
    public static ProSetupStep SetupStepToShow(this ProfessionalDraft draft, int? requested)
    {
        var step = (ProSetupStep)Math.Clamp(requested ?? 1, 1, (int)ProSetupStep.References);
        return draft.FirstUnfinished() is { } unfinished && step > unfinished ? unfinished : step;
    }

    /// <summary>
    /// Where the interview's Done (and launch) leads a Skilled Worker: Pro home once live, else the first missing proof,
    /// else Publish (which asks a Google account to add a verified number first).
    /// </summary>
    public static string AfterInterviewPath(this ProfessionalDraft draft) =>
        draft.PublishedAt is not null ? ProRoutes.Home
        : draft.FirstUnfinished() is { } unfinished ? PathOf(unfinished)
        : ProRoutes.Publish;

    /// <summary>Next from a setup step: the next one, Publish after the last.</summary>
    public static string NextPath(ProSetupStep step) =>
        step == ProSetupStep.References ? ProRoutes.Publish : PathOf(step + 1);

    /// <summary>Back from a setup step: the previous one, the interview's preview from the first.</summary>
    public static string BackPath(ProSetupStep step) =>
        step == ProSetupStep.Photo ? PathOf(ProStep.Preview) : PathOf(step - 1);

    // There is one area, so it is never asked: it follows from the towns (the API needs it on the listing).
    private static void SetAreaFromTowns(this ProfessionalDraft draft) => draft.AreaId = ReferenceData.AreaOf(draft.CityIds.FirstOrDefault());

    // --- Seeker -------------------------------------------------------------

    public static SeekerStep FirstUnanswered(this SeekerAnswers answers)
    {
        if (answers.CategoryId is null) return SeekerStep.Category;
        if (answers.ServiceId is null) return SeekerStep.Service;
        if (answers.CityId is null) return SeekerStep.City;
        if (answers.UrgencyId is null) return SeekerStep.When;
        if (answers.BudgetId is null) return SeekerStep.Budget;
        return SeekerStep.Finding;
    }

    /// <summary>The step a URL asks for, clamped to the real steps and never past the first unanswered one.</summary>
    public static SeekerStep StepToShow(this SeekerAnswers answers, int? requested)
    {
        var step = (SeekerStep)Math.Clamp(requested ?? 1, 1, (int)SeekerStep.Finding);
        var firstUnanswered = answers.FirstUnanswered();
        return step > firstUnanswered ? firstUnanswered : step;
    }

    /// <summary>Picks what job. A different trade clears the job, which belonged to the previous trade.</summary>
    public static void PickCategory(this SeekerAnswers answers, string categoryId)
    {
        if (answers.CategoryId != categoryId)
        {
            answers.CategoryId = categoryId;
            answers.ServiceId = null;
            answers.OtherJobText = null;
        }
    }

    /// <summary>Picks the job. A job other than "Others" forgets what they typed for "Others".</summary>
    public static void PickService(this SeekerAnswers answers, string serviceId)
    {
        answers.ServiceId = serviceId;
        if (!ReferenceData.IsOtherJob(serviceId))
        {
            answers.OtherJobText = null;
        }
    }

    /// <summary>Picks the one home town; its area (never asked) follows.</summary>
    public static void PickTown(this SeekerAnswers answers, string cityId)
    {
        answers.CityId = cityId;
        answers.AreaId = ReferenceData.AreaOf(cityId);
    }

    /// <summary>
    /// Where a Seeker goes after answering <paramref name="step"/>: the next question during the interview; when
    /// changing an answer after it, the next still-missing question, or back to the results when nothing is missing.
    /// </summary>
    public static string NextPath(this SeekerAnswers answers, SeekerStep step, bool finished)
    {
        if (!finished)
        {
            return PathOf(step + 1);
        }

        var next = answers.FirstUnanswered();
        return next == SeekerStep.Finding ? SeekerRoutes.Search : PathOf(next);
    }

    /// <summary>Back from a Seeker question: the results when changing an answer, else the previous question or the role fork.</summary>
    public static string BackPath(SeekerStep step, bool finished) =>
        finished ? SeekerRoutes.Search
        : step == SeekerStep.Category ? RolePath
        : PathOf(step - 1);

    // --- Launch ---------------------------------------------------------------

    /// <summary>
    /// Where the app opens once the language is known and the person is signed in: the role fork, the interview step
    /// they left, or their home. A finished interview that lost its town or trade (coverage shrank, a trade was retired: <see cref="OnboardingCleanup"/>)
    /// or an answer reopens that question.
    /// </summary>
    public static string LaunchPath(OnboardingState state)
    {
        if (state.Role is not { } role)
        {
            return RolePath;
        }

        if (state.CompletedAt is null)
        {
            return state.ResumePath ?? (role == UserRole.Seeker ? "/start/seeker" : "/start/pro");
        }

        if (role == UserRole.Seeker)
        {
            // A lost answer (retired trade, unserved town) reopens its question; budget and urgency are never lost.
            var missing = state.Seeker.FirstUnanswered();
            return missing is SeekerStep.Category or SeekerStep.Service or SeekerStep.City ? PathOf(missing) : SeekerRoutes.Home;
        }

        // Not live yet: back into the setup (photos, references, Publish) where they left it.
        return state.Professional.IsListingComplete ? state.Professional.AfterInterviewPath() : ProFirstUnansweredPath;
    }

    private static void Toggle(List<string> ids, string id)
    {
        if (!ids.Remove(id))
        {
            ids.Add(id);
        }
    }
}
