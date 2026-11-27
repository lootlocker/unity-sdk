#!/usr/bin/env bash
# generate-doxygen-docs.sh — LootLocker Unity SDK
# 1. Syncs the mainpage navigation table from groups.dox
# 2. Reads the SDK version from package.json
# 3. Runs Doxygen
# 4. Makes the generated HTML SEO compliant (canonical URLs, unique titles,
#    meta descriptions, <h1> headings, sitemap.xml)
# Run from the repo root:  bash .doxygen/generate-doxygen-docs.sh
set -euo pipefail

GROUPS_FILE="${1:-.doxygen/groups.dox}"
MAINPAGE_FILE="${2:-.doxygen/mainpage.md}"
PYTHON="${PYTHON:-python3}"

# ── 1. Sync navigation table ──────────────────────────────────────────
"$PYTHON" - "$GROUPS_FILE" "$MAINPAGE_FILE" << 'EOF'
import re, sys

groups_file = sys.argv[1]
mainpage_file = sys.argv[2]

with open(groups_file) as f:
    content = f.read()

pattern = r'/// @defgroup\s+(\w+)\s+([^\r\n]+)(?:\r?\n/// @brief\s+([^\r\n]+))?'
matches = re.findall(pattern, content)

rows = ['| Topic | What it covers |', '|-------|---------------|']
for name, display, brief in matches:
    desc = brief.strip() if brief.strip() else display.strip()
    rows.append(f'| @ref {name} "{display.strip()}" | {desc} |')
new_table = '\n'.join(rows)

with open(mainpage_file) as f:
    text = f.read()

updated = re.sub(
    r'\| Topic \| What it covers \|.*?(?=\r?\n---)',
    new_table,
    text,
    flags=re.DOTALL
)

with open(mainpage_file, 'w') as f:
    f.write(updated)

print(f"sync-nav: updated {mainpage_file} with {len(matches)} groups")
EOF

# ── 2. Extract version ────────────────────────────────────────────────
export LL_SDK_VERSION
LL_SDK_VERSION="$(jq -r '.version' package.json)"
echo "generate-doxygen-docs: version=${LL_SDK_VERSION}"

# ── 3. Run Doxygen ────────────────────────────────────────────────────
doxygen .doxygen/Doxyfile

# ── 4. Make the output SEO compliant ──────────────────────────────────
# Canonical URLs point at latest/, which is the version we want indexed.
"$PYTHON" .doxygen/seo-postprocess.py \
  --html-dir docs/html \
  --base-url https://ref.lootlocker.com/unity \
  --project "LootLocker Unity SDK"
