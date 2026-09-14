#!/usr/bin/env python3
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MAX_TRACKED_BYTES = 20 * 1024 * 1024
FORBIDDEN_ROOTS = {"library", "temp", "obj", "logs", "usersettings", "memorycaptures"}
TEXT_SUFFIXES = {".cs", ".md", ".json", ".yml", ".yaml", ".py", ".txt", ".asmdef", ".gitignore"}
REQUIRED_PATHS = {
    "README.md",
    "docs/ARCHITECTURE.md",
    "docs/QUALITY_GATES.md",
    "docs/VALIDATION.md",
    "ProjectSettings/ProjectVersion.txt",
}
EXPECTED_EDITOR = "m_EditorVersion: 6000.3.23f1"


def tracked_files() -> list[str]:
    output = subprocess.check_output(["git", "ls-files", "-z"], cwd=ROOT)
    return [item for item in output.decode("utf-8").split("\0") if item]


def main() -> int:
    errors: list[str] = []
    files = tracked_files()
    lowered: dict[str, str] = {}

    for relative in files:
        path = ROOT / relative
        first_part = Path(relative).parts[0].lower() if Path(relative).parts else ""
        if first_part in FORBIDDEN_ROOTS:
            errors.append(f"generated Unity directory is tracked: {relative}")

        case_key = relative.lower()
        previous = lowered.get(case_key)
        if previous is not None and previous != relative:
            errors.append(f"case-colliding paths: {previous} <-> {relative}")
        else:
            lowered[case_key] = relative

        if path.exists() and path.is_file() and path.stat().st_size > MAX_TRACKED_BYTES:
            errors.append(f"tracked file exceeds 20 MiB; use an intentional asset/LFS policy: {relative}")

        if path.suffix.lower() in TEXT_SUFFIXES or path.name == ".gitignore":
            try:
                text = path.read_text(encoding="utf-8")
            except UnicodeDecodeError:
                errors.append(f"expected UTF-8 text file is not UTF-8: {relative}")
                continue

            markers = ("<" * 7, "=" * 7, ">" * 7)
            if any(marker in text for marker in markers):
                errors.append(f"merge-conflict marker found: {relative}")

    missing = sorted(REQUIRED_PATHS.difference(files))
    for relative in missing:
        errors.append(f"required project file missing: {relative}")

    project_version = ROOT / "ProjectSettings/ProjectVersion.txt"
    if project_version.exists():
        text = project_version.read_text(encoding="utf-8")
        if EXPECTED_EDITOR not in text:
            errors.append(f"Unity editor version is not pinned to 6000.3.23f1: {project_version.relative_to(ROOT)}")

    validation = ROOT / "docs/VALIDATION.md"
    if validation.exists() and "## Gate status" not in validation.read_text(encoding="utf-8"):
        errors.append("docs/VALIDATION.md must contain an explicit Gate status section")

    if errors:
        print("Repository guard FAILED:\n")
        for error in errors:
            print(f"- {error}")
        return 1

    print(f"Repository guard PASS ({len(files)} tracked files checked).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
