param(
    [Parameter(Mandatory = $true)][string]$SPTPath,
    [string]$DotNet = 'dotnet',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'releases')
)

$ErrorActionPreference = 'Stop'
$SPTPath = (Resolve-Path -LiteralPath $SPTPath).Path
$oldSptPath = $env:SPTPATH
try {
    $env:SPTPATH = $SPTPath
    & $DotNet build (Join-Path $PSScriptRoot 'HideoutAutomation.Server\HideoutAutomation.Server.csproj') -c Release "-p:SPTPath=$SPTPath" --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    # An explicit manifest keeps game dependencies, configs and stale files out of the package.
    $manifest = @(
        'BepInEx\plugins\HideoutAutomation\HideoutAutomation.dll',
        'SPT_Runtime\user\mods\HideoutAutomation.Server\HideoutAutomation.Server.dll',
        'SPT_Runtime\user\mods\HideoutAutomation.Server\HideoutAutomation.Server.deps.json'
    )
    foreach ($relativePath in $manifest) {
        if (!(Test-Path -LiteralPath (Join-Path "$PSScriptRoot\dist" $relativePath) -PathType Leaf)) {
            throw "Required distribution file missing: $relativePath"
        }
    }
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    $OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
    $zipPath = Join-Path $OutputDirectory 'HideoutAutomation-1.3.2-SPT-4.1.zip'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipStream = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::Create)
    $archive = New-Object System.IO.Compression.ZipArchive($zipStream, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($relativePath in $manifest) {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path "$PSScriptRoot\dist" $relativePath), $relativePath.Replace('\','/')) | Out-Null
        }
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $PSScriptRoot 'README.md'), 'HideoutAutomation-README.md') | Out-Null
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $PSScriptRoot 'LICENSE'), 'HideoutAutomation-LICENSE.txt') | Out-Null
    }
    finally {
        $archive.Dispose()
        $zipStream.Dispose()
    }
    Write-Output "Package created: $zipPath"
}
finally {
    $env:SPTPATH = $oldSptPath
}
