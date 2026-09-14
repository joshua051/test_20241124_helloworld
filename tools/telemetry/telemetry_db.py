#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sqlite3
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCHEMA = Path(__file__).with_name("schema.sql")
DEFAULT_DB = ROOT / "LocalData" / "iron_sand.sqlite3"
STATUS = ROOT / "docs" / "PROJECT_STATUS.json"


def now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def git_head() -> str:
    try:
        return subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    except Exception:
        return "unknown"


def connect(path: Path) -> sqlite3.Connection:
    path.parent.mkdir(parents=True, exist_ok=True)
    db = sqlite3.connect(path)
    db.row_factory = sqlite3.Row
    db.executescript(SCHEMA.read_text(encoding="utf-8"))
    return db


def ingest_status(db: sqlite3.Connection, status_path: Path) -> int:
    data = json.loads(status_path.read_text(encoding="utf-8"))
    commit = git_head()
    cur = db.execute(
        "INSERT INTO project_snapshots(captured_at,commit_sha,branch,pr_number,source) VALUES(?,?,?,?,?)",
        (now_iso(), commit, "feat/arena-prototype-v0.1.0", 1, str(status_path.relative_to(ROOT))),
    )
    snapshot_id = cur.lastrowid
    for row in data.get("dimensions", []):
        db.execute(
            "INSERT INTO progress_dimensions(snapshot_id,key,label,maturity_percent,evidence) VALUES(?,?,?,?,?)",
            (snapshot_id, row["key"], row["label"], row["maturity_percent"], row.get("evidence")),
        )
    for row in data.get("gates", []):
        db.execute(
            "INSERT INTO validation_gates(snapshot_id,gate,label,status,percent,notes) VALUES(?,?,?,?,?,?)",
            (snapshot_id, row["gate"], row["label"], row["status"], row["percent"], row.get("notes")),
        )
    db.commit()
    return snapshot_id


def ingest_jsonl(db: sqlite3.Connection, path: Path) -> tuple[int, int]:
    sessions = events = 0
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not line.strip():
            continue
        try:
            row = json.loads(line)
        except json.JSONDecodeError as exc:
            raise SystemExit(f"{path}:{number}: invalid JSON: {exc}") from exc
        kind = row.get("record_type", "combat_event")
        if kind == "session_start":
            db.execute(
                "INSERT OR REPLACE INTO combat_sessions(session_id,commit_sha,started_at,platform,build_kind,unity_version) VALUES(?,?,?,?,?,?)",
                (row["session_id"], row.get("commit_sha", "unknown"), row.get("timestamp", now_iso()), row.get("platform"), row.get("build_kind"), row.get("unity_version")),
            )
            sessions += 1
        elif kind == "session_end":
            db.execute(
                "UPDATE combat_sessions SET ended_at=?,outcome=?,duration_seconds=?,wave_reached=?,player_damage_taken=?,player_damage_dealt=?,kills=?,deaths=?,max_combo=?,style_score=?,crowd_favor=?,fps_avg=?,fps_p95_low=?,notes=? WHERE session_id=?",
                (row.get("timestamp"), row.get("outcome"), row.get("duration_seconds"), row.get("wave_reached"), row.get("player_damage_taken",0), row.get("player_damage_dealt",0), row.get("kills",0), row.get("deaths",0), row.get("max_combo",0), row.get("style_score",0), row.get("crowd_favor",0), row.get("fps_avg"), row.get("fps_p95_low"), row.get("notes"), row["session_id"]),
            )
        elif kind == "balance_parameter":
            value = row.get("value")
            value_num = float(value) if isinstance(value, (int, float)) else None
            value_text = None if value_num is not None else (None if value is None else str(value))
            db.execute(
                "INSERT OR REPLACE INTO balance_parameters(commit_sha,system,entity,parameter,value_num,value_text,unit,source_file,source_symbol,captured_at) VALUES(?,?,?,?,?,?,?,?,?,?)",
                (row.get("commit_sha", git_head()), row["system"], row["entity"], row["parameter"], value_num, value_text, row.get("unit"), row.get("source_file"), row.get("source_symbol"), row.get("timestamp", now_iso())),
            )
        else:
            db.execute(
                "INSERT INTO combat_events(session_id,ts_seconds,event_type,actor,target,weapon,attack_kind,damage,poise_damage,hp_before,hp_after,poise_before,poise_after,combo,style_score,crowd_favor,x,y,z,payload_json) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                (row["session_id"], row.get("ts_seconds",0), row.get("event_type",kind), row.get("actor"), row.get("target"), row.get("weapon"), row.get("attack_kind"), row.get("damage"), row.get("poise_damage"), row.get("hp_before"), row.get("hp_after"), row.get("poise_before"), row.get("poise_after"), row.get("combo"), row.get("style_score"), row.get("crowd_favor"), row.get("x"), row.get("y"), row.get("z"), json.dumps(row.get("payload",{}), ensure_ascii=False, separators=(",",":"))),
            )
            events += 1
    db.commit()
    return sessions, events


def run_query(db: sqlite3.Connection, sql: str) -> None:
    rows = db.execute(sql).fetchall()
    if not rows:
        print("(no rows)")
        return
    columns = rows[0].keys()
    print("\t".join(columns))
    for row in rows:
        print("\t".join("" if row[c] is None else str(row[c]) for c in columns))


def main() -> int:
    parser = argparse.ArgumentParser(description="Iron Sand Arena local SQLite telemetry database")
    parser.add_argument("--db", type=Path, default=DEFAULT_DB)
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("init")
    status_cmd = sub.add_parser("ingest-status")
    status_cmd.add_argument("path", nargs="?", type=Path, default=STATUS)
    jsonl_cmd = sub.add_parser("ingest-jsonl")
    jsonl_cmd.add_argument("path", type=Path)
    query_cmd = sub.add_parser("query")
    query_cmd.add_argument("sql")
    args = parser.parse_args()

    with connect(args.db) as db:
        if args.command == "init":
            print(args.db)
        elif args.command == "ingest-status":
            print(f"snapshot_id={ingest_status(db, args.path)}")
        elif args.command == "ingest-jsonl":
            sessions, events = ingest_jsonl(db, args.path)
            print(f"sessions={sessions} events={events}")
        elif args.command == "query":
            run_query(db, args.sql)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
