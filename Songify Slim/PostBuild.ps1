param(
    [string]$PublishDir = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $candidates = @(
        "$PSScriptRoot\bin\Release\net10.0-windows10.0.19041.0\publish\win-x86",
        "$PSScriptRoot\bin\Release\net10.0-windows10.0.19041.0\publish\win-x64",
        "$PSScriptRoot\bin\Release\app.publish"
    )
    $PublishDir = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not [System.IO.Path]::IsPathRooted($PublishDir)) {
    $PublishDir = Join-Path $PSScriptRoot $PublishDir
}

$PublishDir = [System.IO.Path]::GetFullPath($PublishDir)
$zipFilePath = Join-Path $PublishDir "Songify.zip"
$exeFilePath = Join-Path $PublishDir "Songify.exe"

if (-not (Test-Path -Path $PublishDir)) {
    Write-Host "Publish directory not found, skipping package: $PublishDir"
    exit 0
}

if (-not (Test-Path -Path $exeFilePath)) {
    Write-Error "Executable not found: $exeFilePath"
    exit 1
}

# ClickOnce/app.publish used to omit NuGet runtimes. Copy WebView2Loader.dll for
# win-x86/x64/arm64 so patch notes work on AnyCPU zips (same as the old PostBuild).
$loaderName = "WebView2Loader.dll"
$rids = @("win-x86", "win-x64", "win-arm64")
$runtimeSources = @(
    (Join-Path $PSScriptRoot "bin\Release\net10.0-windows10.0.19041.0\runtimes"),
    (Join-Path $PSScriptRoot "bin\Release\runtimes")
)
$nugetWebView2 = Join-Path $env:USERPROFILE ".nuget\packages\microsoft.web.webview2"
if (Test-Path $nugetWebView2) {
    $pkg = Get-ChildItem $nugetWebView2 -Directory | Sort-Object Name -Descending | Select-Object -First 1
    if ($pkg) {
        $runtimeSources += (Join-Path $pkg.FullName "runtimes")
    }
}

foreach ($rid in $rids) {
    $destDir = Join-Path $PublishDir "runtimes\$rid\native"
    $destFile = Join-Path $destDir $loaderName
    if (Test-Path $destFile) {
        continue
    }

    $src = $runtimeSources |
        ForEach-Object { Join-Path $_ "$rid\native\$loaderName" } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1

    if (-not $src) {
        continue
    }

    New-Item -ItemType Directory -Force -Path $destDir | Out-Null
    Copy-Item -Path $src -Destination $destFile -Force
    Write-Host "Copied $rid $loaderName"
}

$itemsToZip = Get-ChildItem -Path $PublishDir -Force | Where-Object {
    $_.Name -ne "Songify.zip" -and
    $_.Extension -ne ".zip" -and
    $_.Name -ne "checksums.txt" -and
    $_.Name -notlike "update*.xml"
}

if (-not $itemsToZip) {
    Write-Error "Nothing to zip in $PublishDir"
    exit 1
}

if (Test-Path -Path $zipFilePath) {
    Remove-Item -Path $zipFilePath -Force
}

try {
    Compress-Archive -Path $itemsToZip.FullName -DestinationPath $zipFilePath -Force -ErrorAction Stop
}
catch {
    Write-Error "Failed to create zip archive: $zipFilePath. $($_.Exception.Message)"
    exit 1
}

if (-not (Test-Path -Path $zipFilePath)) {
    Write-Error "Zip file was not created: $zipFilePath"
    exit 1
}

$zipMD5 = (Get-FileHash -Algorithm MD5 -Path $zipFilePath).Hash
$zipSHA1 = (Get-FileHash -Algorithm SHA1 -Path $zipFilePath).Hash
$zipSHA256 = (Get-FileHash -Algorithm SHA256 -Path $zipFilePath).Hash

$exeMD5 = (Get-FileHash -Algorithm MD5 -Path $exeFilePath).Hash
$exeSHA1 = (Get-FileHash -Algorithm SHA1 -Path $exeFilePath).Hash
$exeSHA256 = (Get-FileHash -Algorithm SHA256 -Path $exeFilePath).Hash

$checksumFilePath = Join-Path $PublishDir "checksums.txt"
@"
Songify.zip:
MD5:    $zipMD5
SHA1:   $zipSHA1
SHA256: $zipSHA256

Songify.exe:
MD5:    $exeMD5
SHA1:   $exeSHA1
SHA256: $exeSHA256
"@ | Set-Content -Path $checksumFilePath -Encoding utf8

$exeVersion = (Get-Item $exeFilePath).VersionInfo.ProductVersion
$updateXmlPath = Join-Path $PublishDir "update-beta.xml"
@"
<?xml version="1.0" encoding="UTF-8"?>
<item>
    <version>$exeVersion</version>
    <url>https://songify.rocks/Songify.zip</url>
    <checksum algorithm="MD5">$zipMD5</checksum>
    <mandatory>true</mandatory>
</item>
"@ | Set-Content -Path $updateXmlPath -Encoding utf8

Write-Host "Packaged $zipFilePath"
Write-Host "Wrote $checksumFilePath"
Write-Host "Wrote $updateXmlPath"
