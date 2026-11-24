/* =====================================================================================
   Tarbaho usage events: delete what is older than 12 months.

   The API already does this by itself (AppEventStore, at most once a day per server instance, when phones upload
   their events), so this is only needed by hand, e.g. after a long quiet spell. Run it in the Azure portal's Query
   editor. Safe to run any time.
   ===================================================================================== */

DELETE FROM dbo.app_events WHERE server_ts < DATEADD(MONTH, -12, SYSUTCDATETIME());
