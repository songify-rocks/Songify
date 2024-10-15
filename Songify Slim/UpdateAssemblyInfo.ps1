# Set the fixed major and minor versions
$majorMinorVersion = "1.6"

# Correct path to AssemblyInfo.cs, using the Properties folder under the current script's directory
$assemblyInfoPath = "$PSScriptRoot\Properties\AssemblyInfo.cs"
$tempFilePath = "$PSScriptRoot\Properties\AssemblyInfo.tmp"

# Generate a build number based on the day of the year and a revision number based on the current time (HHmm)
$buildNumber = [int](Get-Date).DayOfYear
$revisionNumber = (Get-Date -Format "HHmm")

# Create the full version string by combining the fixed version and the generated build/revision
$fullVersion = "$majorMinorVersion.$buildNumber.$revisionNumber"

# Read the content, replace version information, and save to a temporary file
Get-Content $assemblyInfoPath | 
    ForEach-Object {
        $_ -replace '\[assembly: AssemblyVersion\(".*"\)\]', "[assembly: AssemblyVersion(`"$fullVersion`")]"
    } | ForEach-Object {
        $_ -replace '\[assembly: AssemblyFileVersion\(".*"\)\]', "[assembly: AssemblyFileVersion(`"$fullVersion`")]"
    } | Set-Content $tempFilePath

# Replace original file with the temporary file
Move-Item -Force $tempFilePath $assemblyInfoPath
