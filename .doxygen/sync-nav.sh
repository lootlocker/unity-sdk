#!/usr/bin/env bash
# Synchronizes the mainpage navigation table with groups.dox definitions.
# Run from the repo root: bash .doxygen/sync-nav.sh
set -euo pipefail

GROUPS_FILE="${1:-.doxygen/groups.dox}"
MAINPAGE_FILE="${2:-.doxygen/mainpage.md}"

python3 - "$GROUPS_FILE" "$MAINPAGE_FILE" << 'EOF'
import re, sys

groups_file = sys.argv[1]
mainpage_file = sys.argv[2]

with open(groups_file) as f:
    content = f.read()

# Match @defgroup lines and optionally the following @brief line
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
