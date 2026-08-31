# Songify 2.0.0 - Big Update Changelog

Comprehensive GitHub-style patch notes for the delta between the latest release on `master` and the current branch.

- Release baseline: `v1.8.13` (on `master`)
- Target branch: `feature/wpfui-shell`
- Compare range: `v1.8.13..3436cb3`
- Full compare: https://github.com/songify-rocks/Songify/compare/v1.8.13...feature/wpfui-shell

## Release Stats

- Total commits: **56**
  - Non-merge commits: **55**
  - Merge commits: **1**
- Diff size: **227 files changed**, **+225,976 / -16,726**
- Commit type breakdown (non-merge):
  - `feat`: 25
  - `refactor`: 11
  - `other`: 8
  - `style`: 4
  - `fix`: 4
  - `chore`: 2
  - `build`: 1
- Contributor(s): **Jan Blomacher (56 commits)**

## Major Highlights

- Complete modern WPF-UI shell rollout with page-based navigation and substantial UX restructuring.
- Setup/onboarding improvements: setup wizard, improved home checklist flow, and integrated help surface.
- Songify API authentication transition from AccessKey-based flow to token-centric bearer-auth workflows.
- Significant queue and history experience upgrades including history migration to YAML.
- New Twitch engagement features: `!skippoll`, `!playlist`, expanded command behavior, and UI support.
- Better personalization and accessibility: accent color controls, side menu style persistence, tooltips, and UI zoom.
- Ongoing stability improvements for queue behavior, region handling, and integration edge cases.

## Breaking Changes and Migration Notes

### API/Auth model changes
- AccessKey usage was removed from config and request payload flows.
- Token-based Songify API auth is now centralized and expected by the updated UI/setup path.

### UI architecture changes
- Legacy window structure has been replaced by a WPF-UI shell/page model.
- Several previous standalone windows and flows were removed or reworked into the new shell.

### History format migration
- Legacy `history.shr` handling was replaced by YAML-backed history storage.
- History migration is integrated into the application startup/update flow.

### Build/tooling modernization
- .NET 10 migration and supporting refactors were introduced.
- Fody weavers and related legacy build/package artifacts were removed.

## Added

### Shell, navigation, and user experience
- Modern WPF shell and MVVM navigation model.
- Setup wizard and onboarding checklist upgrades.
- Side menu style setting with persisted state.
- Help page additions and broader UX polish.
- Pop-out queue window and detachable console support.
- UI scale (zoom) support with PerMonitorV2 DPI handling.
- Accent color wheel and expanded theme customization support.
- Tooltips for NavigationView items for better accessibility.

### Twitch/song request capabilities
- `!skippoll` command and improved poll handling.
- `!playlist` command support with localization/UI integration.
- Minimum messages between song announcements setting.
- User-level controls for explicit song request handling.
- Twitch ignore/configuration quality-of-life improvements.

### Platform and integrations
- Songify Premium UI integration points.
- Songify API token setup/status flow in UI.
- Release channel setting in updater configuration.
- Beta patch notes display integration within patch notes window.
- Canvas animation support and always-on album cover download behavior.

### Localization and content
- Setup wizard and new feature strings localized across supported languages.
- Further full-UI localization/resource coverage improvements.

## Changed and Improved

- Settings UX and internal refresh logic reorganization.
- Bot response handling centralized into data-driven catalog structures.
- Dialog and status interaction patterns modernized (clickable status indicators).
- Configuration comparison/import experiences improved.
- General shell, dialog, and control layout polish.
- API/auth and UI code paths simplified and consolidated for maintainability.

## Fixes

- Queue region-lock and queue-logic handling improvements.
- Pear queue index lookup and error handling refinements.
- Twitch command rebind behavior after config load/import fixed.
- Spotify short-link parsing expanded to include `/s/` variants.

## Docs, Structure, and Project Organization

- Wiki source/docs reorganized under `docs/wiki` with sync tooling.
- Beta update documentation revised for Songify 2.0 context.
- Project structure updates tied to modernization and migration workflow scaffolding.

## File Change Distribution (high level)

Largest concentrations of file-level changes:

- `Songify Slim/Views/` (16.3%)
- `Songify Slim/Util/General/` (11.5%)
- `Songify Slim/Views/WPFUI/Pages/` (7.9%)
- `Songify Slim/UserControls/` (6.6%)
- `Songify Slim/Properties/` (5.7%)
- `docs/wiki/` (5.7%)
- `.github/upgrades/scenarios/dotnet-version-upgrade/` (3.9%)

## Merges in This Range

- `2fed2f7` (2026-08-27) Merge branch `feature/wpfui-shell` into `feature/wpfui-shell`

## Full Changelog (non-merge commits, newest first)

- `3436cb3` feat(ui): overhaul setup wizard, home checklist, and help
- `1ed8ceb` style(ui): add tooltips to NavigationView items for accessibility
- `23dbe96` feat(ui): add side menu style setting and persist state
- `0306005` style(ui): reorder XAML attributes and reformat controls
- `28c249a` feat(ui): add accent color wheel and improve settings
- `19ed423` feat(settings): improve import preview and ignore bots UX
- `1d2c3a3` feat(ui): add Help page, accent color, Twitch ignore, and UI upgrades
- `435863f` fix(pear): improve queue index lookup and error handling
- `5b2d0bf` fix(queue): improve region-lock handling and queue logic
- `d86735a` refactor(ui): simplify RefundConditions handling and toggling
- `104c0dd` feat(ui): add pop out queue window and min size override
- `3dbcd25` chore(build): update version to 2.0.0.2 in AssemblyInfo and csproj
- `b7770e8` style(ui): reorder StackPanel attributes and remove fixed width
- `11dff3a` style(ui): adjust NumberBox spacing in UC_CommandItem.xaml
- `651751d` refactor(spotify,ui): improve persistent issue handling and dialogs
- `9a42878` feat(ui): add UI scale (zoom) feature and PerMonitorV2 DPI
- `b66ff20` feat(ui): display beta patch notes in patch notes window
- `9443431` fix(twitch): rebind commands after config load or import
- `1fb32ff` chore(build): update copyright year to 2026
- `28b238d` feat(ui): improve checklist icons and scrolling behavior
- `9cd9d79` Revise beta update document for Songify 2.0.0
- `00dba06` feat(updater): add release channel setting and refactor update logic
- `c4423b8` build(project): remove Fody weavers and related packages
- `e4b8a20` refactor(ui): use clickable status buttons and opaque brushes
- `99dca6c` feat(playlist): add playlist command with localization and UI
- `2c62c02` fix(twitch): expand Spotify URL check to handle /s/ links
- `100cbdc` feat(ui): add Songify Premium support and UI integration
- `3d72c10` refactor(api): remove AccessKey usage from config and payloads
- `1a7b0cd` refactor(ui): replace fully qualified types with direct references
- `2409819` feat(localization): localize min messages between song announcements
- `ce9a908` feat(ui): add MinimumMessagesBetweenAnnounces setting
- `a7564ef` refactor(ui): support detachable console window and UI cleanup
- `1836a8f` feat(ui): add Songify API token setup and status UI
- `5ca1cc0` refactor(api): centralize Songify API auth and update settings flow
- `4735c38` feat(localization): add setup wizard and feature strings in multiple languages
- `4577920` feat(ui): add setup wizard and onboarding checklist
- `abb3885` feat(twitch): add Skip Poll command and improve poll handling
- `4960ad2` feat(ui): add user level config for explicit song requests
- `3a714ff` feat(ui): always enable album cover download, add canvas animation
- `061f6bb` #215 #216
- `ed9e6e3` feat(ui): major history and queue UI/UX improvements
- `f8dc718` Polish Shell PSAs and disable leftover ClickOnce publish.
- `65cf524` Ignore temporary _build_verify* output folders.
- `25b891b` Remove MainWindow and legacy windows; add Shell PSAs and shared services.
- `20dd745` Replace history.shr with YAML storage and remove history upload.
- `bdfc5c5` feat(ui): improve localization, config comparison, and dialogs
- `5ec2d19` feat(localization): enable full UI localization and resource-based strings
- `fa8ffb4` feat(ui): redesign blocklist and reward dialogs, improve UX
- `4b05a1b` refactor(ui): centralize bot response UI with data-driven catalog
- `c40114a` refactor(ui): centralize settings UI refresh logic
- `7f584ee` refactor(ui): use DynamicResource for icon and header colors
- `4a22b82` Polish WPF-UI shell: dialogs, single-file paths, and Spotify idle polling.
- `82f58ad` Modernized WPF UI
- `5cd64d0` feat(ui): add modern WPF shell and MVVM navigation
- `943ef44` refactor(build): migrate to .NET 10 and update charting

## Reference

Full Changelog: https://github.com/songify-rocks/Songify/compare/v1.8.13...feature/wpfui-shell
