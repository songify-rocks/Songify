<#
.SYNOPSIS
    Copies docs/wiki into a local clone of github.com/songify-rocks/Songify.wiki.git

.DESCRIPTION
    The GitHub wiki is not part of the main repository. After copying, cd into the wiki
    clone and: git add -A; git commit -m "docs: sync wiki"; git push

.PARAMETER WikiClonePath
    Path to an existing clone of https://github.com/songify-rocks/Songify.wiki.git

.EXAMPLE
    .\scripts\Sync-Wiki.ps1 -WikiClonePath C:\src\Songify.wiki
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $WikiClonePath
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$src = Join-Path $repoRoot "docs\wiki"
if (-not (Test-Path $src)) {
    Write-Error "Missing docs/wiki at: $src"
}
if (-not (Test-Path $WikiClonePath)) {
    Write-Error "Wiki clone path does not exist: $WikiClonePath"
}
$gitDir = Join-Path $WikiClonePath ".git"
if (-not (Test-Path $gitDir)) {
    Write-Error "Not a git repository: $WikiClonePath"
}

Get-ChildItem -Path $src -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $WikiClonePath -Force
}

Write-Host "Copied wiki files from:"
Write-Host "  $src"
Write-Host "to:"
Write-Host "  $WikiClonePath"
Write-Host ""
Write-Host "Next: cd `"$WikiClonePath`"; git add -A; git status; git commit -m ""docs: sync wiki from docs/wiki""; git push"
