#Requires -Version 5.0
<#
.SYNOPSIS
    Verifies that the LootLocker Unity SDK compiles without errors.

.DESCRIPTION
    Reads unity-dev-settings.json from the repo root, optionally creates a temporary
    Unity project referencing the SDK, then runs Unity in batch mode to check
    compilation.

    See .github/instructions/verification.md for setup instructions.
#>

$ErrorActionPreference = 'Stop'

$RepoRoot     = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$SettingsFile = Join-Path $RepoRoot "unity-dev-settings.json"
$TempProject  = Join-Path $RepoRoot "Temp~\VerificationProject"
$LogFile      = Join-Path $TempProject "compilation.log"

function Write-Step  { param([string]$Msg) Write-Host $Msg }
function Write-Ok    { param([string]$Msg) Write-Host $Msg -ForegroundColor Green }
function Write-Fail  { param([string]$Msg) Write-Host $Msg -ForegroundColor Red }
function Write-Warn  { param([string]$Msg) Write-Host $Msg -ForegroundColor Yellow }

Write-Step "========================================="
Write-Step " LootLocker SDK - Compilation Check"
Write-Step "========================================="
Write-Step ""

# ---------------------------------------------------------------------------
# 1. Load settings
# ---------------------------------------------------------------------------
if (-not (Test-Path $SettingsFile)) {
    Write-Warn "SETUP REQUIRED: 'unity-dev-settings.json' not found at repo root."
    Write-Step "  Create unity-dev-settings.json with your Unity path."
    Write-Step "  See .github/instructions/verification.md for the required format and examples."
    exit 1
}

$Settings      = Get-Content $SettingsFile -Raw | ConvertFrom-Json
$UnityExe      = $Settings.unity_executable
$CustomProject = $Settings.test_project_path

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    Write-Fail "ERROR: 'unity_executable' is empty in unity-dev-settings.json."
    exit 1
}
if (-not (Test-Path $UnityExe)) {
    Write-Fail "ERROR: Unity executable not found: $UnityExe"
    exit 1
}

# ---------------------------------------------------------------------------
# 2. Helper: create / refresh the temporary verification project
# ---------------------------------------------------------------------------
function Initialize-TempProject {
    Write-Step "Creating temporary verification project at Temp~/VerificationProject ..."

    if (Test-Path $TempProject) { Remove-Item $TempProject -Recurse -Force }
    New-Item -ItemType Directory -Path (Join-Path $TempProject "Assets")          -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $TempProject "Packages")        -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $TempProject "ProjectSettings")  -Force | Out-Null

    $SdkRef = $RepoRoot -replace '\\', '/'
    $nl = [char]10
    $manifestContent = '{' + $nl + '  "dependencies": {' + $nl + '    "com.lootlocker.lootlockersdk": "file:' + $SdkRef + '"' + $nl + '  }' + $nl + '}'
    [IO.File]::WriteAllText((Join-Path $TempProject 'Packages\manifest.json'), $manifestContent)

    $psContent = '%YAML 1.1' + $nl + '%TAG !u! tag:unity3d.com,2011:' + $nl + '--- !u!129 &1' + $nl + 'PlayerSettings:' + $nl + '  companyName: LootLockerSDKVerification' + $nl + '  productName: LootLockerSDKVerification'
    [IO.File]::WriteAllText((Join-Path $TempProject 'ProjectSettings\ProjectSettings.asset'), $psContent)

    $SamplesPath = Join-Path $RepoRoot "Samples~\LootLockerExamples"
    if (Test-Path $SamplesPath) {
        Copy-Item $SamplesPath (Join-Path $TempProject "Assets\") -Recurse -Force
    }
}

# ---------------------------------------------------------------------------
# 3. Determine project path
# ---------------------------------------------------------------------------
if (-not [string]::IsNullOrWhiteSpace($CustomProject) -and (Test-Path $CustomProject)) {
    $ProjectPath = (Resolve-Path $CustomProject).Path
    Write-Step "Using custom project: $ProjectPath"

    # Delete only the LootLocker compiled output artifacts so Tundra is forced to
    # recompile the SDK from source. Deleting the entire Bee folder crashes Unity;
    # deleting only outputs is safe — Tundra detects missing outputs and rebuilds them.
    Write-Step "Removing cached LootLocker assemblies to force recompilation..."
    $artifactsPath = Join-Path $ProjectPath "Library\Bee\artifacts"
    Get-ChildItem $artifactsPath -Recurse -Filter "*lootlocker*" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    $scriptAssemblies = Join-Path $ProjectPath "Library\ScriptAssemblies"
    Get-ChildItem $scriptAssemblies -Filter "*lootlocker*" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
} else {
    Initialize-TempProject
    $ProjectPath = $TempProject
}

# ---------------------------------------------------------------------------
# 4. Run Unity in batch mode
# ---------------------------------------------------------------------------
function Invoke-UnityCompile {
    param([string]$ProjectDir)
    Write-Step ""
    Write-Step "Unity:   $UnityExe"
    Write-Step "Project: $ProjectDir"
    Write-Step "Log:     $LogFile"
    Write-Step ""

    # Ensure log directory exists; remove any stale log from a previous run.
    if (-not (Test-Path (Split-Path $LogFile))) { New-Item -ItemType Directory -Path (Split-Path $LogFile) -Force | Out-Null }
    if (Test-Path $LogFile) { Remove-Item $LogFile -Force }

    $prevEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & $UnityExe -batchmode -nographics -projectPath $ProjectDir -logFile $LogFile -quit
    $script:unityExitCode = if ($LASTEXITCODE -ne $null) { $LASTEXITCODE } else { 1 }
    $ErrorActionPreference = $prevEAP
}

$script:unityExitCode = 1
Invoke-UnityCompile $ProjectPath

# Wait for Unity to finish writing the log. Unity child processes (e.g. LicensingClient)
# may hold the file open after the main process exits. Poll until the log contains
# Unity's end-of-session marker. A full first-time compile can take 60-90 seconds so we
# wait up to 3 minutes.  We also require the file size to be stable for 3 consecutive
# reads (1.5 s) before accepting the content, since LicensingClient may still be writing
# small amounts after the main process has exited.
$logContent = ""
$lastSize   = -1
$stableRuns = 0
for ($i = 0; $i -lt 360; $i++) {
    Start-Sleep -Milliseconds 500
    if (-not (Test-Path $LogFile)) { continue }
    try {
        $stream = [System.IO.File]::Open($LogFile, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        $reader = [System.IO.StreamReader]::new($stream, $true) # $true = detect encoding from BOM
        $logContent = $reader.ReadToEnd()
        $reader.Close(); $stream.Close()

        $currentSize = $logContent.Length
        if ($currentSize -eq $lastSize) { $stableRuns++ } else { $stableRuns = 0 }
        $lastSize = $currentSize

        # Stop once Unity's final line is present AND the file hasn't grown for 3 checks
        if (($logContent -match "Application will terminate|Exiting batchmode") -and $stableRuns -ge 3) { break }
    } catch { }
}

# If the custom project crashed on startup (Package Manager never ran — log is tiny with
# no compilation output), fall back to the temp project automatically.
if ($ProjectPath -ne $TempProject -and ($logContent.Length -lt 5000 -or $logContent -notmatch "Package Manager")) {
    Write-Warn "Custom project did not open correctly (startup crash). Falling back to temporary project."
    Write-Warn "To fix: open '$ProjectPath' in the Unity Editor once, then re-run."
    Initialize-TempProject
    $ProjectPath = $TempProject
    Invoke-UnityCompile $ProjectPath

    $lastSize = -1; $stableRuns = 0; $logContent = ""
    for ($i = 0; $i -lt 360; $i++) {
        Start-Sleep -Milliseconds 500
        if (-not (Test-Path $LogFile)) { continue }
        try {
            $stream = [System.IO.File]::Open($LogFile, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
            $reader = [System.IO.StreamReader]::new($stream, $true)
            $logContent = $reader.ReadToEnd()
            $reader.Close(); $stream.Close()
            $currentSize = $logContent.Length
            if ($currentSize -eq $lastSize) { $stableRuns++ } else { $stableRuns = 0 }
            $lastSize = $currentSize
            if (($logContent -match "Application will terminate|Exiting batchmode") -and $stableRuns -ge 3) { break }
        } catch { }
    }
}

$logLines = $logContent -split "`n"

# ---------------------------------------------------------------------------
# 5. Report results
# ---------------------------------------------------------------------------
Write-Step ""
Write-Step "--- Compilation result -------"

$compileErrors = $logLines | Select-String -Pattern "error CS\d+" | Select-String -NotMatch "Licensing::"
$tundraSuccess = ($logLines | Select-String -Pattern "Tundra build success").Count -gt 0
$tundraFailure = ($logLines | Select-String -Pattern "Tundra build failure|Tundra build failed").Count -gt 0

if ($compileErrors) {
    $compileErrors | ForEach-Object { Write-Host $_.Line }
}

Write-Step "-----------------------------------"
Write-Step ""

# Determine outcome from log content:
#   - Any "error CS####" line      => compilation failed
#   - "Tundra build success" found => compilation succeeded (Unity may still exit non-zero
#     due to unrelated project setup issues unrelated to the SDK)
#   - Neither found                => fall back to Unity exit code
if ($compileErrors.Count -gt 0) {
    Write-Fail "COMPILATION FAILED ($($compileErrors.Count) compiler error(s))"
    Write-Step "Full log: $LogFile"
    exit 1
}
elseif ($tundraSuccess) {
    Write-Ok "COMPILATION SUCCEEDED"
    if ($unityExitCode -ne 0) {
        Write-Warn "Note: Unity exited with code $unityExitCode after compilation (likely unrelated project setup - not an SDK issue)."
    }
}
elseif ($tundraFailure -or $unityExitCode -ne 0) {
    # Print any error/warning lines we can find to help diagnose the issue
    if ($logLines) {
        $logLines | Select-String -Pattern "error CS\d+|Scripts have compiler errors|error:" | Select-String -NotMatch "Licensing::" |
            ForEach-Object { Write-Host $_.Line }
    }
    $reason = if ($tundraFailure) { "Tundra build failed" } else { "exit code: $unityExitCode" }
    Write-Fail "COMPILATION FAILED ($reason)"
    Write-Step "Full log: $LogFile"
    exit 1
}
else {
    Write-Warn "UNKNOWN: could not determine compilation result from log. Check: $LogFile"
    exit 1
}
