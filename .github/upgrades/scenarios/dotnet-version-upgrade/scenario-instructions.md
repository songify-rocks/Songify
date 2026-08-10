# .NET 10 Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0 (.NET 10 LTS)

## Source Control
- **Source Branch**: feature/dotnet-wpfui
- **Working Branch**: upgrade-dotnet-10
- **Commit Strategy**: Single Commit at End
- **Branch Sync**: Auto (Merge)

## Upgrade Options
- **Upgrade Strategy**: All-at-Once
- **Project Approach**: In-place
- **SDK-Style Conversion**: Yes
- **Package Management**: PackageReference

## Strategy
**Selected**: All-at-Once  
**Rationale**: Single-project solution enables comprehensive upgrade in one coordinated effort. WPF is fully supported in .NET 10, making in-place migration the most efficient approach.

### Execution Constraints
- Single atomic upgrade — all changes completed together before deployment
- Validate full solution build after each major phase (structure, packages, API fixes)
- Test comprehensively before deployment — no partial production releases
- Maintain .NET Framework version as rollback option until .NET 10 version fully validated
- Commit strategy updated to Single Commit at End (best fit for All-at-Once atomic upgrades)

## Build Tool Decisions
- **Songify Slim.csproj**: msbuild.exe (WPF project with XAML, requires Windows SDK targets from Visual Studio MSBuild)
