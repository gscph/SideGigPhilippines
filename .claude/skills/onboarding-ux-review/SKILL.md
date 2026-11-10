---
name: onboarding-ux-review
description: Review or design a SideGigPh screen, flow, prompt or permission ask against the onboarding research (UserOnboard Bulletproof Onboarding, Growth.Design case studies, Appcues GoodUX, Page Flows) and the app's trust guardrails. Use when the user asks "is this good UX", wants to hook/retain users, add a permission or sign-up prompt, add notifications, design a new first-run or empty state, or asks where auth/monetization should go.
---

# Onboarding UX review

The source of truth is `docs/onboarding.md`. §3 maps each research source to the decisions made here. Use this skill to judge a proposal quickly, then cite the principle by name.

## Product context to hold onto

- **Two sides.** Seekers are households needing a job done; Professionals are side-gig pros looking for customers. The role fork comes second, after the language.
- **Audience:** low English literacy. English / Taglish, icons, numbers.
- **We are an unknown brand. Value comes before friction.** Uber, Grab and Headspace ask for an account in the first seconds, and that pattern is deliberately *not* copied.
- **Money is the hook on both sides.** Seeker budget and pro rate share one price scale ("Pasok sa budget mo"). The pro's private monthly goal turns into "about N jobs a week".
- **Auth goes after onboarding, before the first benefit.** The owner is designing that workflow, so don't propose moving it into or before the interview.

## The checklist (name the principle when you flag something)

**Bulletproof Onboarding (UserOnboard)**
- **Integrated:** is it part of the product, or "an interface on top of your interface" (tooltips, coach marks, carousels, intro videos)? Prefer the real product.
- **Empowering:** does it move the user toward a real outcome (aircon fixed at a fair price, a first customer), or just explain the UI?
- **Steadfast:** does onboarding continue past minute one? Look for empty states with a reason and a CTA, progression (checklist, then "Level 2"), and behavior-triggered messages rather than "we miss you!!" blasts.

**Growth.Design principles**
- **Spark Effect:** the first questions are trivially easy, one tap each.
- **Reciprocity / Personalization Clarity:** every answer visibly comes back (chips, card, SMS, payoff). Never ask the same thing twice.
- **Respect > Data:** explain why before asking. Ask for sensitive things (money, phone number, location) after momentum has built.
- **Labor Illusion:** use waits to show work done *for their answers*, not a bare spinner.
- **Tell → Show, Ask → Offer:** show the thing (the pro's card inside a search list), and attach a benefit to every ask ("Notify me when an aircon tech joins in Taytay").
- **Endowment / IKEA effect:** let users build something that's theirs before a big ask.
- **Investment right after a reward:** the best moment for a big ask is right after a payoff, e.g. after the preview or results peak.
- **Zeigarnik:** unfinished things pull people back ("1 step to go live"), and the app resumes the exact step.
- **Hick's Law:** at most about 6 options per question. Past about 10 categories, show popular ones first plus "More" or search.
- **Exit points / no shaming:** always offer "Di ko pa alam", "Depende sa trabaho", back, or skip.

**Appcues GoodUX and Page Flows patterns**
- **DoorDash:** value before the "checkout" friction.
- **Duolingo:** language first, a real lesson, then "Account unlocked".
- **Fiverr:** account type first in a two-sided app.
- **Airbnb:** verification right before publishing.
- **Thinkific:** jump-in/jump-out checklist with only the critical jobs.
- **Stellar:** the most important questions first ("people just want us to get started").

## Trust guardrails (these block a proposal)

- **No fake numbers:** no fake counters, scarcity, social proof or demand signals. Any number shown is the user's own or real data. Before data exists, show none.
- **No fake competitors:** use skeletons, not invented listings, in previews. Sample pros only come from the dev stub, and their numbers use the unassigned +63 900 prefix.
- **Ask for permissions in context only,** behind a benefit pre-prompt. "Not now" never blocks the flow. Never during the interview without a reason.
- **Anti-fraud rules are non-negotiable:** 1 Phone = 1 GCash = 1 Registration, and duplicate-image rejection. They are enforced server-side, never trusting the local draft.
- **Say plainly what isn't built** rather than implying it works.

## Output format

For each issue give:
1. What the user sees.
2. The principle it breaks, by name.
3. A concrete fix in this codebase: component, Copy key, or ReferenceData entry.

Then list what already works well and why. End with any decisions that belong to the owner: auth placement, monetization, whether seekers see phone numbers before auth.
