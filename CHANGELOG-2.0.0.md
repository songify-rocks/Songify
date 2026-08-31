# Songify 2.0.0 - Release Notes

> Big update from the latest main release to the current development branch.

- **Release line:** `2.0.0`
- **Compared versions:** [`v1.8.6...feature/wpfui-shell`](https://github.com/songify-rocks/Songify/compare/v1.8.6...feature/wpfui-shell)
- **Compared commit range:** `v1.8.6..3436cb3`
- **Total commits in range:** `162` (non-merge: `144`, merge: `18`)
- **Diff size:** `253 files changed`, `+243,915 / -24,534`

---

## Highlights

- New **modern WPF shell** with sidebar navigation and MVVM page model.
- New **onboarding flow**: setup wizard + home checklist + better help surface.
- New **Songify API token flow** (Bearer auth) replacing AccessKey-based usage.
- Major **queue/history UX overhaul** with better state handling and migration support.
- Expanded Twitch tooling: **`!skippoll`**, **`!playlist`**, better command response handling.
- Better Spotify reliability: persistent issue banners, throttled notifications, improved diagnostics and fallback behavior.
- Full UI localization expansion plus broad translation refreshes.
- Platform/build modernization including **.NET 10 migration** and cleanup of legacy weavers.

---

## Breaking Changes and Required Migration Notes

### 1) Auth model changes for Songify web features
- `AccessKey` usage was removed from config/payload flow.
- Songify API usage now relies on centralized auth/token handling and bearer-based requests.
- Existing setups using old key expectations should be updated to token-based auth.

### 2) UI architecture has changed significantly
- Legacy multi-window flow (`MainWindow`, old Settings/History/Queue/Userlist windows, etc.) was removed.
- Navigation now lives in a unified WPF-UI shell and page model.
- Any custom automation/macros targeting old window names must be updated.

### 3) History storage migration
- History storage moved from legacy SHR flow to YAML-backed storage.
- History upload behavior from older flow was removed while local history handling was modernized.

### 4) Build/runtime baseline changes
- Project migrated to newer runtime/tooling stack and package graph.
- Fody weavers and related package usage were removed.

---

## Added

### Core UX / Shell
- Modern WPF shell and page navigation.
- Detachable console support and pop-out queue window.
- Home checklist improvements and setup wizard iteration.
- Help page, tooltips, side-menu style setting, and UI scale (zoom + PerMonitorV2 DPI).
- Accent color controls (including color wheel).
- Embedded Songify/Spotify assets and patch notes view integration.

### Songify API / Premium
- Songify API token setup and status surfaced in UI.
- Centralized Songify auth service flow.
- Songify Premium UI integration and feature entry points.

### Twitch / Song Requests
- New `!playlist` command support with localization/UI wiring.
- New `!skippoll` command + improved skip poll handling.
- Minimum messages between announces setting.
- User-level controls for explicit song requests.
- Twitch chat ignore enhancements and command-response behavior improvements.

### Spotify / Player / Queue
- Spotify idle polling backoff and API-failure notification throttling.
- Persistent Spotify issue banners and improved diagnostics.
- Artist blocklist CSV import + sync flow.
- Better queue/history visuals and synchronization behavior.
- Qobuz player support.

### Web / API
- WebSocket password protection and log retention.
- Interactive WebSocket API docs and tester.
- SR enable/disable via WebSocket with scope support.

### Documentation
- In-repo wiki source under `docs/wiki` with sync script.
- New/updated setup, troubleshooting, command, and API docs.

---

## Changed / Improved

- Clickable status indicators and more actionable service states.
- Bot response management centralized into a data-driven catalog.
- Settings refresh logic and sections reorganized for maintainability.
- Theme resources moved toward dynamic resource patterns for better runtime behavior.
- Localization moved to a broader, resource-first model across the new UI.
- Queue/player routing behavior normalized across Twitch/Spotify/Pear paths.
- Updater settings expanded with release channel support.

---

## Fixed

- Queue logic and region-lock handling stability improvements.
- Pear queue index lookup + error handling improvements.
- Twitch command rebinding after config load/import.
- Spotify short-link parsing improvements (`/s/` handling).
- Duplicate/current-song request blocking in Twitch request flow.
- Spotify device fallback/recovery and auto-connect init order hardening.
- Cover sync and unsupported-player behavior fixes.
- WinRT stream-size conversion guard fixes.
- OAuth callback diagnostics hardening for Spotify.
- Canonical player-type normalization (`YouTube`) across request/queue flow.

---

## Removed / Deprecated

- Legacy windows and old shell entry points removed.
- Old AccessKey-driven API pattern removed.
- Legacy Fody weaver files and package references removed.
- Older standalone documentation files moved/restructured into `docs/wiki` and `docs/releases`.

---

## Build / Project / Infra

- Migration to updated .NET/runtime stack and related package refresh.
- Version progression through late 1.8.x into 2.0.x track.
- Project/solution cleanup and modernization for the new shell model.

---

## Localization and Community Contributions

Translation updates landed across multiple languages (including German, Dutch, Portuguese variants, Russian, Polish, Italian, French, Spanish, Belarusian) through direct edits and Weblate syncs.

### Contributors in this release range

- **Jan Blömacher** - 125 commits
- **Ryan Farrington** - 19 commits
- **Anonymous** - 10 commits
- **gwyden** - 4 commits
- **Berliner** - 3 commits
- **Paul Bürger** - 1 commit

---

## Merged Pull Requests

- #217 - bugfix/07292026
- #214 - bugfix/213
- #211 - bugfix/twitch-init-spotify-device-recovery
- #210 - bugfix/twitch-init-spotify-device-recovery
- #208 - bugfix/207
- #206 - qol-enhancements
- #196 - WebSocket SR enable/disable scopes
- #194 - Windows media session picker
- #190 - merge from main

---

## Full Commit Appendix (newest first, non-merge)

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
- `7d5241e` feat(twitch): enhance skip reward and Spotify diagnostics
- `3be177f` refactor(ui): optimize Twitch rewards UI and settings logic
- `4827138` feat(websocket): add password protection and log retention
- `de86d15` feat(spotify): import and sync artist blocklists from CSV
- `daac51f` fix(twitch,spotify): cancel skip redemption on failure and enforce chat rate limit
- `74b25ea` Translated using Weblate (Belarusian)
- `39950ff` Translated using Weblate (German)
- `a03f33c` build(config): bump version to 1.8.12.0
- `a8cf495` Translated using Weblate (Dutch)
- `1acc1b1` Translated using Weblate (Belarusian)
- `cf9e740` Translated using Weblate (Portuguese (Brazil))
- `fcd3233` Translated using Weblate (Russian)
- `e56078b` Translated using Weblate (Portuguese (Portugal))
- `6051b2c` Translated using Weblate (Polish)
- `75be136` Translated using Weblate (Italian)
- `91d4fa9` Translated using Weblate (French)
- `4ed9ec6` Translated using Weblate (Spanish)
- `4ee8217` Translated using Weblate (German)
- `dba6619` feat(spotify): add idle polling backoff and UI indicator
- `2466482` fix(twitch): improve Spotify track ID extraction
- `f06cb7d` refactor(ui): reorganize settings UI and add section headers
- `bfc1833` refactor(songfetcher): adjust paused track logic
- `69446e6` feat(spotify): throttle user notifications for API failures
- `63ba8ed` refactor(twitch): normalize request ingress and align hosted service flow
- `64bddf3` feat(ui): add Spotify toast toggle and smarter error dedupe
- `8db143a` feat(ui): embed local Songify and Spotify logos
- `ca18667` feat(twitch): send or announce command responses
- `e1befd0` fix(ui): improve Spotify error banner and notification text
- `0b584f9` refactor(spotify): comment out GetPlaylist method
- `8cbfa09` feat(ui): remove Spotify playlist SR restriction
- `2cb22ac` feat(ui): limit Spotify playlist dropdown height
- `cb5ddf1` feat(ui): limit Spotify playlist dropdown height
- `696a1bb` Refactor playlist fetch to handle pagination, update structs
- `f718957` Add Git commit message instructions
- `1891ef9` Update assembly version to 1.8.11.0
- `395209a` fix(twitch-requests): block duplicate requests for currently playing song
- `78a58db` fix(pear): keep autoconnect active with one-shot alerts and backoff retries
- `3247d2a` fix(spotify,queue): skip device lookup when a stored Spotify device id exists
- `a912bc2` feat(pear): toggle auto-connect from status icon and update Pear action tooltip copy
- `bb666d0` feat(logger,pear): centralize JSON parse exception logging with around-error preview
- `24a6b24` fix(twitch,spotify,queue,logging): harden twitch auto-connect init order, add spotify device fallback/recovery, and normalize diagnostic logging
- `135d815` fix: avoid blocking UI thread during cover image retries
- `6cd25fe` fix: Corrected logging text
- `d98ce3c` fix: guard WinRT stream size conversions for thumbnails
- `e9ed43f` fix(spotify): harden OAuth callback port 4002 startup diagnostics
- `e4a5cfc` fix(windows-playback,queue,pear): improve cover sync and unsupported-player handling
- `2cc5886` fix(queue,twitch,pear): canonicalize request player type to YouTube Rename RequestPlayerType enum member from Youtube to YouTube across request flow Update Twitch and SongFetcher references to the canonical enum value Normalize incoming queue playerType values case-insensitively in QueueService Keep Spotify normalization explicit for consistent playerType storage
- `8fd6549` fix(queue): preserve canonical request data across fallback and queue actions
- `0ab923b` Added try catch
- `9c58a87` feat(i18n,songhandler): localize wrong-player refund reason and apply PR fixes
- `c1b0c16` feat(player,twitch): improve service indicators and request routing
- `0a958f0` feat(spotify): add persistent Spotify error banner
- `d17e4a7` feat(pear,twitch,build): improve player QoL and compatibility
- `20f9631` feat(spotify): add user notifications for Spotify errors
- `03795cb` feat(bot,ui,twitch): add customizable response for disabled commands
- `b8d31bb` feat(pear,twitch): improve queue cleanup and command feedback
- `9dcb61d` feat(pear): improve real-time Pear sync with robust WebSocket/HTTP
- `e0fa78a` Add song request enable/disable actions
- `36e8d61` build(version): bump version to 1.8.10.0
- `801c576` feat(player): add Qobuz desktop player support
- `fe8bad6` feat(api): add interactive WebSocket API docs and tester
- `f19bf43` feat(websocket): add SR enable/disable with scope support #196
- `47a0acb` chore(build): bump version to 1.8.9.1
- `df7dc32` refactor(settings, twitch): clean up, improve error handling, and remove dead code
- `a117977` chore(build): bump assembly version to 1.8.9.0
- `7572df5` style(ui): improve XAML design-time localization support
- `3a0b56e` feat(spotify): add live gating, Test Mode, and localization
- `9454ffc` Translated using Weblate (Belarusian)
- `953520f` feat(responseparams): add search/filter and {userreq} variable
- `5041f8d` feat(ui): add refresh button for Windows media sessions
- `96fd877` refactor(song-upload): use strongly-typed payload for Songify API
- `4d0bcd0` chore(assembly): bump version to 1.8.7.2
- `2bb89a7` refactor(song-fetcher): improve next track selection and metadata sync
- `fecb809` chore(assembly): bump version to 1.8.7.1
- `e9d7892` refactor(ui): update copyright text and hyperlink in footer
- `2318860` feat(spotify): improve API error logging and test mode UI gating
- `198d505` fix(spotify): improve queue error handling and clarify gating
- `a90ab30` chore(assembly): bump version to 1.8.7.0
- `d5e5efe` feat(ui): add Windows media session picker to main window #194
- `e2389d0` Document WebSocket connection and response details
- `78057fd` refactor(api): simplify user model and restructure JSON response
- `f7c95a8` feat(pear): improve YT Music queue correlation & metadata
- `c753b24` Use Bearer auth header for API token in user settings
- `af09b07` Delete Songify Slim/Util/Spotify/SpotifyApiClient.cs
- `1b1e063` Add Spotify fetch gating bypass and Test Mode toggle
- `8e3a0f4` fix(ui): prevent overwriting custom status on Spotify link
- `359c222` docs: normalize Getting-Started wiki page filename
- `e156695` docs: add wiki source under docs/wiki and sync script
- `44b2cbd` Translated using Weblate (Belarusian)

---

## Notes

- This changelog is intentionally comprehensive and derived directly from commit history and diff stats for `v1.8.6..3436cb3`.
- For raw file-level change details, use the compare view linked above.
