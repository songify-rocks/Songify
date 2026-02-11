<#
.SYNOPSIS
    Generates patch notes from git commits since the last tag.

.DESCRIPTION
    This script retrieves commits since the last git tag and formats them into
    structured patch notes. It categorizes commits by type (feat, fix, refactor, etc.)
    and generates both a markdown file and console output.

.PARAMETER OutputFile
    Optional. The path to save the generated patch notes. Default: "PATCHNOTES.md"

.PARAMETER Format
    Optional. Output format: "markdown" or "text". Default: "markdown"

.EXAMPLE
    .\GeneratePatchNotes.ps1
    
.EXAMPLE
    .\GeneratePatchNotes.ps1 -OutputFile "release-notes.md"
#>

param(
    [string]$OutputFile = "PATCHNOTES.md",
    [string]$Format = "markdown"
)

# Function to get the latest tag
function Get-LatestTag {
    # Try to get the tag reachable from HEAD first
    $tag = git describe --tags --abbrev=0 2>$null
    
    # If that fails (e.g., in grafted/shallow repos), get the most recent version tag by date
    if (-not $tag) {
        # First try to get semantic version tags (v*.*.*)
        $tag = git for-each-ref --sort=-creatordate --format '%(refname:short)' refs/tags | 
               Where-Object { $_ -match '^v?\d+\.\d+' } | 
               Select-Object -First 1
        
        # If no version tags found, get any tag
        if (-not $tag) {
            $tag = git for-each-ref --sort=-creatordate --format '%(refname:short)' refs/tags | 
                   Select-Object -First 1
        }
    }
    
    if (-not $tag) {
        Write-Warning "No tags found in repository"
        return $null
    }
    return $tag
}

# Function to categorize commit by conventional commit type
function Get-CommitCategory {
    param([string]$message)
    
    if ($message -match "^feat(\(.+\))?:") { return "Features" }
    if ($message -match "^fix(\(.+\))?:") { return "Bug Fixes" }
    if ($message -match "^refactor(\(.+\))?:") { return "Refactoring" }
    if ($message -match "^perf(\(.+\))?:") { return "Performance" }
    if ($message -match "^docs(\(.+\))?:") { return "Documentation" }
    if ($message -match "^style(\(.+\))?:") { return "Styling" }
    if ($message -match "^test(\(.+\))?:") { return "Tests" }
    if ($message -match "^build(\(.+\))?:") { return "Build" }
    if ($message -match "^ci(\(.+\))?:") { return "CI/CD" }
    if ($message -match "^chore(\(.+\))?:") { return "Chores" }
    if ($message -match "^revert(\(.+\))?:") { return "Reverts" }
    if ($message -match "^merge") { return "Merges" }
    
    return "Other Changes"
}

# Function to format commit message
function Format-CommitMessage {
    param([string]$message)
    
    # Remove conventional commit prefix
    $formatted = $message -replace "^(feat|fix|refactor|perf|docs|style|test|build|ci|chore|revert)(\(.+\))?:\s*", ""
    
    # Capitalize first letter
    if ($formatted.Length -gt 0) {
        $formatted = $formatted.Substring(0,1).ToUpper() + $formatted.Substring(1)
    }
    
    return $formatted
}

# Main script
Write-Host "Generating patch notes..." -ForegroundColor Cyan

# Get the latest tag
$latestTag = Get-LatestTag
if (-not $latestTag) {
    Write-Error "Cannot generate patch notes without any tags"
    exit 1
}

Write-Host "Latest tag: $latestTag" -ForegroundColor Green

# Get commits since the last tag
$commitRange = "$latestTag..HEAD"
$commits = git log $commitRange --pretty=format:"%H|%s|%an|%ad" --date=short

if (-not $commits) {
    Write-Warning "No commits found since tag $latestTag"
    $content = @"
# Patch Notes

**Version:** Next Release (after $latestTag)
**Date:** $(Get-Date -Format "yyyy-MM-dd")

No changes since last release.
"@
} else {
    # Parse and categorize commits
    $categorized = @{}
    $commitCount = 0
    
    foreach ($commitLine in $commits) {
        if (-not $commitLine) { continue }
        
        $parts = $commitLine -split '\|'
        if ($parts.Length -lt 4) { continue }
        
        $hash = $parts[0].Substring(0, 7)
        $message = $parts[1]
        $author = $parts[2]
        $date = $parts[3]
        
        # Skip merge commits unless they have meaningful messages
        if ($message -match "^Merge (branch|pull request|remote-tracking)") {
            continue
        }
        
        $category = Get-CommitCategory -message $message
        $formattedMessage = Format-CommitMessage -message $message
        
        if (-not $categorized.ContainsKey($category)) {
            $categorized[$category] = @()
        }
        
        $categorized[$category] += @{
            Hash = $hash
            Message = $formattedMessage
            Author = $author
            Date = $date
        }
        
        $commitCount++
    }
    
    # Generate markdown content
    if ($Format -eq "markdown") {
        $content = @"
# Patch Notes

**Changes since:** $latestTag
**Date:** $(Get-Date -Format "yyyy-MM-dd")
**Commits:** $commitCount

---

"@
        
        # Define category order
        $categoryOrder = @(
            "Features",
            "Bug Fixes",
            "Performance",
            "Refactoring",
            "Documentation",
            "Styling",
            "Tests",
            "Build",
            "CI/CD",
            "Chores",
            "Reverts",
            "Other Changes"
        )
        
        foreach ($category in $categoryOrder) {
            if ($categorized.ContainsKey($category)) {
                $content += "## $category`n`n"
                
                foreach ($commit in $categorized[$category]) {
                    $content += "- $($commit.Message) ``[$($commit.Hash)]```n"
                }
                
                $content += "`n"
            }
        }
        
        # Add full commit history section
        $content += @"
---

## Full Commit History

"@
        foreach ($commitLine in $commits) {
            if (-not $commitLine) { continue }
            
            $parts = $commitLine -split '\|'
            if ($parts.Length -lt 4) { continue }
            
            $hash = $parts[0].Substring(0, 7)
            $message = $parts[1]
            
            # Skip merge commits
            if ($message -match "^Merge (branch|pull request|remote-tracking)") {
                continue
            }
            
            $content += "- ``$hash`` $message`n"
        }
        
    } else {
        # Plain text format
        $content = @"
Patch Notes
===========

Changes since: $latestTag
Date: $(Get-Date -Format "yyyy-MM-dd")
Commits: $commitCount

"@
        
        foreach ($category in $categoryOrder) {
            if ($categorized.ContainsKey($category)) {
                $content += "`n$category`n"
                $content += ("-" * $category.Length) + "`n`n"
                
                foreach ($commit in $categorized[$category]) {
                    $content += "  * $($commit.Message) [$($commit.Hash)]`n"
                }
            }
        }
    }
}

# Output to file
$content | Out-File -FilePath $OutputFile -Encoding UTF8
Write-Host "`nPatch notes generated successfully: $OutputFile" -ForegroundColor Green

# Display preview
Write-Host "`n=== PREVIEW ===" -ForegroundColor Cyan
Write-Host $content
Write-Host "=== END PREVIEW ===" -ForegroundColor Cyan

# Return success
exit 0
