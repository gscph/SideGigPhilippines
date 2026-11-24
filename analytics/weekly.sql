/* =====================================================================================
   Tarbaho weekly report: our own usage events (dbo.app_events, read through dbo.v_events).

   Two questions: do matches happen (someone needs a service and gets one), and do they repeat (customers rehire,
   workers come back)? Run it in the Azure portal's Query editor ONE DAY AFTER the week ends (weeks start on Monday,
   Philippine time), so searches still waiting on phones have time to arrive.

   The events (SideGigPh.Contracts/AppEvents.cs): sign_up, search, search_empty (app), request_created,
   request_matched, job_completed, rehire, worker_profile_complete, worker_first_job, report_user, block_user.
   A "request" is a household's job post; "matched" is the household picking a worker's offer.
   ===================================================================================== */

-- 1. Jobs completed per week (main number)
SELECT week_start_ph, COUNT(*) AS jobs
FROM dbo.v_events WHERE event_name = 'job_completed'
GROUP BY week_start_ph ORDER BY week_start_ph;

-- 2. Fulfilment rate + speed (last 7 days)
SELECT
  SUM(CASE WHEN event_name = 'request_created' THEN 1 ELSE 0 END) AS created,
  COUNT(DISTINCT CASE WHEN event_name = 'request_matched' THEN request_id END) AS matched, -- a cancelled pick picked again is one request
  AVG(CASE WHEN event_name = 'request_matched' THEN minutes_to_match END) AS avg_minutes_to_match
FROM dbo.v_events
WHERE event_ts_ph >= DATEADD(DAY, -7, DATEADD(HOUR, 8, SYSUTCDATETIME()));

-- 3. Unmet demand: top empty searches (last 7 days)
SELECT TOP 20 area, service_category, search_term, COUNT(*) AS n
FROM dbo.v_events
WHERE event_name = 'search_empty'
  AND event_ts_ph >= DATEADD(DAY, -7, DATEADD(HOUR, 8, SYSUTCDATETIME()))
GROUP BY area, service_category, search_term ORDER BY n DESC;

-- 4. Demand by category and area (last 7 days)
SELECT service_category, area, COUNT(*) AS searches,
       SUM(CASE WHEN result_count = 0 THEN 1 ELSE 0 END) AS empty_searches
FROM dbo.v_events
WHERE event_name = 'search'
  AND event_ts_ph >= DATEADD(DAY, -7, DATEADD(HOUR, 8, SYSUTCDATETIME()))
GROUP BY service_category, area ORDER BY searches DESC;

-- 5. Worker activation funnel (all time)
SELECT
  (SELECT COUNT(DISTINCT user_id) FROM dbo.app_events WHERE event_name = 'sign_up' AND user_type = 'worker') AS signed_up,
  (SELECT COUNT(DISTINCT user_id) FROM dbo.app_events WHERE event_name = 'worker_profile_complete') AS profile_complete,
  (SELECT COUNT(DISTINCT user_id) FROM dbo.app_events WHERE event_name = 'worker_first_job') AS first_job;

-- 6. Rehires per week
SELECT week_start_ph, COUNT(*) AS rehires
FROM dbo.v_events WHERE event_name = 'rehire'
GROUP BY week_start_ph ORDER BY week_start_ph;

-- 7. Trust: reports/blocks by reason (last 30 days)
SELECT event_name, reason, COUNT(*) AS n
FROM dbo.v_events
WHERE event_name IN ('report_user', 'block_user')
  AND event_ts_ph >= DATEADD(DAY, -30, DATEADD(HOUR, 8, SYSUTCDATETIME()))
GROUP BY event_name, reason ORDER BY n DESC;
