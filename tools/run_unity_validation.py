#!/usr/bin/env python3
"""Validate a committed snapshot in isolation; never promote manual Unity gates."""
from __future__ import annotations
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import signal
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET
import zipfile

EDITOR = "6000.3.23f1"
TARGETS = {"StandaloneWindows64", "StandaloneOSX", "StandaloneLinux64"}
ROOT = Path(__file__).resolve().parents[1]
REQUIRED_SUITES = {
    "EditMode": {"ImportedGladiatorTests", "ArenaRegressionTests", "CombatCoreTests", "StateIntegrityTests"},
    "PlayMode": {"ArenaSmokeTests", "ArenaStateIntegrityTests", "ArenaIntegrationTests"},
}

class ValidationError(RuntimeError):
    pass


def read_test_result(path: Path, required_suites: set[str]) -> dict:
    """Fail closed for empty, skipped, malformed, incomplete or missing NUnit results."""
    if not path.is_file() or not 0 < path.stat().st_size <= 20 * 1024 * 1024:
        raise ValidationError(f"Missing, empty or oversized test XML: {path}")
    data = path.read_bytes()
    if b"<!DOCTYPE" in data.upper() or b"<!ENTITY" in data.upper():
        raise ValidationError("External/entity XML declarations are not allowed")
    try:
        root = ET.fromstring(data)
        if root.tag != "test-run" or root.get("result") != "Passed":
            raise ValidationError("Test run did not report Passed")
        total = int(root.attrib["total"])
        passed = int(root.attrib["passed"])
        failed = int(root.attrib["failed"])
        skipped = int(root.attrib["skipped"])
        inconclusive = int(root.attrib["inconclusive"])
        cases = list(root.iter("test-case"))
        if total <= 0 or passed != total or failed != 0 or skipped != 0 or inconclusive != 0:
            raise ValidationError("All tests must execute and pass; ignored/skipped tests need explicit review")
        if len(cases) != total or any(c.get("result") != "Passed" for c in cases):
            raise ValidationError("Test case results/counts disagree with the run summary")
        names = {part for c in cases for part in c.get("fullname", "").split(".")}
        missing = required_suites - names
        if missing:
            raise ValidationError("Required suites absent: " + ", ".join(sorted(missing)))
    except (ET.ParseError, ValueError, KeyError) as error:
        raise ValidationError(f"Malformed NUnit XML: {error}") from error
    return {"status": "PASS", "total": total, "passed": passed, "xml_sha256": hashlib.sha256(data).hexdigest()}


def run_process(args: list[str], log: Path, timeout: float, env: dict | None = None) -> None:
    """Capture output and bound the entire process tree's lifetime."""
    settings = {"start_new_session": True} if os.name != "nt" else {
        "creationflags": subprocess.CREATE_NEW_PROCESS_GROUP}
    with log.open("wb") as stream:
        with subprocess.Popen(args, stdout=stream, stderr=subprocess.STDOUT, env=env, **settings) as process:
            try:
                code = process.wait(timeout=timeout)
            except subprocess.TimeoutExpired as error:
                if os.name == "nt":
                    subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"],
                                   stdout=stream, stderr=subprocess.STDOUT, check=False)
                else:
                    try:
                        os.killpg(process.pid, signal.SIGKILL)
                    except ProcessLookupError:
                        pass
                process.wait()
                raise ValidationError(f"Process timed out; see {log.name}") from error
    if code != 0:
        raise ValidationError(f"Process exit code {code}; see {log.name}")


def extract_snapshot(archive: Path, project: Path) -> None:
    """Only regular committed files/directories; no traversal or symlink extraction."""
    project.mkdir()
    with zipfile.ZipFile(archive) as files:
        for entry in files.infolist():
            destination = (project / entry.filename).resolve()
            mode = (entry.external_attr >> 16) & 0o170000
            if not destination.is_relative_to(project.resolve()) or mode == 0o120000:
                raise ValidationError(f"Unsafe archive entry: {entry.filename}")
            if entry.is_dir():
                destination.mkdir(parents=True, exist_ok=True)
            else:
                destination.parent.mkdir(parents=True, exist_ok=True)
                destination.write_bytes(files.read(entry))


def check_build_receipt(path: Path, project: Path, target: str) -> dict:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
        if value["result"] != "Succeeded" or value["errors"] != 0 or value["unityVersion"] != EDITOR or value["target"] != target:
            raise ValidationError("Build receipt reports failure, wrong editor or wrong target")
        output = Path(value["output"]).resolve()
        if not output.is_relative_to(project.resolve()) or not output.exists():
            raise ValidationError("Expected standalone output was not created in the isolated project")
        files = [output] if output.is_file() else list(output.rglob("*"))
        if not any(p.is_file() and p.stat().st_size > 0 for p in files):
            raise ValidationError("Standalone output is empty")
    except (OSError, ValueError, KeyError) as error:
        raise ValidationError(f"Missing or malformed build receipt: {error}") from error
    return {"status": "PASS", "receipt": value, "boundary": "Build only; not launched or soak-tested"}


def default_target() -> str:
    return "StandaloneWindows64" if os.name == "nt" else "StandaloneOSX" if sys.platform == "darwin" else "StandaloneLinux64"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", default=os.environ.get("UNITY_EDITOR", ""))
    parser.add_argument("--out", type=Path, default=Path(os.environ.get("IRON_SAND_VALIDATION_OUT", str(ROOT / "ValidationArtifacts"))))
    parser.add_argument("--target", choices=sorted(TARGETS), default=os.environ.get("IRON_SAND_BUILD_TARGET", default_target()))
    parser.add_argument("--timeout", type=int, default=1800, help="Maximum seconds per Unity stage")
    parser.add_argument("--allow-dirty", action="store_true", help="Explicitly ignore local edits and test committed HEAD only")
    args = parser.parse_args(argv)
    run = args.out.expanduser().resolve() / (datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ") + "-" + uuid.uuid4().hex[:8])
    run.mkdir(parents=True)
    summary = {"schema": 1, "status": "BLOCKED", "stages": {}, "manual_gates": "NOT_RUN", "evidence": str(run)}
    result = 2
    try:
        if args.timeout <= 0:
            raise ValidationError("Timeout must be positive")
        editor = Path(args.unity).expanduser().resolve() if args.unity else None
        if editor is None or not editor.is_file():
            raise ValidationError("Unity executable unavailable. Set UNITY_EDITOR or --unity; no Unity test was executed.")
        commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
        status = subprocess.check_output(["git", "status", "--porcelain"], cwd=ROOT, text=True)
        summary["source_commit"] = commit
        (run / "worktree-status.txt").write_text(status, encoding="utf-8")
        if status.strip() and not args.allow_dirty:
            raise ValidationError("Worktree has local changes. Preserve/commit them, or explicitly use --allow-dirty to test HEAD only.")
        run_process([str(editor), "-version"], run / "00-version.log", 60)
        versions = re.findall(r"\b\d{4}\.\d+\.\d+[abfp]\d+\b", (run / "00-version.log").read_text(errors="replace"))
        if not versions or set(versions) != {EDITOR}:
            raise ValidationError(f"Expected Unity {EDITOR}; observed {versions}")
        archive = run / "source.zip"
        subprocess.run(["git", "archive", "--format=zip", "--output=" + str(archive), commit], cwd=ROOT, check=True)
        project = run / "Project"
        extract_snapshot(archive, project)
        # Engine writes only into this fresh disposable snapshot, never the caller's scene.
        common = [str(editor), "-batchmode", "-nographics", "-projectPath", str(project), "-buildTarget", args.target]
        env = dict(os.environ, IRON_SAND_BUILD_RECEIPT=str(run / "build-receipt.json"))
        summary["status"] = "FAILED"
        result = 1
        run_process(common + ["-executeMethod", "IronSand.Editor.PrototypeBuilder.BuildForValidation", "-logFile", str(run / "01-builder.log"), "-quit"], run / "01-process.log", args.timeout, env)
        for relative in ("Assets/Scenes/ArenaPrototype.unity", "Assets/Resources/Gladiators/RuntimeMaterial.mat", "Packages/packages-lock.json"):
            if not (project / relative).is_file():
                raise ValidationError("Builder did not create required artifact: " + relative)
        summary["stages"]["builder"] = {"status": "PASS"}
        for index, platform in enumerate(("EditMode", "PlayMode"), 2):
            output = run / f"0{index}-{platform}.xml"
            # No -quit with -runTests: the test runner must complete its own asynchronous work.
            run_process(common + ["-runTests", "-testPlatform", platform, "-testResults", str(output), "-logFile", str(run / f"0{index}-{platform}.log")], run / f"0{index}-process.log", args.timeout, env)
            summary["stages"][platform] = read_test_result(output, REQUIRED_SUITES[platform])
        run_process(common + ["-executeMethod", "IronSand.Editor.ValidationBuild.BuildCurrentTargetForValidation", "-logFile", str(run / "04-build.log"), "-quit"], run / "04-process.log", args.timeout, env)
        summary["stages"]["standalone_build"] = check_build_receipt(run / "build-receipt.json", project, args.target)
        # Leave genuinely generated scene/meta/lock/settings intact for review; never auto-upload them.
        summary["status"] = "AUTOMATED_BATCH_PASS_MANUAL_NOT_RUN"
        summary["project"] = str(project)
        result = 0
    except (ValidationError, OSError, subprocess.SubprocessError, zipfile.BadZipFile) as error:
        summary["error"] = str(error)
        print(str(error), file=sys.stderr)
    finally:
        (run / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"Evidence: {run}")
        print(f"Status: {summary['status']}; manual/standalone runtime gates remain NOT_RUN")
    return result

if __name__ == "__main__":
    sys.exit(main())
