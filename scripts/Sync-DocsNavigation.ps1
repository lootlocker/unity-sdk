#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Synchronizes the mainpage navigation table with groups.dox definitions.

.DESCRIPTION
    Extracts group definitions from groups.dox and generates the navigation
    table for mainpage.md, ensuring they stay synchronized.
#>

param(
    [string]$GroupsFile = ".doxygen/groups.dox",
    [string]$MainPageFile = ".doxygen/mainpage.md"
)

# Parse groups from groups.dox
function Get-DoxygenGroups {
    param([string]$FilePath)
    
    $content = Get-Content $FilePath -Raw
    $groups = @()
    
    # Extract each @defgroup definition with its brief description
    $pattern = '(?m)/// @defgroup\s+(\w+)\s+([^\r\n]+)(?:\r?\n/// @brief\s+([^\r\n]+))?'
    $matches = [regex]::Matches($content, $pattern)
    
    foreach ($match in $matches) {
        $groups += [PSCustomObject]@{
            GroupName = $match.Groups[1].Value
            DisplayName = $match.Groups[2].Value
            Brief = if ($match.Groups[3].Success) { $match.Groups[3].Value } else { "" }
        }
    }
    
    return $groups
}

# Generate navigation table markdown
function New-NavigationTable {
    param([array]$Groups)
    
    $table = @"
| Topic | What it covers |
|-------|---------------|
"@
    
    foreach ($group in $Groups) {
        $ref = "@ref $($group.GroupName) `"$($group.DisplayName)`""
        $description = if ($group.Brief) { $group.Brief } else { $group.DisplayName }
        $table += "`n| $ref | $description |"
    }
    
    return $table
}

# Main execution
try {
    Write-Host "Parsing groups from $GroupsFile..."
    $groups = Get-DoxygenGroups $GroupsFile
    Write-Host "Found $($groups.Count) groups"
    
    Write-Host "Generating navigation table..."
    $newTable = New-NavigationTable $groups
    
    Write-Host "Reading $MainPageFile..."
    $content = Get-Content $MainPageFile -Raw
    
    # Replace the navigation table section
    $startMarker = "| Topic | What it covers |"
    $endMarker = "---`r?`n"
    
    $pattern = "(?s)(\| Topic \| What it covers \|.*?)`r?`n---"
    if ($content -match $pattern) {
        $newContent = $content -replace $pattern, "$newTable`r`n---"
        Set-Content $MainPageFile $newContent -NoNewline
        Write-Host "✅ Navigation table updated in $MainPageFile" -ForegroundColor Green
    } else {
        Write-Warning "⚠️  Could not find navigation table section to replace"
    }
    
} catch {
    Write-Error "❌ Error: $_"
    exit 1
}