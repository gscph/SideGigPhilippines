---
name: taglish-copy
description: Write or review user-facing text for SideGigPh in English + Taglish (LocalizedText pairs in Copy.cs or ReferenceData.cs). Use whenever a change adds or edits any string a user will see — titles, hints, buttons, errors, job/service labels, SMS templates — or when the user asks to fix wording, "local lingo", or translations. Includes copy-check.sh (missing, unused and placeholder-mismatched Copy keys).
---

# Bilingual copy (English / Taglish)

Many of our Professionals are **not confident English readers**. The user picks English or Taglish on the first screen, so every visible string exists in both languages.

## Where text lives (never hard-code it in a component)

| Kind of text | File | Shape |
|---|---|---|
| UI strings (titles, why-lines, buttons, errors, SMS templates) | `SideGigPh.Maui/Services/Localization/Copy.cs` | `public static readonly LocalizedText Name = new("English", "Taglish");` |
| Answer lists (categories, jobs, towns, prices, experience) | `SideGigPh.Maui/Services/ReferenceData.cs` | `CatalogOption(Id, Label, Icon, Hint, Amount)` |

Components render text with `@T.Get(Copy.X)` or `@T.Format(Copy.X, args)` (inject `AppText T`). Format placeholders are `{0}`, `{1}` and are formatted with `InvariantCulture`. Write a separate key for the singular when English needs one (see `ResultsTitleOne`).

**The one exception:** `LanguagePage` shows "Choose your language / Pumili ng wika" literally, because no language has been chosen yet.

## Taglish rules (local lingo, not translation)

- **Write everyday conversational Taglish**, the way a customer texts a tubero. Avoid formal or uncommon Tagalog. "Karanasan" is wrong; "Experience" is right.
- **Keep English technical terms as they are:** split-type, chemical wash, freon, breaker, inverter, PMS, CCTV, waterproofing, drywall, wiring.
- **Use the local household words people actually say:** CR (alongside banyo or inidoro), gripo, lababo, yero, alulod, kisame, semento, palitada, poso, poso negro, tangke, saksakan.
- **Use Pa-/Paki- only where it sounds natural** ("Pa-linis ng aircon", "Pa-freon", "Pabayad ng bills"). Don't force it onto every label ("Chemical wash ng aircon", "Wiring o rewiring").
- **Use "po" in messages sent to a pro** (the SMS templates), but not in UI labels.
- **Use "Nagca-canvass lang", "Di ko pa alam", "Depende sa trabaho"**: the short phrases people really use.

## Write for low literacy (both languages)

- Short, plain words. One idea per line.
- Every choice tile gets an **icon** (emoji in `CatalogOption.Icon`). Let numbers carry the meaning: ₱ amounts, years, town names.
- **Service labels stand on their own:** "Pa-linis ng aircon", not "Linis". The label must still make sense without its category heading, because it is dropped verbatim into the pre-filled SMS: `"Kailangan ko po: {0}, dito sa {1}, ngayon."`
- **"Why we ask" lines** state the user's benefit, not our need ("So you only see pros who go to your area.").
- **Use positive framing** ("Gusto kong kumita sa skills ko", not "Need a job?").

## Honesty guardrails (copy is product)

- **No invented numbers or claims.** No "12 people viewed this", no "pros respond in 5 minutes", no "free" unless the business says so, no "every week" promises.
- Any number shown is the user's own answer or real data.
- Say plainly when something isn't built ("Saved on this phone. Not visible to customers yet.").

## Checklist before finishing

1. No literal user-facing string in any `.razor` file: `bash .claude/skills/new-razor-component/scripts/ui-lint.sh` warns on text written straight into markup.
2. Run the checker. It fails on a key used but not defined, or a `{n}` placeholder that is in one language and not the other; it warns on keys nobody uses (delete them) and lists strings that are the same in both languages (fine for names like "Inbox", otherwise translate):
   ```bash
   bash .claude/skills/taglish-copy/scripts/copy-check.sh            # add --no-info to hide the same-in-both list
   ```
3. Both sides are filled in. Taglish is not just the English copied over, unless the term is genuinely the same in both (town names, "Hilot", "PasaBuy", ₱ ranges).
4. Read every service label inside the SMS template sentence. It should read naturally.
