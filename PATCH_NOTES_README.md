# Patch Notes Generation

This directory contains tools for automatically generating patch notes from git commit history.

## Overview

The patch notes generator extracts commits since the last version tag and organizes them into categorized release notes.

## Tools

### 1. PowerShell Script (`GeneratePatchNotes.ps1`)

A standalone PowerShell script that generates patch notes locally.

#### Usage

```powershell
# Generate patch notes with default settings
.\GeneratePatchNotes.ps1

# Specify custom output file
.\GeneratePatchNotes.ps1 -OutputFile "RELEASE_NOTES.md"

# Generate plain text format
.\GeneratePatchNotes.ps1 -Format "text"
```

#### Parameters

- **OutputFile** (optional): Path to save the generated patch notes. Default: `PATCHNOTES.md`
- **Format** (optional): Output format - `markdown` or `text`. Default: `markdown`

#### Features

- Automatically detects the latest version tag
- Categorizes commits by type (feat, fix, refactor, etc.)
- Supports conventional commit message format
- Generates both categorized and full commit history sections
- Works with grafted/shallow repositories

### 2. GitHub Actions Workflow (`.github/workflows/generate-patch-notes.yml`)

An automated workflow for generating patch notes in CI/CD pipelines.

#### Usage

1. Navigate to the **Actions** tab in your GitHub repository
2. Select **Generate Patch Notes** workflow
3. Click **Run workflow**
4. Configure options:
   - **Output file name**: Specify the output filename (default: `PATCHNOTES.md`)
   - **Create draft release**: Optionally create a draft GitHub release with the notes

#### Features

- Runs on-demand via workflow dispatch
- Generates patch notes as a downloadable artifact
- Optionally creates draft GitHub releases
- Supports custom output filenames

## Commit Message Format

For best results, use conventional commit messages:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Supported Types

| Type | Category | Description |
|------|----------|-------------|
| `feat` | Features | New features |
| `fix` | Bug Fixes | Bug fixes |
| `refactor` | Refactoring | Code refactoring |
| `perf` | Performance | Performance improvements |
| `docs` | Documentation | Documentation changes |
| `style` | Styling | Code style changes |
| `test` | Tests | Test additions/changes |
| `build` | Build | Build system changes |
| `ci` | CI/CD | CI/CD configuration changes |
| `chore` | Chores | Maintenance tasks |
| `revert` | Reverts | Revert previous commits |

### Examples

```bash
feat(spotify): add playlist caching support
fix(ui): resolve crash on null song data
refactor(auth): simplify token management
docs(readme): update installation instructions
```

## Output Format

The generated patch notes include:

1. **Header**: Version info and date
2. **Categorized Changes**: Commits organized by type
3. **Full Commit History**: Complete list of all commits with hashes

### Example Output

```markdown
# Patch Notes

**Changes since:** v1.7.2.0
**Date:** 2026-02-11
**Commits:** 5

---
## Features

- Add playlist caching support `[abc1234]`
- Implement auto-refresh for tokens `[def5678]`

## Bug Fixes

- Resolve crash on null song data `[ghi9012]`

---

## Full Commit History
- `abc1234` feat(spotify): add playlist caching support
- `def5678` feat(auth): implement auto-refresh for tokens
- `ghi9012` fix(ui): resolve crash on null song data
```

## Integration with Release Process

### Manual Release

1. Run the PowerShell script locally:
   ```powershell
   .\GeneratePatchNotes.ps1 -OutputFile "RELEASE_NOTES.md"
   ```

2. Review and edit the generated notes

3. Copy the content to your GitHub release description

### Automated Release (GitHub Actions)

1. Trigger the workflow from GitHub Actions UI
2. Download the generated artifact
3. Use the content for your release notes

### Future Enhancement

Consider integrating with the existing `PostBuild.ps1` script to automatically generate patch notes during build process.

## Troubleshooting

### "No tags found in repository"

**Cause**: The repository doesn't have any git tags.

**Solution**: Create at least one tag:
```bash
git tag v1.0.0
git push origin v1.0.0
```

### "No commits found since tag"

**Cause**: No commits exist between the latest tag and HEAD.

**Solution**: This is normal if you're on the exact commit that's tagged. Make some commits first.

### Merge commits appearing in output

**Cause**: The script filters most merge commits, but some may slip through.

**Solution**: Edit the generated file to remove unwanted merge commits.

## Contributing

When adding new features to the patch notes generator:

1. Update both the PowerShell script and GitHub Actions workflow
2. Test with various repository states (grafted, shallow, normal)
3. Update this documentation
4. Add example output if the format changes

## License

This tool is part of the Songify project and follows the same license.
