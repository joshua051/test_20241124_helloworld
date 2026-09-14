PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=ON;

CREATE TABLE IF NOT EXISTS project_snapshots (
  id INTEGER PRIMARY KEY,
  captured_at TEXT NOT NULL,
  commit_sha TEXT NOT NULL,
  unity_version TEXT,
  branch TEXT,
  pr_number INTEGER,
  source TEXT NOT NULL DEFAULT 'local'
);

CREATE TABLE IF NOT EXISTS progress_dimensions (
  snapshot_id INTEGER NOT NULL REFERENCES project_snapshots(id) ON DELETE CASCADE,
  key TEXT NOT NULL,
  label TEXT NOT NULL,
  maturity_percent REAL NOT NULL CHECK(maturity_percent BETWEEN 0 AND 100),
  evidence TEXT,
  PRIMARY KEY(snapshot_id, key)
);

CREATE TABLE IF NOT EXISTS validation_gates (
  snapshot_id INTEGER NOT NULL REFERENCES project_snapshots(id) ON DELETE CASCADE,
  gate TEXT NOT NULL,
  label TEXT NOT NULL,
  status TEXT NOT NULL,
  percent REAL NOT NULL CHECK(percent BETWEEN 0 AND 100),
  evidence_uri TEXT,
  notes TEXT,
  PRIMARY KEY(snapshot_id, gate)
);

CREATE TABLE IF NOT EXISTS balance_parameters (
  id INTEGER PRIMARY KEY,
  commit_sha TEXT NOT NULL,
  system TEXT NOT NULL,
  entity TEXT NOT NULL,
  parameter TEXT NOT NULL,
  value_num REAL,
  value_text TEXT,
  unit TEXT,
  source_file TEXT,
  source_symbol TEXT,
  captured_at TEXT NOT NULL,
  UNIQUE(commit_sha, system, entity, parameter)
);

CREATE TABLE IF NOT EXISTS balance_experiments (
  id INTEGER PRIMARY KEY,
  created_at TEXT NOT NULL,
  baseline_commit TEXT NOT NULL,
  candidate_commit TEXT,
  system TEXT NOT NULL,
  entity TEXT NOT NULL,
  parameter TEXT NOT NULL,
  baseline_value TEXT,
  candidate_value TEXT,
  hypothesis TEXT NOT NULL,
  evidence_query TEXT,
  status TEXT NOT NULL DEFAULT 'proposed',
  outcome TEXT,
  reverted INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS combat_sessions (
  session_id TEXT PRIMARY KEY,
  commit_sha TEXT NOT NULL,
  started_at TEXT NOT NULL,
  ended_at TEXT,
  platform TEXT,
  build_kind TEXT,
  unity_version TEXT,
  outcome TEXT,
  duration_seconds REAL,
  wave_reached INTEGER,
  player_damage_taken REAL DEFAULT 0,
  player_damage_dealt REAL DEFAULT 0,
  kills INTEGER DEFAULT 0,
  deaths INTEGER DEFAULT 0,
  max_combo INTEGER DEFAULT 0,
  style_score INTEGER DEFAULT 0,
  crowd_favor INTEGER DEFAULT 0,
  fps_avg REAL,
  fps_p95_low REAL,
  notes TEXT
);

CREATE TABLE IF NOT EXISTS combat_events (
  id INTEGER PRIMARY KEY,
  session_id TEXT NOT NULL REFERENCES combat_sessions(session_id) ON DELETE CASCADE,
  ts_seconds REAL NOT NULL,
  event_type TEXT NOT NULL,
  actor TEXT,
  target TEXT,
  weapon TEXT,
  attack_kind TEXT,
  damage REAL,
  poise_damage REAL,
  hp_before REAL,
  hp_after REAL,
  poise_before REAL,
  poise_after REAL,
  combo INTEGER,
  style_score INTEGER,
  crowd_favor INTEGER,
  x REAL,
  y REAL,
  z REAL,
  payload_json TEXT
);
CREATE INDEX IF NOT EXISTS idx_combat_events_session_type ON combat_events(session_id, event_type);
CREATE INDEX IF NOT EXISTS idx_combat_events_weapon ON combat_events(weapon);

CREATE TABLE IF NOT EXISTS test_runs (
  id INTEGER PRIMARY KEY,
  commit_sha TEXT NOT NULL,
  run_at TEXT NOT NULL,
  suite TEXT NOT NULL,
  platform TEXT,
  passed INTEGER NOT NULL,
  failed INTEGER NOT NULL,
  skipped INTEGER NOT NULL DEFAULT 0,
  duration_seconds REAL,
  artifact_path TEXT,
  notes TEXT
);

CREATE TABLE IF NOT EXISTS defects (
  id INTEGER PRIMARY KEY,
  commit_sha TEXT NOT NULL,
  created_at TEXT NOT NULL,
  severity TEXT NOT NULL,
  area TEXT NOT NULL,
  title TEXT NOT NULL,
  reproduction TEXT,
  status TEXT NOT NULL DEFAULT 'open',
  fixed_commit TEXT,
  evidence_uri TEXT
);

CREATE TABLE IF NOT EXISTS feel_ratings (
  id INTEGER PRIMARY KEY,
  session_id TEXT NOT NULL REFERENCES combat_sessions(session_id) ON DELETE CASCADE,
  dimension TEXT NOT NULL,
  score REAL NOT NULL CHECK(score BETWEEN 0 AND 10),
  note TEXT,
  UNIQUE(session_id, dimension)
);

CREATE TABLE IF NOT EXISTS artifacts (
  id INTEGER PRIMARY KEY,
  commit_sha TEXT NOT NULL,
  created_at TEXT NOT NULL,
  kind TEXT NOT NULL,
  path TEXT NOT NULL,
  sha256 TEXT,
  metadata_json TEXT
);

CREATE VIEW IF NOT EXISTS latest_progress AS
SELECT d.*
FROM progress_dimensions d
JOIN project_snapshots s ON s.id=d.snapshot_id
WHERE s.id=(SELECT MAX(id) FROM project_snapshots);

CREATE VIEW IF NOT EXISTS latest_gates AS
SELECT g.*
FROM validation_gates g
JOIN project_snapshots s ON s.id=g.snapshot_id
WHERE s.id=(SELECT MAX(id) FROM project_snapshots);

CREATE VIEW IF NOT EXISTS weapon_performance AS
SELECT weapon,
       COUNT(*) AS hit_events,
       SUM(COALESCE(damage,0)) AS damage,
       AVG(CASE WHEN event_type='perfect_guard' THEN 1.0 ELSE 0.0 END) AS perfect_guard_event_share
FROM combat_events
WHERE weapon IS NOT NULL
GROUP BY weapon;
