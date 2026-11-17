---
name: add-interview-step
description: Add, remove, reorder or change a question in SideGigPh's onboarding interviews (Seeker /start/seeker/{step} or Professional /start/pro/{step}), including the language/role screens, the loader, results chips, and resume logic. Use when the user wants to ask users something new during onboarding, change a question, or alter the onboarding flow.
---

# Changing an onboarding interview

The onboarding is a no-login interview: language, then role fork, then a Seeker or Professional interview. It's specified in `docs/onboarding.md`; read §2 and §4 first.

## Where things live

| Piece | File |
|---|---|
| Saved answers (on device, no account) | `Models/OnboardingState.cs` (`SeekerAnswers`, `ProfessionalDraft`), persisted by `IOnboardingStore` (`Services/Local/FileOnboardingStore.cs`) |
| Seeker steps | `Components/Onboarding/SeekerInterview.razor(.cs)`, `enum SeekerStep` |
| Pro steps | `Components/Onboarding/ProfessionalInterview.razor(.cs)`, `enum ProStep` |
| Start routing / resume | `Components/Onboarding/LaunchRouter.razor.cs` |
| Where Seeker answers are used after the interview | `Services/SeekerWorkspace.cs` (`Request`, `DefaultMessage`, the recorded `Inquiry`), Search tab filters in `Components/Search/SearchPage` |
| Question text | `Services/Localization/Copy.cs` |
| Answer options | `Services/ReferenceData.cs` |
| Step shell / tiles | `InterviewStep`, `ChoiceTiles` (skeleton: `MobileAppTheme/Components/Shared/`; changing them affects every app, see `mobile-app-theme`) |

## Adding a question: every place that must change

1. **Model:** add a nullable property to `SeekerAnswers` or `ProfessionalDraft` (a `List<string>` for multi-select). The JSON serializer is source-generated (`OnboardingJsonContext`), so a new property needs no extra registration. Old saved files simply load it as null.
2. **Options:** add a `CatalogOption` list to `ReferenceData` with stable IDs, icons, and both languages.
3. **Copy:** add the `Title` and a `Why` line ("why we ask", framed as the user's benefit) to `Copy.cs`. See the `taglish-copy` skill.
4. **Step enum:** insert the new member into `SeekerStep` or `ProStep` in the right position. The numbers are positional, so **every later step's number shifts.**
5. **`FirstUnanswered()`:** add the check in the same order as the enum. This guard stops a stale URL or resume path from skipping a question.
6. **Markup:** add a `case` that wraps `<ChoiceTiles>` (or a `MudTextField`) in `<InterviewStep Title Why Progress OnBack>`.
   - **Single-choice questions advance on tap:** the pick handler sets the value and returns `AdvanceAsync()`. No Next button.
   - **Multi-choice and text questions** pass `OnNext` and `CanContinue`.
   - **A question with a payoff** shown under the tiles does *not* auto-advance, so the reward stays visible.
   - **A step with only one thing to do** (the setup's selfie) does it for them where it can and moves on by itself, with no Next.
7. **Clear dependent answers** when a parent answer changes. For example, changing the Seeker category clears `ServiceId`, and unticking a pro trade removes that trade's services.
8. **Seeker answers after the interview:** the Search tab edits answers in place (job chips, town, budget and when menus; Seeker home's recent searches), not by reopening interview steps. If the new answer should change results, add it to `SearchRequest` and `SeekerWorkspace.Request`, rank on it in `SearchRanker` (see the `search-relevance` skill), and give it a filter on `SearchPage` if users will want to change it.
9. **Use the answer somewhere visible.** Results ranking, the card, the SMS template, or a payoff. An answer that goes nowhere breaks the "Personalization Clarity / Reciprocity" principle; if nothing would use it, push back on asking it.
10. **Docs:** update the step table in `docs/onboarding.md` §2.1 / §2.2 and the route summary in `CLAUDE.md` ("Onboarding & Initial Navigation").

## Flow rules that must keep holding

- **One question per screen. Tiles before typing.** At most about 6 tiles.
- **One URL per step** (`/start/seeker/3`), so Android hardware back walks the steps. The on-screen back goes to the previous step: step 1 goes to `/start/role`, and once completed it goes back to `/search` or `/professional`. `LaunchRouter` sends a returning user to `/seeker` (Seeker home) or `/professional`; the Seeker's loader lands on `/search` as the payoff.
- **Save on every forward move** (`Store.SaveAsync`), and set `state.ResumePath` while the interview isn't complete, so a killed app resumes at the exact step.
- **Editing after completion:** Seeker picks jump back to `/search` once all answers exist. Pro edits walk forward to the preview.
- **Progress** is `step / lastStep`, so step 1 already shows some progress (endowed progress). No "n of N" text.
- **No auth inside the interview.** Auth goes *after the interview, before the first benefit* (`FinishAsync` in both interviews). The owner is still designing that workflow, so don't wire it in unless they say so.
- **Markup is MudBlazor only** (see the `new-razor-component` skill).

## Verify

Don't run CLI builds (the user builds in Visual Studio). Walk through the flow on paper:
- Fresh install.
- Kill the app mid-interview and resume.
- Go back from each step.
- Change an early answer after completing.
- Tap each results chip.

Then ask the user to deploy from VS. To restart onboarding while testing, clear the app's storage in Android Settings.
