using SideGigPh.Maui.Components.JobPosts;
using SideGigPh.Maui.Components.Professional;
using SideGigPh.Maui.Components.Seeker;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The Skilled Worker interview's questions, in order; the number is the step in its URL (<c>/start/pro/{n}</c>).
/// The proof a listing needs (<see cref="Photo"/>, <see cref="ProfessionalDraft.HasProof"/>) and the verified number
/// (<see cref="Number"/>, skipped when it isn't asked: <see cref="OnboardingSteps.AsksNumber"/>) are questions too, so
/// the preview shows the real card and its Done goes straight to Publish.
/// </summary>
public enum ProStep
{
    Skills = 1,
    Services,
    Rate,
    Towns,
    Experience,
    Photo,
    Number,
    Preview,
}

/// <summary>The Seeker interview's questions, in order; the number is the step in its URL (<c>/start/seeker/{n}</c>).</summary>
public enum SeekerStep
{
    Category = 1,
    Service,
    City,
    When,
    Budget,
    FindBy,
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

    /// <summary>The first interview question for <paramref name="role"/>.</summary>
    public static string FirstPath(UserRole role) =>
        role == UserRole.Seeker ? PathOf(SeekerStep.Category) : PathOf(ProStep.Skills);

    /// <summary>
    /// Whether the Skilled Worker interview asks for a verified number (<see cref="ProStep.Number"/>): only with a sign-in
    /// provider (<paramref name="hasSignIn"/>), for an account without one (a Google or Apple sign-in), and only while
    /// this phone may still get an SMS code today (<paramref name="smsWaitLeft"/> from <c>SmsSendLimit.WaitLeft</c>, null
    /// when used up). The step offers "Do this later"; Publish asks again either way. A Seeker is never asked.
    /// </summary>
    public static bool AsksNumber(bool hasSignIn, bool hasVerifiedNumber, TimeSpan? smsWaitLeft) =>
        hasSignIn && !hasVerifiedNumber && smsWaitLeft is not null;

    // --- Skilled Worker ------------------------------------------------------

    public static ProStep FirstUnanswered(this ProfessionalDraft draft)
    {
        if (draft.CategoryId is null) return ProStep.Skills;
        if (draft.ServiceIds.Count == 0) return ProStep.Services;
        if (draft.RateTierId is null) return ProStep.Rate;
        if (draft.CityIds.Count == 0) return ProStep.Towns;
        if (draft.ExperienceId is null) return ProStep.Experience;
        if (!draft.HasProof) return ProStep.Photo;
        return ProStep.Preview;
    }

    /// <summary>
    /// The step a URL asks for, clamped to the real steps and never past the first unanswered one (e.g. a stale resume
    /// path). The jobs question of the "Others" trade is its trade question, where they typed the work; the number
    /// question, when it isn't asked (<see cref="AsksNumber"/>), is the preview.
    /// </summary>
    public static ProStep StepToShow(this ProfessionalDraft draft, int? requested, bool asksNumber = false)
    {
        var step = (ProStep)Math.Clamp(requested ?? 1, 1, (int)ProStep.Preview);
        var firstUnanswered = draft.FirstUnanswered();
        step = step > firstUnanswered ? firstUnanswered : step;
        return step switch
        {
            ProStep.Services when draft.IsOtherTrade() => ProStep.Skills,
            ProStep.Number when !asksNumber => ProStep.Preview,
            _ => step,
        };
    }

    /// <summary>The worker picked "Others" after the trades: a kind of work we don't list, typed in <see cref="ProfessionalDraft.OtherJobText"/>.</summary>
    public static bool IsOtherTrade(this ProfessionalDraft draft) => draft.CategoryId == ReferenceData.OtherTradeId;

    /// <summary>
    /// Picks the one trade. A different trade clears the jobs, which belonged to the previous trade. "Others" (a kind of
    /// work we don't list) is its own only job, so the jobs question is skipped and the interview goes on: nobody is
    /// stuck on the trade question.
    /// </summary>
    public static void PickTrade(this ProfessionalDraft draft, string categoryId)
    {
        if (draft.CategoryId != categoryId)
        {
            draft.CategoryId = categoryId;
            draft.ServiceIds.Clear();
            draft.OtherJobText = null;
        }

        if (categoryId == ReferenceData.OtherTradeId)
        {
            draft.ServiceIds.Clear();
            draft.ServiceIds.Add(ReferenceData.OtherTradeId);
        }
    }

    /// <summary>
    /// Next from a Skilled Worker question: the next one; the "Others" trade skips the jobs question, and the number
    /// question is skipped when it isn't asked (<see cref="AsksNumber"/>).
    /// </summary>
    public static string NextPath(this ProfessionalDraft draft, ProStep step, bool asksNumber = false) =>
        PathOf(step switch
        {
            ProStep.Skills when draft.IsOtherTrade() => ProStep.Rate,
            ProStep.Photo when !asksNumber => ProStep.Preview,
            _ => step + 1,
        });

    /// <summary>Ticks or unticks a job. Unticking "Others" forgets what they typed for it.</summary>
    public static void ToggleService(this ProfessionalDraft draft, string serviceId)
    {
        Toggle(draft.ServiceIds, serviceId);
        if (!draft.ServiceIds.Any(ReferenceData.IsOtherJob))
        {
            draft.OtherJobText = null;
        }
    }

    /// <summary>The jobs "Select all" ticks for a trade: every job it lists, not "Others" (which asks them to type the work).</summary>
    public static IReadOnlyList<string> ListedJobs(string? categoryId) =>
        ReferenceData.ServicesOf(categoryId).Select(job => job.Id).Where(id => !ReferenceData.IsOtherJob(id)).ToList();

    /// <summary>True when every listed job of their trade is ticked, so the button reads "Remove all".</summary>
    public static bool HasAllJobs(this ProfessionalDraft draft)
    {
        var jobs = ListedJobs(draft.CategoryId);
        return jobs.Count > 0 && jobs.All(draft.ServiceIds.Contains);
    }

    /// <summary>
    /// "Select all" ticks every listed job of their trade (keeping "Others" if ticked); when all are already ticked,
    /// "Remove all" unticks every job, "Others" and its text too, so they start the question again.
    /// </summary>
    public static void ToggleAllJobs(this ProfessionalDraft draft)
    {
        if (draft.HasAllJobs())
        {
            draft.ServiceIds.Clear();
            draft.OtherJobText = null;
            return;
        }

        draft.ServiceIds.AddRange(ListedJobs(draft.CategoryId).Except(draft.ServiceIds));
    }

    /// <summary>
    /// A Skilled Worker reaching the towns question with none ticked starts with the launch's default town ticked
    /// (server-set); they can untick it and tick others. Does nothing once they have towns.
    /// </summary>
    public static void PreselectDefaultTown(this ProfessionalDraft draft)
    {
        if (draft.CityIds.Count == 0 && ReferenceData.DefaultTownId is { } town)
        {
            draft.CityIds.Add(town);
            draft.SetAreaFromTowns();
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

    /// <summary>Back from a Skilled Worker question, skipping the jobs question of the "Others" trade and an unasked number question.</summary>
    public static string BackPath(this ProfessionalDraft draft, ProStep step, bool finished, bool asksNumber = false) =>
        step switch
        {
            ProStep.Rate when draft.IsOtherTrade() => PathOf(ProStep.Skills),
            ProStep.Preview when !asksNumber => PathOf(ProStep.Photo),
            _ => BackPath(step, finished),
        };

    /// <summary>Back from a Skilled Worker question: the previous one, else Pro home (finished) or the role fork.</summary>
    public static string BackPath(ProStep step, bool finished) =>
        step != ProStep.Skills ? PathOf(step - 1)
        : finished ? ProRoutes.Home
        : RolePath;

    // --- After the interview (Publish) ---------------------------------------

    /// <summary>How full the progress bar is on an interview question (step 1 already shows some: endowed progress).</summary>
    public static double ProgressOf(ProStep step) => (int)step / (double)ProStep.Preview;

    /// <summary>
    /// Where the interview's Done (and launch) leads a Skilled Worker: Pro home once live, else the selfie question while
    /// the photo is missing, else Publish (which asks for a verified number if the number question was put off).
    /// </summary>
    public static string AfterInterviewPath(this ProfessionalDraft draft) =>
        draft.PublishedAt is not null ? ProRoutes.Home
        : !draft.HasProof ? PathOf(ProStep.Photo)
        : ProRoutes.Publish;

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
        if (answers.FindBy is null) return SeekerStep.FindBy;
        return SeekerStep.Finding;
    }

    /// <summary>
    /// The step a URL asks for, clamped to the real steps and never past the first unanswered one. The job question of
    /// the "Others" trade is its trade question, where they typed the work.
    /// </summary>
    public static SeekerStep StepToShow(this SeekerAnswers answers, int? requested)
    {
        var step = (SeekerStep)Math.Clamp(requested ?? 1, 1, (int)SeekerStep.Finding);
        var firstUnanswered = answers.FirstUnanswered();
        step = step > firstUnanswered ? firstUnanswered : step;
        return step == SeekerStep.Service && answers.IsOtherTrade() ? SeekerStep.Category : step;
    }

    /// <summary>The household picked "Others" after the trades: work we don't list, typed in <see cref="SeekerAnswers.OtherJobText"/>.</summary>
    public static bool IsOtherTrade(this SeekerAnswers answers) => answers.CategoryId == ReferenceData.OtherTradeId;

    /// <summary>
    /// Picks what job. A different trade clears the job, which belonged to the previous trade. "Others" (work we don't
    /// list) is its own job, so the job question is skipped and the interview goes on to the town.
    /// </summary>
    public static void PickCategory(this SeekerAnswers answers, string categoryId)
    {
        if (answers.CategoryId != categoryId)
        {
            answers.CategoryId = categoryId;
            answers.ServiceId = null;
            answers.OtherJobText = null;
        }

        if (categoryId == ReferenceData.OtherTradeId)
        {
            answers.ServiceId = ReferenceData.OtherTradeId;
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
    /// <summary>Picks their home town. A barangay of another town is dropped (one of this town stays).</summary>
    public static void PickTown(this SeekerAnswers answers, string cityId)
    {
        answers.CityId = cityId;
        answers.AreaId = ReferenceData.AreaOf(cityId);
        answers.BarangayId = ReferenceData.FindBarangay(cityId, answers.BarangayId)?.Id;
    }

    /// <summary>
    /// The town the household's town question shows picked: their own, else the launch's default town (server-set),
    /// so most people only confirm it. Null when neither is there.
    /// </summary>
    public static string? TownShown(this SeekerAnswers answers) => answers.CityId ?? ReferenceData.DefaultTownId;

    /// <summary>Picks (or with null, "Not sure", clears) their barangay; only one we serve in <paramref name="cityId"/> is kept.</summary>
    public static void PickBarangay(this SeekerAnswers answers, string? cityId, string? barangayId) =>
        answers.BarangayId = ReferenceData.FindBarangay(cityId, barangayId)?.Id;

    /// <summary>Whether the household wants to post what they need rather than see workers first.</summary>
    public static bool WantsToPost(this SeekerAnswers answers) => answers.FindBy == ReferenceData.FindByPost;

    /// <summary>
    /// Where the interview's last step lands: the form to post what they need (pre-filled from these answers) for
    /// "Post what I need", else the results.
    /// </summary>
    public static string FinishPath(this SeekerAnswers answers) =>
        answers.WantsToPost() ? JobPostRoutes.New : SeekerRoutes.Search;

    /// <summary>
    /// Where a Seeker goes after answering <paramref name="step"/>: the next question during the interview; when
    /// changing an answer after it, the next still-missing question, or back to the results when nothing is missing.
    /// "Search or post" is never asked again when changing an answer (someone who finished before it existed has none):
    /// Home offers both.
    /// </summary>
    public static string NextPath(this SeekerAnswers answers, SeekerStep step, bool finished)
    {
        if (!finished)
        {
            return PathOf(step == SeekerStep.Category && answers.IsOtherTrade() ? SeekerStep.City : step + 1);
        }

        var next = answers.FirstUnanswered();
        return next is SeekerStep.FindBy or SeekerStep.Finding ? SeekerRoutes.Search : PathOf(next);
    }

    /// <summary>Back from a Seeker question, skipping the job question of the "Others" trade.</summary>
    public static string BackPath(this SeekerAnswers answers, SeekerStep step, bool finished) =>
        !finished && step == SeekerStep.City && answers.IsOtherTrade() ? PathOf(SeekerStep.Category) : BackPath(step, finished);

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
            // A lost answer (retired trade, unserved town) reopens its question; budget and urgency are never lost. No job
            // is not lost: a trade tapped on Home searches all its jobs (SeekerWorkspace.PickCategoryAsync).
            return state.Seeker.CategoryId is null ? PathOf(SeekerStep.Category)
                : state.Seeker.CityId is null ? SeekerTownPath
                : SeekerRoutes.Home;
        }

        // Not live yet: back to the selfie question or Publish, where they left it.
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
