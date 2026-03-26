
function UnityVersionExistsInDockerHub {
  [CmdletBinding()]
  param([string] $Version)

    $Repository = "unityci/editor"
    $Tag = "ubuntu-${Version}f1-linux-il2cpp-3.2.1"
  Try {
    $URL = "https://hub.docker.com/layers/$Repository/$Tag/"
    Invoke-RestMethod $URL | Out-Null
  } Catch {
    return $false
  } # Assume image does not exist on erroneous response

  return $?
}

function FindMaxUnityVersionInDockerHub {
  [CmdletBinding()]
  param([string] $MajorVersion)

    $MinorVersion = 1
    if ($MajorVersion -eq "2019") {
      $MinorVersion = 3
    }
    $PatchVersion = 1

    # Find max minor version
    for (; $MinorVersion -le 99; $MinorVersion++) {
      $Version = "$MajorVersion.$MinorVersion.$PatchVersion"
      if (-not (UnityVersionExistsInDockerHub -Version $Version)) {
        break
      }
    }
    
    # Use the last existing minor version (subtract 1 since loop broke on first non-existing)
    $MinorVersion = $MinorVersion - 1
    
    # Handle case where no minor versions exist
    if ($MinorVersion -eq 0) {
      return $null
    }
    
    # Binary search for max patch version
    $Low = 1
    $High = 99
    while ($Low -le $High) {
      $Mid = [math]::Floor(($Low + $High) / 2)
      $Version = "$MajorVersion.$MinorVersion.$Mid"
      if (UnityVersionExistsInDockerHub -Version $Version) {
        $Low = $Mid + 1
      } else {
        $High = $Mid - 1
      }
    }
    return "$MajorVersion.$MinorVersion.$High"
}

$MajorVersions = @("2019", "2020", "2021", "2022", "2023", "6000")
$MaxVersions = @{}
foreach ($MajorVersion in $MajorVersions) {
    $MaxVersion = FindMaxUnityVersionInDockerHub -MajorVersion $MajorVersion
    $MaxVersions[$MajorVersion] = $MaxVersion
}

$OutputString = "["
foreach ($MajorVersion in $MajorVersions) {
    $OutputString += "`"$($MaxVersions[$MajorVersion])f1`", "
}
$OutputString = $OutputString.TrimEnd(", ")
$OutputString += "]"

Write-Output "$OutputString"