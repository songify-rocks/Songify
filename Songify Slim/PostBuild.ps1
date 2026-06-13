# Only run post-build tasks for Release configuration
if ($env:Configuration -ne "Release") {
    exit 0
}

# Define paths
$releaseDir = "$PSScriptRoot\bin\Release\app.publish"
$runtimeDir = "$PSScriptRoot\bin\Release\runtimes"
$zipFilePath = "$releaseDir\Songify.zip"
$exeFilePath = "$releaseDir\Songify.exe"

# Verify Release directory exists before proceeding
if (-not (Test-Path -Path $releaseDir)) {
    Write-Error "Release directory not found: $releaseDir"
    exit 1
}

# Copy runtimes folder to the release directory (if it exists)
if (Test-Path -Path $runtimeDir) {
    Copy-Item -Path "$runtimeDir" -Destination "$releaseDir" -Recurse -ErrorAction SilentlyContinue
}

# Step 1: Zip the app.publish folder
if (Test-Path -Path $zipFilePath) {
    Remove-Item -Path $zipFilePath -Force
}

if (-not (Compress-Archive -Path "$releaseDir\*" -DestinationPath $zipFilePath -ErrorAction SilentlyContinue)) {
    Write-Error "Failed to create zip archive: $zipFilePath"
    exit 1
}

# Verify zip file was created before proceeding
if (-not (Test-Path -Path $zipFilePath)) {
    Write-Error "Zip file was not created: $zipFilePath"
    exit 1
}

# Step 2: Calculate checksums for the zip file
$zipMD5 = (Get-FileHash -Algorithm MD5 -Path $zipFilePath).Hash
$zipSHA1 = (Get-FileHash -Algorithm SHA1 -Path $zipFilePath).Hash
$zipSHA256 = (Get-FileHash -Algorithm SHA256 -Path $zipFilePath).Hash

# Step 3: Calculate checksums for the exe file (if it exists)
if (-not (Test-Path -Path $exeFilePath)) {
    Write-Error "Executable not found: $exeFilePath"
    exit 1
}

$exeMD5 = (Get-FileHash -Algorithm MD5 -Path $exeFilePath).Hash
$exeSHA1 = (Get-FileHash -Algorithm SHA1 -Path $exeFilePath).Hash
$exeSHA256 = (Get-FileHash -Algorithm SHA256 -Path $exeFilePath).Hash

# Step 4: Output the checksums to a text file
$checksumFilePath = "$releaseDir\checksums.txt"

@"
Songify.zip:
MD5:    $zipMD5
SHA1:   $zipSHA1
SHA256: $zipSHA256

Songify.exe:
MD5:    $exeMD5
SHA1:   $exeSHA1
SHA256: $exeSHA256
"@ | Set-Content -Path $checksumFilePath

# Step 5: Extract version from exe
$exeVersion = (Get-Item $exeFilePath).VersionInfo.ProductVersion

# Step 6: Create the update-beta.xml file
$updateXmlPath = "$releaseDir\update-beta.xml"

@"
<?xml version="1.0" encoding="UTF-8"?>
<item>
    <version>$exeVersion</version>
    <url>https://songify.rocks/Songify.zip</url>
    <checksum algorithm="MD5">$zipMD5</checksum>
    <mandatory>true</mandatory>
</item>
"@ | Set-Content -Path $updateXmlPath
