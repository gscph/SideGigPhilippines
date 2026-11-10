---
name: search-relevance
description: Tune or debug how SideGigPh's Seeker search understands typed words and ranks pros — Services/Search (SearchText, SearchIntentParser, TradeWords synonyms, SearchRanker weights, ProfessionalSearch). Use when a query finds the wrong trade/job or nothing ("tubero barado", "sira", "aircond"), when results are in the wrong order, when adding a ranking signal or a search filter, or after adding categories/jobs/labels to ReferenceData. Includes a script that runs the real search code against queries without the MAUI build. Not for the search page's layout (that's new-razor-component / mudblazor-ui).
---

# Search relevance (Seeker search)

Finding the right pro and contacting them is the product's core. Search must stay **explainable**: every weight is a named constant, and every change is checked against real queries before it ships. The architecture summary is in `CLAUDE.md` (Seeker side → Search relevance); this skill is the how-to.

## Pipeline

| Stage | File | What it decides |
|---|---|---|
| Normalise words | `Services/Search/SearchText.cs` | Lower case, accents off, punctuation split, filler words dropped (`FillerWords`: English + Tagalog like `ng`, `po`, `kailangan`). Word match: exact 1, typed prefix 0.8 ("airc"→aircon), longer form 0.7 ("leaking"→leak). |
| Text → trade + job | `Services/Search/SearchIntentParser.cs` | Scores each category against its labels (both languages), its hint, all its jobs' labels and `TradeWords` synonyms. `GenericWords` (sira, repair, install…) count half toward the trade. Weak (< `MinCategoryScore`) or tied → **no trade**, plus the closest trades, and the UI asks "which kind of pro?". A job is picked only when one job clearly out-scores the rest. `-other` jobs are never matched. |
| Fetch | `Services/Search/ProfessionalSearch.cs` | The trade's pros from **all** towns + their `ReviewSummary`s. |
| Rank | `Services/Search/SearchRanker.cs` | Only pros listing the exact job (else the whole trade, `ServiceFallback`). Groups: goes to the Seeker's town, then other towns. Score: budget fit, smoothed stars (prior 4.0 weighted as 3 reviews), experience, own photo, work photos. |
| UI | `Components/Search/SearchPage` | Shows the understood trade as the selected chip, alternatives next, job chips, town/budget menus; the "which kind of pro?" / "couldn't tell" alert. |

## Check a change with the script (do this every time)

It compiles the repo's real `Models/*.cs`, `ReferenceData.cs`, `Services/Search/*` and the sample pros into a throwaway console app. No MAUI build, so it's safe with Visual Studio open. Pass a work dir **outside the repo** (the session scratchpad); the script refuses one inside it.

```bash
bash .claude/skills/search-relevance/scripts/search-check.sh "<scratchpad>"                       # regression list
bash .claude/skills/search-relevance/scripts/search-check.sh "<scratchpad>" "pa ayos ng gripo" "ac"  # your own queries
bash .claude/skills/search-relevance/scripts/search-check.sh "<scratchpad>" --rank aircon aircon-cleaning taytay 500-1000
```

- Output per query: `query -> trade job also: alternatives`. `? ask` means the UI will ask which kind of pro (expected for `linis`, `repair`, `sira`).
- `--rank` shows the sample pros' order with score, budget fit and job match (reviews are left out, so every pro gets the 4.0 prior).
- Compare the regression list before and after your change. Every line that moved must be an improvement you can explain.
- Then run the `build-check` skill: the script doesn't compile the UI.

## Common fixes

| Symptom | Cause | Fix |
|---|---|---|
| A word people use finds nothing ("aircond", "hinang") | Not in any label, hint or synonym | Add it to that trade's `TradeWords` (lower case, no accents). Prefer this over renaming labels (labels are copy; see `taglish-copy`). |
| A generic word picks a random trade ("sira" → Welding) | It appears in one trade's label more exactly than in others | Add it to `GenericWords`. |
| Filler words skew matching ("pa", "ng", "need") | Missing from `FillerWords` | Add to `FillerWords`. Never add a word that names a job. |
| One word picks a too-specific job ("massage" → full body) | Only one job label contains the word | Usually fine. If not, make the job labels distinguishable rather than special-casing. |
| Right pros, wrong order | A weight in `SearchRanker` | Change the named constant, explain why in its comment, re-run `--rank`. Don't add unnamed numbers. |
| Results empty for a real job | Nobody lists that job | Expected: `ServiceFallback` shows the trade's pros with a note. Don't loosen the job filter. |

## Adding a ranking signal or filter

1. Put the data on `ProfessionalListing` (or a summary like `ReviewSummary` fetched in `ProfessionalSearch`). Real backends should denormalise it onto the listing; don't fetch per pro in the ranker.
2. Add it to `SearchRequest` and `SeekerWorkspace.Request` if the Seeker chooses it (save it in `SeekerAnswers` so it survives restarts), and a `FilterMenu` on `SearchPage`.
3. Score it in `SearchRanker` with a named constant, and add it to `SearchHit` if the UI should explain it (like `FitsBudget`).
4. Re-run the script and `build-check`; update the "Search relevance" bullets in `CLAUDE.md`.

## Guardrails

- **Honesty:** never boost or show invented activity. Sample reviews exist only in Debug (`DevSamples`).
- **Town grouping stays visible** ("Goes to Taytay" / "Other towns nearby"); never mix out-of-town pros into the local group silently.
- **Budget is a sort signal, not a hard filter.** Over-budget pros drop lower but stay findable.
- **Category/job ids are stable** (`CLAUDE.md`). New jobs are matched automatically from their labels; check them with the script.
