# Telemetry and Local Learning Database

Iron Sand Arena keeps runtime capture and analytics storage separate:

- Unity writes append-only JSONL telemetry under `Application.persistentDataPath/IronSandTelemetry/`.
- `tools/telemetry/telemetry_db.py` imports project status and runtime JSONL into local SQLite.
- SQLite files live under `LocalData/` by default and are intentionally ignored by Git.

This avoids shipping a platform-specific SQLite native dependency inside the Unity player while keeping the local data fully queryable by Codex, Python, sqlite3, Datasette, DBeaver or any other SQLite client.

## Initialize

```bash
python3 tools/telemetry/telemetry_db.py init
python3 tools/telemetry/telemetry_db.py ingest-status
```

Default database:

```text
LocalData/iron_sand.sqlite3
```

Use another path with `--db`.

## Import a Unity telemetry file

After a Play Mode or standalone session, find the JSONL path printed by `TelemetryRecorder` in the Unity Console. Then run:

```bash
python3 tools/telemetry/telemetry_db.py ingest-jsonl /path/to/combat-YYYYMMDD-HHMMSS-sessionid.jsonl
```

## Core tables

- `project_snapshots`: commit/version/status snapshots.
- `progress_dimensions`: the ten engineering maturity dimensions used for progress charts.
- `validation_gates`: R0 and U1-U7 evidence state.
- `balance_parameters`: versioned weapon/attack/AI/Crowd tuning values.
- `combat_sessions`: one row per play session.
- `combat_events`: attacks, hits, blocks, Perfect Guards, throws, executions, Crowd actions and other event telemetry.
- `test_runs`: EditMode/PlayMode/standalone validation history.
- `defects`: reproducible defects and fixed commits.
- `feel_ratings`: manual 0-10 ratings for weight, responsiveness, camera, readability and similar qualitative dimensions.
- `artifacts`: logs, captures, test XML and hashes.

Views `latest_progress`, `latest_gates` and `weapon_performance` cover common queries.

## Query examples

Current project maturity:

```bash
python3 tools/telemetry/telemetry_db.py query \
  "SELECT label, maturity_percent FROM latest_progress ORDER BY maturity_percent DESC"
```

Validation blockers:

```bash
python3 tools/telemetry/telemetry_db.py query \
  "SELECT gate,label,status FROM latest_gates WHERE status <> 'PASS'"
```

Weapon damage from recorded sessions:

```bash
python3 tools/telemetry/telemetry_db.py query \
  "SELECT weapon,COUNT(*) hits,SUM(damage) damage FROM combat_events WHERE damage IS NOT NULL GROUP BY weapon ORDER BY damage DESC"
```

Compare manual feel scores by session:

```sql
SELECT s.commit_sha, f.dimension, AVG(f.score) AS avg_score
FROM feel_ratings f
JOIN combat_sessions s USING(session_id)
GROUP BY s.commit_sha, f.dimension
ORDER BY f.dimension, s.commit_sha;
```

Find regressions after a commit:

```sql
SELECT commit_sha, suite, passed, failed, duration_seconds
FROM test_runs
ORDER BY id DESC;
```

## Learning workflow

Do not let an agent optimize directly from one session. Prefer multiple sessions for the same commit, compare distributions, and keep human feel ratings alongside quantitative telemetry. Any automatically proposed balance change should preserve the source commit, old value, proposed value, evidence query and validation result so later regressions can be traced.

The database is local analytics state, not authoritative game content. Source-controlled tuning remains in C# until the project later moves selected parameters into authored data assets or a production configuration layer.
