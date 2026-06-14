\# GitHub Copilot Instructions - Songify



\## Project Context



Songify is a C# WPF desktop application built on .NET Framework 4.8.



Main integrations:



\- Spotify API

\- Twitch API / EventSub / Chat

\- Local WebSocket server

\- Local HTTP server

\- OBS widgets and overlays

\- Browser Companion extensions



The codebase prioritizes:



\- Stability over cleverness

\- Backward compatibility whenever possible

\- Clear and maintainable code

\- Defensive error handling

\- User-friendly error messages

\- Minimal breaking changes



When generating code:



\- Prefer readability over micro-optimizations.

\- Follow existing project patterns.

\- Avoid introducing new frameworks unless explicitly requested.

\- Keep dependencies to a minimum.

\- Use async/await where appropriate.

\- Handle exceptions gracefully and log meaningful information.

\- Avoid unnecessary abstractions.

\- Do not refactor unrelated code while implementing a feature.



\---



\## C# Guidelines



\### General



\- Target .NET Framework 4.8 compatibility.

\- Prefer explicit types when it improves readability.

\- Use `var` only when the type is obvious from the right side.

\- Keep methods focused on a single responsibility.

\- Avoid deeply nested logic.

\- Extract helper methods when complexity increases.



\### Async Code



\- Prefer async/await over blocking calls.

\- Avoid `.Result` and `.Wait()`.

\- Always consider cancellation and exception handling.



\### Error Handling



\- Never swallow exceptions silently.

\- Log enough context to diagnose issues.

\- User-facing errors should be actionable and understandable.



\### Configuration



\- Preserve existing configuration patterns.

\- Avoid breaking existing config files.

\- Provide sensible defaults for new settings.

\- Support migration paths when config structures change.



\### UI



\- Follow existing WPF and MahApps patterns.

\- Keep business logic out of UI code whenever practical.

\- Avoid introducing large MVVM refactors unless explicitly requested.



\---



\## Spotify Integration



When modifying Spotify-related code:



\- Minimize API requests whenever possible.

\- Consider Spotify rate limits.

\- Handle token expiration gracefully.

\- Maintain compatibility with user-provided Spotify credentials.

\- Preserve existing queue and playback behavior unless specifically changing it.



\---



\## Twitch Integration



When modifying Twitch-related code:



\- Prefer EventSub over legacy PubSub solutions.

\- Consider broadcaster, moderator, VIP, subscriber, and follower permissions.

\- Respect existing command cooldown systems.

\- Maintain backward compatibility with existing bot configurations.

\- Avoid introducing behavior that could cause accidental moderation actions.



\---



\## WebSocket and API Changes



When adding WebSocket commands or API endpoints:



\- Follow existing naming conventions.

\- Validate incoming data.

\- Return useful error responses.

\- Preserve backward compatibility whenever possible.

\- Document new commands and payloads.



\---



\## Performance



Before introducing caching, background tasks, or concurrency:



\- Verify that complexity is justified.

\- Prefer simple solutions first.

\- Consider memory usage.

\- Consider API rate limits.

\- Consider impact on streamers running Songify on older hardware.



\---



\## Testing and Validation



Before proposing a change:



\- Check for null-reference scenarios.

\- Check for disconnected Spotify clients.

\- Check for expired Twitch tokens.

\- Consider startup and shutdown behavior.

\- Consider migration from older Songify versions.



\---



\## Commit Message Format



Always use Conventional Commits.



Format:



```text

<type>(<scope>): <short summary>



<optional detailed description>



<optional ticket/reference>

```



\### Types



\- feat – New feature

\- fix – Bug fix

\- refactor – Code change that neither fixes a bug nor adds a feature

\- perf – Performance improvement

\- style – Formatting only (no logic change)

\- docs – Documentation only

\- test – Adding or updating tests

\- build – Build system or dependency changes

\- ci – CI/CD changes

\- chore – Maintenance tasks



\### Rules



\- Use imperative mood ("add", not "adds" or "added")

\- Keep summary ≤ 72 characters

\- Be specific and concise

\- No trailing period in the summary

\- Scope should describe the affected area

&#x20; - Examples:

&#x20;   - spotify

&#x20;   - twitch

&#x20;   - websocket

&#x20;   - api

&#x20;   - queue

&#x20;   - updater

&#x20;   - ui

&#x20;   - config

&#x20;   - auth

\- Explain why, not just what, in the body if needed

\- If user action is required, mention it clearly in the body

\- Use `BREAKING CHANGE:` for incompatible changes



\### Examples



```text

feat(spotify): add queue support for YouTube Music Desktop



fix(twitch): prevent duplicate EventSub subscriptions



refactor(config): simplify YAML migration handling



perf(queue): reduce playlist cache lookups



docs(api): document websocket queue\_add payload

```



Breaking change example:



```text

feat(auth): require PKCE authentication



BREAKING CHANGE: Users must re-authenticate their Spotify account

```



\---



\## Pull Request Guidelines



When generating pull requests:



\- Summarize user-visible changes first.

\- Mention configuration changes explicitly.

\- Mention migration steps if required.

\- Call out breaking changes clearly.

\- Include testing notes where applicable.



Focus on why the change exists and how it impacts Songify users.

