#!/usr/bin/env bash
# verify-compilation.sh — LootLocker Unity SDK local compilation check
# See .github/instructions/verification.md for setup instructions.
set -uo pipefail   # intentionally no -e: we handle non-zero exits ourselves

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SETTINGS_FILE="$REPO_ROOT/unity-dev-settings.json"
TEMP_PROJECT="$REPO_ROOT/Temp~/VerificationProject"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'

echo "========================================="
echo " LootLocker SDK - Compilation Check"
echo "========================================="
echo ""

# ---------------------------------------------------------------------------
# 1. Load settings
# ---------------------------------------------------------------------------
if [[ ! -f "$SETTINGS_FILE" ]]; then
  echo -e "${YELLOW}SETUP REQUIRED:${NC} 'unity-dev-settings.json' not found at repo root."
  echo "  Create unity-dev-settings.json with your Unity path."
  echo "  See .github/instructions/verification.md for the required format and examples."
  exit 1
fi

read_json_field() {
  python3 -c "import json; d=json.load(open('$1')); print(d.get('$2',''))" 2>/dev/null || true
}

UNITY_EXE=$(read_json_field "$SETTINGS_FILE" "unity_executable")
CUSTOM_PROJECT=$(read_json_field "$SETTINGS_FILE" "test_project_path")

if [[ -z "$UNITY_EXE" ]]; then
  echo -e "${RED}ERROR:${NC} 'unity_executable' is empty in unity-dev-settings.json."
  exit 1
fi
if [[ ! -f "$UNITY_EXE" || ! -x "$UNITY_EXE" ]]; then
  echo -e "${RED}ERROR:${NC} Unity executable not found or not executable: $UNITY_EXE"
  exit 1
fi

# ---------------------------------------------------------------------------
# 2. Helper: create / refresh the temporary verification project
# ---------------------------------------------------------------------------
init_temp_project() {
  echo "Creating temporary verification project at Temp~/VerificationProject ..."
  rm -rf "$TEMP_PROJECT"
  mkdir -p "$TEMP_PROJECT/Assets" "$TEMP_PROJECT/Packages" "$TEMP_PROJECT/ProjectSettings"

  # manifest.json - local file: reference to the SDK root
  cat > "$TEMP_PROJECT/Packages/manifest.json" <<EOF
{
  "dependencies": {
    "com.lootlocker.lootlockersdk": "file:${REPO_ROOT}"
  }
}
EOF

  # Minimal ProjectSettings (no game keys needed for compilation)
  cat > "$TEMP_PROJECT/ProjectSettings/ProjectSettings.asset" <<'EOF'
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!129 &1
PlayerSettings:
  companyName: LootLockerSDKVerification
  productName: LootLockerSDKVerification
EOF

  # Copy Samples so they are also compiled
  if [[ -d "$REPO_ROOT/Samples~/LootLockerExamples" ]]; then
    cp -r "$REPO_ROOT/Samples~/LootLockerExamples" "$TEMP_PROJECT/Assets/"
  fi
}

# ---------------------------------------------------------------------------
# 3. Determine project path
# ---------------------------------------------------------------------------
if [[ -n "$CUSTOM_PROJECT" && -d "$CUSTOM_PROJECT" ]]; then
  PROJECT_PATH="$CUSTOM_PROJECT"
  echo "Using custom project: $PROJECT_PATH"

  # Delete only the LootLocker compiled output artifacts so Tundra is forced to
  # recompile the SDK from source. Deleting the entire Bee folder crashes Unity;
  # deleting only outputs is safe — Tundra detects missing outputs and rebuilds them.
  echo "Removing cached LootLocker assemblies to force recompilation..."
  find "$PROJECT_PATH/Library/Bee/artifacts" -iname "*lootlocker*" -delete 2>/dev/null || true
  find "$PROJECT_PATH/Library/ScriptAssemblies" -iname "*lootlocker*" -delete 2>/dev/null || true
else
  init_temp_project
  PROJECT_PATH="$TEMP_PROJECT"
fi

# ---------------------------------------------------------------------------
# 4. Run Unity in batch mode
# ---------------------------------------------------------------------------
# On Linux/macOS, -logFile - pipes Unity output directly to stdout, which is
# simpler and more reliable than polling a file.
run_unity() {
  local proj="$1"
  echo ""
  echo "Unity:   $UNITY_EXE"
  echo "Project: $proj"
  echo ""
  UNITY_EXIT_CODE=0
  LOG_CONTENT=$("$UNITY_EXE" -batchmode -nographics -projectPath "$proj" -logFile - -quit 2>&1) \
    || UNITY_EXIT_CODE=$?
}

run_unity "$PROJECT_PATH"

# If the custom project crashed on startup (Package Manager never ran — output is tiny),
# fall back to the temp project automatically.
if [[ "$PROJECT_PATH" != "$TEMP_PROJECT" ]] && \
   ! echo "$LOG_CONTENT" | grep -q "Package Manager"; then
  echo -e "${YELLOW}Custom project did not open correctly (startup crash). Falling back to temporary project.${NC}"
  echo "To fix: open '$PROJECT_PATH' in the Unity Editor once, then re-run."
  init_temp_project
  PROJECT_PATH="$TEMP_PROJECT"
  run_unity "$PROJECT_PATH"
fi

# ---------------------------------------------------------------------------
# 5. Report results
# ---------------------------------------------------------------------------
echo ""
echo "--- Compilation result -------"

# Collect compiler error lines, excluding Licensing noise
COMPILE_ERRORS=$(echo "$LOG_CONTENT" | grep -E "error CS[0-9]+" | grep -v "Licensing::" || true)
TUNDRA_SUCCESS=$(echo "$LOG_CONTENT" | grep -c "Tundra build success" || true)
TUNDRA_FAILURE=$(echo "$LOG_CONTENT" | grep -cE "Tundra build failure|Tundra build failed" || true)

if [[ -n "$COMPILE_ERRORS" ]]; then
  echo "$COMPILE_ERRORS"
fi
echo "-----------------------------------"
echo ""

if [[ -n "$COMPILE_ERRORS" ]]; then
  ERROR_COUNT=$(echo "$COMPILE_ERRORS" | wc -l | tr -d ' ')
  echo -e "${RED}COMPILATION FAILED${NC} (${ERROR_COUNT} compiler error(s))"
  exit 1
elif [[ "$TUNDRA_FAILURE" -gt 0 ]]; then
  # Explicit Tundra failure is a hard fail regardless of exit code
  echo "$LOG_CONTENT" | grep -E "error CS[0-9]+|Scripts have compiler errors|error:" | grep -v "Licensing::" || true
  echo -e "${RED}COMPILATION FAILED${NC} (Tundra build failed)"
  exit 1
elif [[ "$TUNDRA_SUCCESS" -gt 0 ]]; then
  echo -e "${GREEN}COMPILATION SUCCEEDED${NC}"
  if [[ $UNITY_EXIT_CODE -ne 0 ]]; then
    echo -e "${YELLOW}Note: Unity exited with code $UNITY_EXIT_CODE after compilation (likely unrelated project setup - not an SDK issue).${NC}"
  fi
elif [[ $UNITY_EXIT_CODE -eq 0 ]]; then
  # No Tundra marker but Unity exited cleanly with no compiler errors — treat as success
  echo -e "${GREEN}COMPILATION SUCCEEDED${NC}"
else
  # Non-zero exit, no Tundra success, no compiler errors extracted — surface diagnostics
  echo "$LOG_CONTENT" | grep -E "error CS[0-9]+|Scripts have compiler errors|error:" | grep -v "Licensing::" || true
  echo -e "${RED}COMPILATION FAILED${NC} (exit code: $UNITY_EXIT_CODE)"
  exit 1
fi
