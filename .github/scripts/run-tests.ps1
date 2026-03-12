#Requires -Version 5.0
<#
.SYNOPSIS
    Runs LootLocker SDK PlayMode tests against a local Unity installation.

.DESCRIPTION
    Reads unity-dev-settings.json from the repo root, creates (or reuses) a temporary
    Unity test project that references the SDK as a local package, then runs Unity's
    test runner in batch mode and reports results.

    Use -TestCategory to filter by NUnit category (e.g. LootLockerCIFast, LootLockerDebug).
    Use -TestFilter to filter by full or partial test name / regex.
    Both can be combined; Unity applies them with AND logic.

    See .github/instructions/testing.md for more details.

.PARAMETER TestCategory
    NUnit category to run (e.g. "LootLockerCIFast", "LootLockerCI", "LootLockerDebug").
    If omitted, all tests in the LootLocker category are run.

.PARAMETER TestFilter
    Full or partial test name, class name, or NUnit filter string
    (e.g. "LeaderboardTest", "Leaderboard_ListTopTen_Succeeds").
    Supports Unity's "-testFilter" regex syntax.

.PARAMETER TestMode
    Unity test platform - PlayMode (default) or EditMode.

.PARAMETER Force
    If specified, always recreates the temporary test project even if it already exists.

.EXAMPLE
    # Fast subset (good default before a commit)
    .github\scripts\run-tests.ps1 -TestCategory LootLockerCIFast

    # All tests for a specific feature
    .github\scripts\run-tests.ps1 -TestFilter "LeaderboardTest"

    # Debug category (temporary tests only)
    .github\scripts\run-tests.ps1 -TestCategory LootLockerDebug

    # Combine category and name filter
    .github\scripts\run-tests.ps1 -TestCategory LootLockerCI -TestFilter "GuestSession"
#>

param(
    [string] $TestCategory = "",
    [string] $TestFilter   = "",
    [ValidateSet("PlayMode", "EditMode")]
    [string] $TestMode     = "PlayMode",
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

$RepoRoot     = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$SettingsFile = Join-Path $RepoRoot "unity-dev-settings.json"
$TempProject  = Join-Path $RepoRoot "Temp~\TestProject"
$ResultsFile  = Join-Path $TempProject "TestResults.xml"
$LogFile      = Join-Path $TempProject "test-run.log"

function Write-Step { param([string]$Msg) Write-Host $Msg }
function Write-Ok   { param([string]$Msg) Write-Host $Msg -ForegroundColor Green }
function Write-Fail { param([string]$Msg) Write-Host $Msg -ForegroundColor Red }
function Write-Warn { param([string]$Msg) Write-Host $Msg -ForegroundColor Yellow }

Write-Step "========================================="
Write-Step " LootLocker SDK - Test Runner"
Write-Step "========================================="
Write-Step ""

# ---------------------------------------------------------------------------
# 1. Load settings
# ---------------------------------------------------------------------------
if (-not (Test-Path $SettingsFile)) {
    Write-Warn "SETUP REQUIRED: 'unity-dev-settings.json' not found at repo root."
    Write-Step "  Create unity-dev-settings.json with your Unity path."
    Write-Step "  See .github/instructions/verification.md for the required format."
    exit 1
}

$Settings = Get-Content $SettingsFile -Raw | ConvertFrom-Json
$UnityExe  = $Settings.unity_executable

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    Write-Fail "ERROR: 'unity_executable' is empty in unity-dev-settings.json."
    exit 1
}
if (-not (Test-Path $UnityExe)) {
    Write-Fail "ERROR: Unity executable not found: $UnityExe"
    exit 1
}

# ---------------------------------------------------------------------------
# 2. Create / refresh the test project if needed
# ---------------------------------------------------------------------------
function Initialize-TestProject {
    Write-Step "Creating test project at Temp~/TestProject ..."

    if (Test-Path $TempProject) {
        # Clear read-only attributes Unity sets on Library files before deleting
        Get-ChildItem $TempProject -Recurse -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Attributes -band [IO.FileAttributes]::ReadOnly } |
            ForEach-Object { $_.Attributes = $_.Attributes -band (-bnot [IO.FileAttributes]::ReadOnly) }
        Remove-Item $TempProject -Recurse -Force -ErrorAction SilentlyContinue
    }
    New-Item -ItemType Directory -Path (Join-Path $TempProject "Assets")          -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $TempProject "Packages")        -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $TempProject "ProjectSettings")  -Force | Out-Null

    $SdkRef = $RepoRoot -replace '\\', '/'
    $nl = [char]10
    # Include "testables" so Unity imports the SDK's test assemblies (UNITY_INCLUDE_TESTS)
    $manifestContent = '{' + $nl + '  "dependencies": {' + $nl + '    "com.lootlocker.lootlockersdk": "file:' + $SdkRef + '"' + $nl + '  },' + $nl + '  "testables": [' + $nl + '    "com.lootlocker.lootlockersdk"' + $nl + '  ]' + $nl + '}'
    [IO.File]::WriteAllText((Join-Path $TempProject 'Packages\manifest.json'), $manifestContent)

    $psContent = '%YAML 1.1' + $nl + '%TAG !u! tag:unity3d.com,2011:' + $nl + '--- !u!129 &1' + $nl + 'PlayerSettings:' + $nl + '  companyName: LootLockerSDKVerification' + $nl + '  productName: LootLockerSDKVerification'
    [IO.File]::WriteAllText((Join-Path $TempProject 'ProjectSettings\ProjectSettings.asset'), $psContent)
}

if ($Force -or -not (Test-Path (Join-Path $TempProject "Packages\manifest.json"))) {
    Initialize-TestProject
} else {
    Write-Step "Reusing existing test project at Temp~/TestProject (use -Force to recreate)."
}

# ---------------------------------------------------------------------------
# 3. Build Unity arguments
# ---------------------------------------------------------------------------
$UnityArgs = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $TempProject,
    "-logFile", $LogFile,
    "-runTests",
    "-testPlatform", $TestMode,
    "-testResults", $ResultsFile
)

if (-not [string]::IsNullOrWhiteSpace($TestCategory)) {
    $UnityArgs += @("-testCategory", $TestCategory)
}
if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    $UnityArgs += @("-testFilter", $TestFilter)
}

# ---------------------------------------------------------------------------
# 4. Run Unity test runner
# ---------------------------------------------------------------------------
Write-Step ""
Write-Step "Unity:    $UnityExe"
Write-Step "Project:  $TempProject"
Write-Step "Mode:     $TestMode"
if (-not [string]::IsNullOrWhiteSpace($TestCategory)) { Write-Step "Category: $TestCategory" }
if (-not [string]::IsNullOrWhiteSpace($TestFilter))   { Write-Step "Filter:   $TestFilter" }
Write-Step "Results:  $ResultsFile"
Write-Step "Log:      $LogFile"
Write-Step ""

if (-not (Test-Path (Split-Path $LogFile))) { New-Item -ItemType Directory -Path (Split-Path $LogFile) -Force | Out-Null }
if (Test-Path $LogFile)    { Remove-Item $LogFile    -Force }
if (Test-Path $ResultsFile) { Remove-Item $ResultsFile -Force }

# ---------------------------------------------------------------------------
# 4a. Warm-up pass - open the project and quit so Unity fully builds the
#     Library before the test run. Without this, PlayMode tests on a fresh
#     project can hit "referenced script (Unknown)" during scene load because
#     the backup scene is written before script compilation completes.
#     Uses Start-Process with a timeout so a hung Unity does not block forever.
# ---------------------------------------------------------------------------
$WarmupMarker = Join-Path $TempProject "Library\ScriptAssemblies"
if (-not (Test-Path $WarmupMarker)) {
    Write-Step "Warming up project (first-time Library build, up to 10 min) ..."
    $WarmupLog = Join-Path $TempProject "warmup.log"
    $warmupProc = Start-Process -FilePath $UnityExe `
        -ArgumentList @("-batchmode", "-nographics", "-projectPath", $TempProject, "-logFile", $WarmupLog, "-quit") `
        -PassThru -NoNewWindow
    $warmupProc.WaitForExit(600000) | Out-Null   # wait up to 10 minutes
    if (-not $warmupProc.HasExited) {
        Write-Warn "Warm-up did not finish in time -- killing Unity and proceeding anyway."
        $warmupProc | Stop-Process -Force -ErrorAction SilentlyContinue
    }
    Write-Step "Warm-up complete."
}

# Use Start-Process + WaitForExit so the shell blocks regardless of whether
# Unity.exe is a GUI-subsystem or console-subsystem executable.
$unityProc = Start-Process -FilePath $UnityExe -ArgumentList $UnityArgs -PassThru -NoNewWindow
if ($null -ne $unityProc) {
    $unityProc.WaitForExit()
    $script:unityExitCode = $unityProc.ExitCode
} else {
    $script:unityExitCode = 1
}

# ---------------------------------------------------------------------------
# 5. Wait for Unity to finish writing the results file
#    (short retry loop to handle any OS file-flush delay after exit)
# ---------------------------------------------------------------------------
$resultsContent = ""
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Milliseconds 500
    if (-not (Test-Path $ResultsFile)) { continue }
    try {
        $stream = [System.IO.File]::Open($ResultsFile, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        $reader = [System.IO.StreamReader]::new($stream, $true)
        $resultsContent = $reader.ReadToEnd()
        $reader.Close(); $stream.Close()
        if ($resultsContent -match "</test-run>") { break }
    } catch { }
}

# ---------------------------------------------------------------------------
# 6. Parse and report results
# ---------------------------------------------------------------------------
Write-Step ""
Write-Step "--- Test results -----------------------------------------"

if ([string]::IsNullOrWhiteSpace($resultsContent)) {
    Write-Fail "No test results file found. Unity may have crashed or timed out."
    Write-Step "Check the log: $LogFile"
    exit 1
}

# Parse NUnit XML summary attributes from the <test-run> element
$totalMatch   = [regex]::Match($resultsContent, 'total="(\d+)"')
$passedMatch  = [regex]::Match($resultsContent, 'passed="(\d+)"')
$failedMatch  = [regex]::Match($resultsContent, 'failed="(\d+)"')
$skippedMatch = [regex]::Match($resultsContent, 'skipped="(\d+)"')

$total   = if ($totalMatch.Success)   { $totalMatch.Groups[1].Value }   else { "?" }
$passed  = if ($passedMatch.Success)  { $passedMatch.Groups[1].Value }  else { "?" }
$failed  = if ($failedMatch.Success)  { $failedMatch.Groups[1].Value }  else { "?" }
$skipped = if ($skippedMatch.Success) { $skippedMatch.Groups[1].Value } else { "?" }

Write-Step "  Total:   $total"
Write-Step "  Passed:  $passed"
Write-Step "  Failed:  $failed"
Write-Step "  Skipped: $skipped"

# Print individual failure messages for quick diagnosis
if ($failed -ne "0" -and $failed -ne "?") {
    Write-Step ""
    Write-Step "--- Failed tests -----------------------------------------"
          $failureMatches = [regex]::Matches($resultsContent, '<test-case\s[^>]*?\sname="([^"]+)"[^>]*?\sresult="Failed"[^>]*?>[\s\S]*?<message><!\[CDATA\[([\s\S]*?)\]\]></message>')
    foreach ($m in $failureMatches) {
        Write-Fail "FAIL: $($m.Groups[1].Value)"
        $msg = $m.Groups[2].Value.Trim()
        if ($msg.Length -gt 0) {
            $msg -split "`n" | Select-Object -First 5 | ForEach-Object { Write-Step "       $_" }
        }
    }
}

Write-Step "----------------------------------------------------------"
Write-Step ""

if ($failed -eq "0" -or $failed -eq "?") {
    if ($script:unityExitCode -eq 0) {
        Write-Ok "ALL TESTS PASSED"
        exit 0
    } else {
        Write-Warn "Unity exited with code $($script:unityExitCode) but no test failures found."
        Write-Warn "Check the log for non-test errors: $LogFile"
        exit $script:unityExitCode
    }
} else {
    Write-Fail "TESTS FAILED ($failed failure(s))"
    Write-Step "Full results: $ResultsFile"
    Write-Step "Full log:     $LogFile"
    exit 1
}
