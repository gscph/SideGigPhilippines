# Search ranking

How `GET search/professionals` orders Skilled Workers for a Seeker (`SideGigPh.AzureSQL/SearchRanker.cs`), why it
works this way, and how paid boosts will fit in later.

## The shape: filter, score, then place

Big marketplaces (Airbnb, Thumbtack, Yelp, Google Local Services) split search into stages:

1. **Retrieval / hard filters.** Throw out anyone who can't do the job. Nothing a worker pays or scores can
   get them past this stage.
2. **Ranking.** Give every worker who's left one relevance score and sort by it. Big companies learn the score from
   logs (learning to rank: gradient-boosted trees, then neural nets with hundreds of features). Before there's
   enough traffic to learn from, they use a hand-weighted formula. That's what we have now.
3. **Re-ranking / placement.** Apply business rules to the sorted list: sponsored slots, variety, new-listing
   exposure. **Ads are placed here, labeled, in their own slots. They never feed into the organic score.**

## 1. Hard filters

| Rule | Behaviour |
|---|---|
| **Job** | Only workers who list the exact job (`ServiceId`). If nobody lists it, there are no results. There's no fallback to the whole trade (`SearchPageDto.ServiceFallback` is always `false` and stays in the contract only for older apps). |
| **Town** (forgiving) | Workers who go to the Seeker's town make up the first group, `InTown`. Workers from other towns we serve come after as "Other towns nearby". Workers who only go to towns we don't serve are left out. A great worker from a nearby town never jumps above a worker from the Seeker's own town. |

## 2. The score (0–100, plus up to 10 for badges, within each group)

Each part is a 0–1 feature multiplied by a weight. The weights add up to 100 and are named constants in `SearchRanker`.
`SearchRanker.Score` returns a `RankScore` with each part, so any ranking can be explained.

| Part | Points | Feature |
|---|---|---|
| **Reviews**: stars | 22 | Bayesian average: stars are pulled toward 4.0 as if every worker already had 3 reviews, so a single 5★ can't beat 40 reviews averaging 4.8★. Scaled from 1–5 to 0–1. |
| **Reviews**: how many | 8 | log(1 + n) / log(26): the 5th review counts for more than the 45th. Full at 25 reviews. |
| **Responsiveness**: reply rate | 12 | Share of chats answered in the chat, out of chats Seekers started in the last 90 days, smoothed (a new worker starts at 70% as if they'd had 3 chats). Not counted: a chat under 24 h old with no reply yet, and a chat where the Seeker *called* (the worker may have answered by phone) with no chat reply. |
| **Responsiveness**: reply speed | 8 | Each answered chat scores 1 / (1 + minutes to first reply / 60), so a reply after an hour earns half; reply times are capped at 1 day. An unanswered chat scores 0, so never replying can't beat replying late. Smoothed like the reply rate, as if the worker already had 3 chats answered in an hour. |
| **Activity**: recent | 14 | 0.5^(days since last active / 7): halves every week away. Last active = last opened their Inbox (`Users.LastActiveAt`, stamped at most hourly) or last edited the listing, whichever is later. |
| **Activity**: tenure | 6 | Days since the account was made / 180, full at 6 months. Small on purpose, so new workers aren't buried. |
| **Budget fit** | 20 | The worker's price for the searched job (that job's price guide at their rate: minimum, average or above average; their cheapest job when the whole trade is searched) against the top of the Seeker's range: at or under = 1, "depends on the job" = 0.4, over loses fit in step with how far over (50% over = 0.5, double = 0). No budget, or a range with no top = 1 for everyone. Listings and budgets from app versions before price guides read as their old tier's typical price and range. |
| **Experience** | 10 | Position on the experience scale (0 = least, 1 = 10+ years). |

### Badges: up to +10 on top

| Badge (approved by a person at Tarbaho) | Bonus |
|---|---|
| **ID Verified** (a government ID checked against the profile photo) | 4 |
| **TESDA Certified** (a National Certificate or training certificate) | 3 |
| **Past project** with a developer or any other company (several count once) | 3 |

`RankScore.Trust` (`SearchRanker.TrustFeature`). It is a **bonus on top of the 100, not a share of it**: a worker
without badges scores exactly as before, and nobody's order changes until someone earns a badge. Between two
otherwise equal workers the one with a badge comes first; a worker with all three gains 10 points, about what the
whole experience part is worth, so badges help but can't carry a worker who ignores chats or has poor reviews. It
is the one signal a brand-new worker can earn on day one. Seekers see the same badges as chips on the result card
(`BadgeChips`) and under "What we checked" on the profile, so the order can be read off the screen. Approving a
badge clears the 30 s search cache.

**Why these weights.** They follow the order you suggested (reviews, then responsiveness, then activity), with the
40% meant for boosts moved out of the score (see below). The slot you left open went to budget fit and experience:
money is the hook for both sides (the "Pasok sa budget" badge), and experience is something Seekers see on every
card. Photos no longer score because every published listing must have them.

**Cold start.** Every behavioural feature starts from a neutral prior instead of zero. A brand-new worker lands
in the middle of the list, above a worker who ignores most chats and below a proven one, so newcomers can win
their first customers. FAANG ranking systems handle new listings the same way.

**Data.** `Chats.ProfessionalRepliedAt` (the worker's first reply, set by `ChatStore.SendAsync`; schema change `005-schema-ranking-signals.sql`
fills it in for older chats) and `Users.LastActiveAt` (set by `UserStore.TouchAsync` from `GET chats?side=professional`).
`ProfessionalStore` reads them in two grouped queries alongside the listings and caches them with the listings for
30 s (`ListingCache`). They never leave the server.

## 3. Boosts (monetization, not built yet)

**Recommendation: don't make a boost 40% of the score. Sell labeled sponsored slots instead.**

- If 40% of the score is paid, any boosted worker beats any unboosted one, even with bad reviews and unanswered
  chats. Seekers stop trusting the order, and we're no longer the neutral "Yellow Pages". Paid placement that
  isn't marked as paid also runs into consumer-protection disclosure rules.
- Every big marketplace keeps two separate orders: the **organic** order (this score) and an **ads** order (bid ×
  quality). Ads get fixed slots and a "Sponsored" label.

The plan, when boosts ship:

1. **Data.** A `Boosts` table (worker, trade, starts, ends, maybe towns), filled by a subscription.
2. **Eligibility.** The worker must pass both hard filters (exact job, the Seeker's own town) and a quality floor,
   e.g. organic score ≥ 50 and reply rate ≥ 50%. Paying can't get a bad worker in front of a customer.
3. **Placement.** After `Rank`, put up to one sponsored worker at the top of each page (`Paging.Search` = 4, so
   about 1 in 4), marked `Sponsored` on `SearchHitDto` (a new field, so older apps still work). Among boosted
   workers, order by organic score. With a flat subscription that *is* bid × quality. Bids can come later.
4. **No double listing.** The sponsored worker is removed from their organic position on that page.
5. **Measure.** Log impressions, profile opens and chats per position. Those logs show whether boosts pay off
   for workers, and they're the training data for a learned ranking model later.

## Next steps

- **Log search impressions and outcomes** (what was shown in which position, what got opened, chatted, reviewed).
  It's cheap to start now, and it's what lets us tune these weights from data or train a learning-to-rank model later.
- Show the signals to Seekers as trust badges ("Usually replies within an hour"), like Airbnb's response rate.
