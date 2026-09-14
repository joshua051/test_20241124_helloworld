#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_EDITOR="${UNITY_EDITOR:-${1:-}}"
if [[ -z "$UNITY_EDITOR" ]]; then
  echo "Set UNITY_EDITOR to the Unity 6000.3.23f1 executable, e.g.:" >&2
  echo "  export UNITY_EDITOR=/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity" >&2
  exit 2
fi
if [[ ! -x "$UNITY_EDITOR" ]]; then
  echo "Unity executable is not executable: $UNITY_EDITOR" >&2
  exit 2
fi

if [[ -n "${IRON_SAND_BUILD_TARGET:-}" ]]; then
  BUILD_TARGET="$IRON_SAND_BUILD_TARGET"
else
  case "$(uname -s)" in
    Darwin*) BUILD_TARGET="StandaloneOSX" ;;
    Linux*) BUILD_TARGET="StandaloneLinux64" ;;
    *) echo "Unsupported host for validate_unity.sh; set IRON_SAND_BUILD_TARGET explicitly." >&2; exit 2 ;;
  esac
fi

OUT="${IRON_SAND_VALIDATION_OUT:-$ROOT/ValidationArtifacts}"
mkdir -p "$OUT"
COMMIT="$(git -C "$ROOT" rev-parse HEAD 2>/dev/null || echo unknown)"
printf '%s\n' "$COMMIT" > "$OUT/commit.txt"
printf '%s\n' "$UNITY_EDITOR" > "$OUT/unity-editor-path.txt"
printf '%s\n' "$BUILD_TARGET" > "$OUT/build-target.txt"

run_unity() {
  local label="$1"; shift
  echo "== $label =="
  "$UNITY_EDITOR" -batchmode -nographics -projectPath "$ROOT" "$@"
}

run_unity "Rebuild generated arena" \
  -executeMethod IronSand.Editor.PrototypeBuilder.BuildForValidation \
  -logFile "$OUT/01-scene-build.log" -quit

run_unity "EditMode tests" \
  -runTests -testPlatform EditMode \
  -testResults "$OUT/02-editmode-results.xml" \
  -logFile "$OUT/02-editmode.log"

run_unity "PlayMode tests" \
  -runTests -testPlatform PlayMode \
  -testResults "$OUT/03-playmode-results.xml" \
  -logFile "$OUT/03-playmode.log"

run_unity "Development standalone build for $BUILD_TARGET" \
  -buildTarget "$BUILD_TARGET" \
  -executeMethod IronSand.Editor.ValidationBuild.BuildCurrentTargetForValidation \
  -logFile "$OUT/04-standalone-build.log" -quit

find "$ROOT/Assets" -name '*.meta' -print | sort > "$OUT/generated-meta-files.txt" || true
if [[ -f "$ROOT/Packages/packages-lock.json" ]]; then
  cp "$ROOT/Packages/packages-lock.json" "$OUT/packages-lock.json"
fi
cp "$ROOT/ProjectSettings/ProjectVersion.txt" "$OUT/ProjectVersion.txt"

echo "Validation batch completed. Evidence directory: $OUT"
echo "Manual combat-feel and standalone soak gates are still required; see docs/VALIDATION.md."
