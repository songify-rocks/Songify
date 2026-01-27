# Define paths
$releaseDir = "$PSScriptRoot\bin\Release\app.publish"
$runtimeDir = "$PSScriptRoot\bin\Release\runtimes"
$zipFilePath = "$releaseDir\Songify.zip"
$exeFilePath = "$releaseDir\Songify.exe"

# Copy runtimes folder to the release directory
Copy-Item -Path "$runtimeDir" -Destination "$releaseDir" -Recurse

# Step 1: Zip the app.publish folder
if (Test-Path -Path $zipFilePath) {
    Remove-Item -Path $zipFilePath -Force  # Remove old zip file if it exists
}
    
Compress-Archive -Path "$releaseDir\*" -DestinationPath $zipFilePath

# Step 2: Calculate checksums for the zip file
$zipMD5 = (Get-FileHash -Algorithm MD5 -Path $zipFilePath).Hash
$zipSHA1 = (Get-FileHash -Algorithm SHA1 -Path $zipFilePath).Hash
$zipSHA256 = (Get-FileHash -Algorithm SHA256 -Path $zipFilePath).Hash

# Step 3: Calculate checksums for the exe file
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

# Step 5: Extract version from exe (using File Version Info)
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
