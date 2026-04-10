# generate-doxygen-docs.ps1 — LootLocker Unity SDK
# 1. Syncs the mainpage navigation table from groups.dox
# 2. Reads the SDK version from package.json
# 3. Runs Doxygen
# Run from the repo root:  pwsh .doxygen/generate-doxygen-docs.ps1
param(
    [string]$GroupsFile  = ".doxygen/groups.dox",
    [string]$MainpageFile = ".doxygen/mainpage.md"
)

Push-Location (Join-Path $PSScriptRoot "..")

# ── 1. Sync navigation table ──────────────────────────────────────────
$pythonScript = @'
import re, sys

groups_file  = sys.argv[1]
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
'@

python3 -c $pythonScript $GroupsFile $MainpageFile

# ── 2. Extract version ────────────────────────────────────────────────
$env:LL_SDK_VERSION = (Get-Content "package.json" | ConvertFrom-Json).version
Write-Host "generate-doxygen-docs: version=$($env:LL_SDK_VERSION)"

# ── 3. Run Doxygen ────────────────────────────────────────────────────
doxygen .doxygen/Doxyfile

$env:LL_SDK_VERSION = $null
Pop-Location
