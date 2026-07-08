# Git Commit Message Instructions

Always generate commit messages using the following format:

```text
<type>(<scope>): <short summary>

<optional detailed description>

<optional ticket/reference>
```

## Types

Use exactly one of the following commit types:

- feat – New feature
- fix – Bug fix
- refactor – Code change that neither fixes a bug nor adds a feature
- perf – Performance improvement
- style – Formatting only (no logic change)
- docs – Documentation only
- test – Adding or updating tests
- build – Build system or dependency changes
- ci – CI/CD changes
- chore – Maintenance tasks

## Scope

The scope should describe the primary affected area.

Examples:

- api
- ui
- spotify
- twitch
- youtube
- websocket
- overlay
- config
- localization
- updater
- database
- auth
- build

Use only one scope unless multiple areas are equally important.

## Summary

The summary must:

- Use the imperative mood (e.g. "add", "fix", "remove", "refactor")
- Be 72 characters or fewer
- Be specific and concise
- Not end with a period

Good examples:

```text
fix(spotify): load all playlists using pagination
feat(ui): add dark mode toggle
refactor(api): simplify request retry handling
```

## Body

Include a body only when additional context is helpful.

When included:

- Explain **why** the change was made, not just **what** changed.
- Mention important implementation details only if they help future readers.
- If user action is required after updating, state it clearly.
- Wrap body lines at approximately 72 characters.

Example:

```text
fix(spotify): load all playlists using pagination

Spotify only returns up to 50 playlists per request. Continue
requesting additional pages until all playlists have been loaded.
```

## Breaking Changes

For incompatible changes, add a separate paragraph beginning with:

```text
BREAKING CHANGE:
```

Example:

```text
BREAKING CHANGE: Existing Spotify credentials must be recreated.
```

## References

If applicable, append a ticket or issue reference at the end.

Examples:

```text
Refs #123
Fixes #456
Closes #789
```

## General Rules

- Base the commit message only on the staged changes.
- Do not invent changes or motivations.
- Do not mention files unless they are relevant.
- Avoid vague summaries such as:
  - update
  - fixes
  - cleanup
  - misc changes
  - improvements
- Prefer describing the observable outcome over implementation details.
