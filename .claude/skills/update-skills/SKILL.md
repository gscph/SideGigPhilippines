---
name: update-skills
description: Review the current session and create or refine this repo's `.claude/skills` to capture durable, reusable lessons. INVOKE ONLY WHEN THE USER EXPLICITLY ASKS — e.g. "/update-skills", "update the skills", "save that as a skill", "capture what we learned", "turn this into a skill". Never trigger this on your own: it is a deliberate end-of-session action, not an automatic reaction to doing work.
---

# Update project skills from this session

This is a **manual, user-invoked** skill. Do not auto-run it. Capturing skills is only worth doing when the user decides a session produced something durable — most turns do not, and running this unprompted is noise.

## What earns a skill (and what doesn't)

Create or update a skill only for a **non-obvious, repeatable** lesson:
- a gotcha with a concrete fix, an error→cause→fix mapping, a multi-step procedure, or a house convention a future reader would otherwise get wrong.

Do **not** make a skill for:
- a one-off fact with no reuse, anything already in `CLAUDE.md`, obvious code, or ephemeral task/session state. If it won't help a *future* session, skip it.

## Procedure

1. **Scan the session** for durable lessons: what was surprising, what took several iterations to get right, what a newcomer would redo incorrectly, and any rule the user stated ("always/never do X here").
2. **For each lesson, decide the action — prefer editing over creating:**
   - An existing skill already covers the area → **edit** it (add the new case to its error table / checklist; prune anything now stale).
   - Nothing covers it → **create** a new skill.
   - Already captured well → **no-op**.
3. **Match the house style.** Open an existing `SKILL.md` (e.g. `mudblazor-ui`, `search-relevance`) and mirror it: frontmatter `name` + a trigger-focused `description`, a concise body, error→cause→fix tables where useful, a "Before you finish" checklist, and a reminder to keep `CLAUDE.md` in sync.
4. **Scope the `description` so the skill triggers only when relevant** — name the specific files, error codes, and verbs that should pull it in. The description is the *only* part always loaded into context, so it must be specific. Vague descriptions over-trigger; narrow ones stay dormant until needed.
5. **Check for trigger overlap.** If a new skill's description could fire for the same cue as an existing one, make each description disambiguate (say what each is *for*, and what it's *not* for). A skill meant to be run by hand must say "invoke only when the user asks" (like this one), so it never fires passively.
6. **One skill per topic, no duplication.** Don't restate `CLAUDE.md` inside a skill — link to it. Keep a skill as instructions, not a changelog.
7. **Sync `CLAUDE.md`** if the session changed project structure, architecture, theme, or conventions — the repo standard requires it. Its "Project Skills" section must list **every** folder in `.claude/skills/` (and nothing else): add a line for a new skill, delete the line for a removed or merged one.
   - Repeated manual checks from the session (greps, one-off test apps) are worth turning into a script in the matching skill's `scripts/` folder; test it before saving, and refuse to write inside the repo if it generates files.
8. **Report** what you did: created / edited / skipped, one line each, so the user can veto.

## Before you finish

- Re-read each `description` you wrote or changed: is it specific enough to stay dormant until genuinely relevant? Would it wrongly fire on unrelated work?
- Confirm you edited an existing skill rather than creating a near-duplicate.
- List the changes for the user; don't commit unless asked.
