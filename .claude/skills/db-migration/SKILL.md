---
name: db-migration
description: Change SideGigPh's live Azure SQL schema safely. Every table, column, index or constraint change after the first release goes in a new numbered script (SideGigPh.AzureSQL/Sql/NNN-schema-<what>.sql) that checks before each step (IF OBJECT_ID / COL_LENGTH / sys.indexes … IS NULL), never an edit to 001-schema.sql and never a drop-and-create. Use whenever a feature adds or changes a table, column, index, constraint or default, backfills existing rows, or when the user asks for a "migration", "schema change", "ALTER TABLE" or "update script". Includes new-migration.sh (makes the next-numbered file from the template) and migration-check.sh (fails on an edited 001, an unguarded step, or a missing SchemaChanges row).
---

# Database changes on a live database

The Azure SQL database is **live**. Its data may not matter yet (alpha), but its objects already exist and real
testers write rows to it. So:

- **`001-schema.sql` is frozen.** It is the first release's schema, already run on the live database. Never edit
  it, not even to "tidy" a `CREATE TABLE` (an edited `CREATE TABLE` never runs on a database where the table
  exists, so live and fresh databases would drift).
- **Never drop and create** a table to change it, because that loses the data. Use `ALTER`.
- **Every change is a new numbered script**, `NNN-schema-<what>.sql`, numbered after the highest file in
  `SideGigPh.AzureSQL/Sql/` (001–004 are taken: 002 seed, 003 seed rollback, 004 config; the first change was
  `005-schema-ranking-signals.sql`). Numbers never repeat and a script is never edited after it ran live. To fix
  a mistake, write the next script.
- **Every step checks first**, so the script is safe to run twice or on any database:

| Change | Guard |
|---|---|
| New table | `IF OBJECT_ID(N'dbo.T', N'U') IS NULL BEGIN CREATE TABLE … END;` |
| New column | `IF OBJECT_ID(N'dbo.T', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.T', N'C') IS NULL EXEC (N'ALTER TABLE dbo.T ADD C … NULL;');` |
| Backfill a new column | A second `EXEC (N'UPDATE …')` inside the same `IF`. `EXEC` is needed because a batch that names a column added in that same batch doesn't compile. |
| NOT NULL column | Add it with a named default (`CONSTRAINT DF_T_C DEFAULT (…)`), or add it NULL, backfill, then `ALTER COLUMN … NOT NULL`, each step guarded. |
| New index | `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_…' AND object_id = OBJECT_ID(N'dbo.T')) EXEC (N'CREATE INDEX …');` |
| New constraint / check | `IF OBJECT_ID(N'CK_…') IS NULL EXEC (N'ALTER TABLE dbo.T ADD CONSTRAINT CK_… CHECK (…);');` |
| Change a column's type or length | `IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.T') AND name = N'C' AND max_length <> 120) EXEC (N'ALTER TABLE dbo.T ALTER COLUMN C nvarchar(60) NULL;');` Drop and recreate any index on the column around it. |
| Rename or remove | Don't, while an older API version might still read it. Add the new thing, ship the API, and remove the old one in a later script once nothing reads it. |

- **Each script records itself** in `dbo.SchemaChanges` (it creates the table if it's missing), so
  `SELECT * FROM dbo.SchemaChanges` shows which changes the live database has had.
- The script runs inside `SET XACT_ABORT ON; BEGIN TRANSACTION; … COMMIT;`, so it applies fully or not at all.
- Name every constraint and index (`PK_`, `UQ_`, `UX_`, `IX_`, `CK_`, `DF_`, `FK_`), the same names `SideGigPhDb` uses.

## Recipe

1. `bash .claude/skills/db-migration/scripts/new-migration.sh <short-what>` (e.g. `boosts`) creates
   `SideGigPh.AzureSQL/Sql/NNN-schema-<short-what>.sql` from `template.sql`, numbered after the highest file there.
2. Write the guarded steps, using the table above. Keep the header comment: what it adds, and why.
3. Mirror it in code: the `*Row` property in `Rows.cs` and any mapping in `SideGigPhDb.cs` (no EF migrations; EF
   must match 001 plus every change). Sample rows go in `002-seed.sql` only if the UI needs them.
4. `bash .claude/skills/db-migration/scripts/migration-check.sh` checks 001 is unchanged, every step in new scripts
   is guarded, and each script records itself.
5. Run tests (`run-tests` skill). The integration tests build their database the way live is upgraded (001, then
   every `NNN-schema-*.sql` in order, then 004-config), and `SchemaScriptTests` runs every schema script a second
   time to prove the guards.
6. Update `docs/data-model.md` (the table's columns) and `CLAUDE.md` if the change matters to the architecture.
7. **Deploy order:** add the script's name to `SchemaScripts.Required` (`SchemaCheckTests` fails otherwise). The
   push to `main` runs `deploy-api.yml`: `db-check` lists the pending scripts, `db-migrate` runs them **after the
   owner approves** in the GitHub environment `live-database` (tool `SideGigPh.Migrate`), and only then the API
   deploys. Tell the user in your report that a push with a new script waits for their approval (Actions → the run
   → Review deployments). `004-config.sql` runs only on a manual run with *Also run 004-config.sql* ticked.

## Data scripts are different

`004-config.sql` (required answer lists, an idempotent upsert you re-run after editing it) and `002-seed.sql` /
`003-rollback-seed.sql` / `006-seed-search-ranking.sql` (sample data) change rows, not schema. They keep their own numbers and are edited in place.
