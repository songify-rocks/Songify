# SECURITY AUDIT — Songify (.NET 10)

Date: 2026-08-20  
Auditor: Cursor Cloud Agent (adversarial application-security review)

## Scope and methodology

- Reviewed repository architecture and attack surface across:
  - Desktop app startup/runtime, local IPC/listeners, auth flows, updater, config/secret handling, build scripts, and dependency metadata.
- Performed static code analysis with line-level tracing of suspicious data flows.
- Actively attempted to disprove potential issues before reporting.
- Dependency review:
  - `dotnet build`, `dotnet test`, and `dotnet list package --vulnerable --include-transitive` **could not be executed** in this environment because `dotnet` is not installed (`dotnet: command not found`).
  - As compensating controls, I ran:
    - OSV batch checks over direct packages from `Songify Slim/Songify Slim.csproj`
    - OSV batch checks over 73 package/version pairs from `.github/upgrades/scenarios/dotnet-version-upgrade/dependencies-health.json`
    - NuGet latest-version comparison for direct dependencies.

---

## Executive summary

Overall risk: **High**

The highest-risk issues are:

1. **WebSocket command channel exposed to browser-originated local attacks by default** (no required auth by default, no origin validation), enabling drive-by control of Songify/Twitch actions if the web server is enabled.
2. **Updater trust chain is weak** (MD5-based manifest checksum without explicit code-signature verification in repo logic), creating supply-chain blast radius if the update channel is compromised.

No SQL injection vectors were found (no SQL stack in repo). No unsafe polymorphic JSON/YAML deserialization pattern (e.g., `TypeNameHandling`) was found. Most `Process.Start` usage is non-exploitable in current flows (static URLs or constrained URI parsing), though overall shell-launch surface should still be hardened.

---

## Architecture and attack surface (threat model)

### Key trust boundaries

- **Untrusted network input**
  - Songify APIs, Twitch APIs, Spotify APIs, GitHub releases.
- **Untrusted local-browser origin**
  - Any website opened by user can attempt localhost requests/websockets.
- **Untrusted local process**
  - Same-host processes interacting with named pipes/local listeners.
- **Config and secret-at-rest boundary**
  - YAML config files under app directory.
- **Update channel boundary**
  - AutoUpdater XML + downloaded archive execution.

### Exposed/control surfaces

- Local HTTP/WebSocket server (`HttpListener`) and command routing:
  - `Songify Slim/Util/General/WebServer.cs`
- Named pipe single-instance channel:
  - `Songify Slim/App.xaml.cs`, `Songify Slim/Util/General/PipeMessenger.cs`
- OAuth localhost listeners:
  - Twitch implicit flow: `Songify Slim/Util/Songify/TwitchOAuth/ImplicitOAuth.cs`
  - Spotify PKCE loopback flow: `Songify Slim/Util/Spotify/SpotifyApiHandler.cs`
- Updater:
  - `Songify Slim/Util/General/AppStartup.cs`, `Songify Slim/PostBuild.ps1`
- Secret/config persistence:
  - `Songify Slim/Util/Configuration/Settings.cs`, `Songify Slim/Util/Configuration/ConfigHandler.cs`

---

## Findings

## High severity

### SEC-001 — Local WebSocket command channel is effectively unauthenticated by default and lacks origin validation

- **Severity:** High  
- **Confidence:** High  
- **CWE:** CWE-306 (Missing Authentication for Critical Function), CWE-346 (Origin Validation Error)

**Affected files/lines**

- `Songify Slim/Util/Configuration/ConfigHandler.cs:1025-1032`  
  - WebSocket password protection is explicitly optional and off by default.
- `Songify Slim/Util/General/WebServer.cs:542-543, 597-626`  
  - Auth enforcement depends on optional setting; command processing proceeds if not required.
- `Songify Slim/Util/General/WebServer.cs:330-333, 396-447`  
  - Listener accepts localhost websocket requests and executes JSON commands.
- `Songify Slim/Util/General/WebServer.cs:52-79`  
  - Command map includes action-capable operations (`queue_add`, `skip`, `play_playlist`, `send_to_chat`, etc.).

**Evidence and data flow**

1. Web server listens on loopback.
2. WebSocket request accepted and message parsed as command.
3. If password protection is disabled (default), command handlers execute directly.
4. No origin/referer validation is performed for websocket clients.

**Realistic attack scenario**

- User enables Songify web server.
- User visits a malicious website.
- Site JavaScript opens `ws://127.0.0.1:<port>/...` and sends command JSON.
- Commands execute on victim’s local Songify instance (queue changes, playback control, Twitch-side actions routed by bot/account context).

**Impact**

- Unauthorized command execution in app context.
- Potential abuse of connected Twitch/Spotify actions and automation.

**Why this is not inflated**

- This is not “generic localhost risk”: websocket clients from arbitrary origins are practical in browsers; auth is optional/off by default in current design.

**Recommended remediation**

1. Require authentication by default for all command actions.
2. Add explicit origin allowlist checks for websocket handshakes.
3. Consider binding command websocket to a random per-session path/token.
4. Rate-limit and audit failed auth attempts.

---

### SEC-002 — Update mechanism integrity relies on weak trust model (MD5 manifest checksum; no in-repo strong authenticity controls)

- **Severity:** High  
- **Confidence:** Medium  
- **CWE:** CWE-494 (Download of Code Without Integrity Check), CWE-327 (Use of Broken or Risky Crypto Algorithm)

**Affected files/lines**

- `Songify Slim/Util/General/AppStartup.cs:291-300`  
  - App auto-checks update XML on startup.
- `Songify Slim/Util/General/AppActions.cs:46-53`  
  - Manual update path uses same feed mechanism.
- `Songify Slim/PostBuild.ps1:73-79`  
  - Generated update XML includes `<checksum algorithm="MD5">`.

**Evidence and data flow**

1. Startup triggers updater against remote XML endpoint.
2. Build script emits update metadata using MD5 checksum.
3. No repository evidence of mandatory signature validation of downloaded binaries before execution.

**Realistic attack scenario**

- If update infrastructure/content path is compromised (server, CDN, publish pipeline), attacker can distribute malicious update payload to clients.

**Impact**

- Potential remote code execution via trusted update path (supply-chain compromise).

**Why this is not inflated**

- This does **not** assume TLS bypass; it assumes a realistic updater-channel compromise event.

**Recommended remediation**

1. Enforce Authenticode signature verification on downloaded binaries before install/launch.
2. Use SHA-256+ integrity metadata (or stronger framework-native signed manifests).
3. Add explicit publisher certificate pin/allowlist for updater artifacts.
4. Add release-signing gates in CI/CD (fail build if unsigned/invalid).

---

## Medium severity

### SEC-003 — Sensitive credentials are stored in plaintext at rest (except Songify API key)

- **Severity:** Medium  
- **Confidence:** High  
- **CWE:** CWE-312 (Cleartext Storage of Sensitive Information)

**Affected files/lines**

- `Songify Slim/Util/Configuration/Settings.cs:335-343, 3212-3229`  
  - `SongifyApiKey` is encrypted via DPAPI (positive control).
- `Songify Slim/Util/Configuration/Settings.cs:1805-1807, 2050-2052, 2097-2104, 2297-2299`  
  - Spotify client secret, Spotify refresh token, Twitch access/bot tokens, and web server password are returned/stored as plaintext values.
- `Songify Slim/Util/Configuration/ConfigHandler.cs:247-257, 801-827, 963-1138`  
  - YAML persistence model for credential-bearing objects.

**Evidence and data flow**

- Credentials are loaded from and saved into YAML-backed config objects.
- Only `SongifyApiKey` has DPAPI wrapping; other high-value tokens/secrets do not.

**Realistic attack scenario**

- Local compromise, endpoint backup leak, or accidental config-file sharing exposes long-lived API credentials/tokens for account takeover or abuse.

**Impact**

- Credential theft with downstream account/API abuse.

**Recommended remediation**

1. Extend DPAPI (or Windows Credential Manager) protection to Spotify/Twitch tokens, client secret, and webserver password.
2. Minimize credential lifetime where provider supports rotation/refresh constraints.
3. Add secure export/import path that redacts or re-wraps secrets by default.

---

### SEC-004 — Twitch OAuth localhost flow is susceptible to callback disruption and has weak state construction

- **Severity:** Medium  
- **Confidence:** Medium  
- **CWE:** CWE-352 (CSRF), CWE-330 (Insufficiently Random Values), CWE-20 (Improper Input Validation)

**Affected files/lines**

- `Songify Slim/Util/Songify/Twitch/TwitchHandler.cs:708-714, 724-729, 776-779`  
  - OAuth state salt is weakly generated; mismatch path still tears down active flow.
- `Songify Slim/Util/Songify/TwitchOAuth/ImplicitOAuth.cs:53-60`  
  - State value derived from timestamp + year-offset arithmetic, not cryptographic random nonce.
- `Songify Slim/Util/Songify/TwitchOAuth/ImplicitOAuth.cs:176-193, 372-375`  
  - Local fetch listener accepts posted token/state payload without origin/auth; brittle manual JSON manipulation/parsing.

**Evidence and data flow**

1. OAuth flow starts local listeners.
2. Browser-side script posts token/state to local fetch endpoint.
3. Posted payload is minimally validated and parsed manually.
4. Any received callback path can terminate pending OAuth flow in handler cleanup.

**Realistic attack scenario**

- During OAuth window, malicious local-web origin can attempt localhost POSTs that interrupt or fail the flow (DoS).
- State generation quality is weaker than expected for robust CSRF defense.

**Impact**

- OAuth login disruption; potential forced relogin loops; elevated risk of token-flow abuse relative to stronger nonce/validation designs.

**Recommended remediation**

1. Generate state using cryptographic RNG (`RandomNumberGenerator`), high entropy, one-time nonce.
2. Replace manual string mangling with strict JSON schema parse and explicit error handling.
3. Bind callback acceptance to single-use nonce and expected source constraints; reject malformed payloads safely.
4. Do not tear down flow on first malformed callback unless explicitly user-cancelled.

---

## Low severity

### SEC-005 — API authentication key is sent in query string on state-changing calls

- **Severity:** Low  
- **Confidence:** High  
- **CWE:** CWE-598 (Information Exposure Through Query Strings)

**Affected files/lines**

- `Songify Slim/Util/Songify/ApiClient.cs:72-75, 110-113, 142-145`

**Evidence and data flow**

- `api_key` is appended to URL query for POST/PATCH/Clear API calls.

**Realistic attack scenario**

- Intermediary logs, telemetry tooling, reverse proxies, or diagnostics capture full URLs and leak API key material.

**Impact**

- Increased risk of credential exposure outside intended channels.

**Recommended remediation**

1. Move API key to `Authorization` header or request body (prefer header).
2. Remove query-key support server-side after migration.

---

### SEC-006 — Named pipe control channel has no authentication/authorization boundary

- **Severity:** Low  
- **Confidence:** Medium  
- **CWE:** CWE-306 (Missing Authentication for Critical Function)

**Affected files/lines**

- `Songify Slim/App.xaml.cs:569-587`  
  - Pipe server accepts messages and can trigger deep-link handling.
- `Songify Slim/Util/General/PipeMessenger.cs:21-31`  
  - Client writes arbitrary message to named pipe.
- `Songify Slim/Util/General/SingleInstanceHelper.cs:17-18`  
  - Forwards command-line arg directly to pipe message.

**Evidence and data flow**

- Any local process able to connect to pipe can send `SHOW` or `songify://...` payloads.

**Realistic attack scenario**

- Local software can spam UI actions/token-import prompts (local nuisance/social-engineering vector).

**Impact**

- Local abuse/DoS potential; limited by same-host context and user confirmation on critical token import paths.

**Recommended remediation**

1. Apply explicit pipe security descriptor limiting to current user SID.
2. Add signed/nonce-based message format for sensitive actions.

---

## Dependency vulnerabilities and package posture

## Command execution status

- `dotnet build` → not executed (`dotnet` missing)
- `dotnet test` → not executed (`dotnet` missing)
- `dotnet list package --vulnerable --include-transitive` → not executed (`dotnet` missing)

## Compensating scan results

- OSV (direct dependencies from `.csproj`): **0 known vulnerabilities**
- OSV (73 package/version entries from `dependencies-health.json`, includes transitive set snapshot): **0 known vulnerabilities**

## Outdated direct packages (stable latest comparison)

- Autoupdater.NET.Official `1.9.2` → `1.9.3`
- Costura.Fody `6.1.0` → `6.2.0`
- LiveChartsCore.SkiaSharpView.WPF `2.0.0-rc4.5` → `2.0.5`
- Microsoft.Extensions.Hosting `10.0.7` → `10.0.11`
- Microsoft.Extensions.Hosting.Abstractions `10.0.7` → `10.0.11`
- Microsoft.Extensions.Logging `10.0.7` → `10.0.11`
- Microsoft.Extensions.Options `10.0.7` → `10.0.11`
- Microsoft.Web.WebView2 `1.0.3912.50` → `1.0.4129.50`
- YamlDotNet `17.1.0` → `18.1.0`

Risk note: Outdated does not automatically mean vulnerable, but increases patch-lag and supportability risk.

---

## Secrets review

### Hardcoded secrets

- No hardcoded private API keys/tokens/passwords found in repository code.
- Hardcoded Twitch Client ID is present (`Songify Slim/Util/Songify/TwitchOAuth/ApplicationDetails.cs:9`), which is typically public identifier material (not secret).

### Sensitive data handling observations

- **Positive:** `SongifyApiKey` is DPAPI-protected (`Settings.cs:335-343`, `3212-3229`).
- **Positive:** Cloud save explicitly strips secret-bearing fields before upload (`ConfigHandler.cs:570-578`).
- **Risk:** Multiple other credentials/tokens remain plaintext at rest (SEC-003).

---

## Positive security controls already present

1. **Spotify OAuth uses PKCE + state checks** (`SpotifyApiHandler.cs:222-234`, `633-634`).
2. **WebSocket password comparison is constant-time style** (`WebServer.cs:550-558`).
3. **Artist CSV importer enforces HTTPS and rejects HTTPS→HTTP downgrade redirects** (`ArtistCsvImport.cs:31-47`).
4. **Deep-link token import includes user-interaction confirmation for Songify token replacement** (`App.xaml.cs:282-296`).
5. **No dangerous JSON polymorphic settings observed** (`TypeNameHandling` not found).

---

## Prioritized remediation plan

1. **Lock down WebSocket command surface (SEC-001)**  
   - Require auth by default, enforce origin checks, and introduce anti-automation protections.
2. **Harden updater trust chain (SEC-002)**  
   - Signature-verify update payloads and retire MD5-based integrity signaling.
3. **Encrypt all stored credentials/tokens (SEC-003)**  
   - Extend DPAPI/credential-vault use beyond Songify API key.
4. **Refactor Twitch OAuth localhost callback handling (SEC-004)**  
   - CSPRNG state, strict parsing, resilient malformed-callback behavior.
5. **Remove query-string API keys (SEC-005)**  
   - Migrate to header-based auth and sanitize server logs.
6. **Constrain named-pipe trust boundary (SEC-006)**  
   - Apply SID-scoped pipe ACL and message authentication for sensitive actions.

---

## Findings summary by severity

- **Critical:** 0
- **High:** 2
- **Medium:** 2
- **Low:** 2

