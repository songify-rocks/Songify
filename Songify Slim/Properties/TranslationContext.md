# Context-aware translations

Translators see **context in two ways**:

## 1. Key prefix (where it appears)

| Prefix | Meaning |
|--------|--------|
| `common_*` | Reused in multiple places (buttons like Add, Close, Delete; column headers like Artist, Title). Translate for **generic** use. |
| `menu_*` | Main menu (File, View, Twitch, FAQ, Discord, etc.). |
| `window_<name>_*` | Specific window or tab. Example: `window_queue_*` = Queue window, `window_blocklist_*` = Blocklist, `window_settings_*` = Settings. |
| `dialog_*` | Dialog boxes and prompts. |
| `param_*` | Bot/response parameter descriptions (shown in UI as help text). |
| `cta_*` | Call-to-action (e.g. support link; can override menu text per language). |
| `language_*` | Language name for the language selector. |
| `uc_*` | User control (e.g. `uc_command_*` = command editor, `uc_reward_*` = reward tile). |

Same English word can have **different keys** in different contexts so you can translate differently (e.g. “Close” as button vs “Close” in another sense).

## 2. RESX &lt;comment&gt; (how it’s used)

In `Resources.resx` (and culture files), each entry can have a `<comment>` that describes:

- **Context:** Button, menu item, tooltip, tab header, column header, window title, etc.
- **Where:** e.g. “Queue window”, “Blocklist dialog”, “Settings → Spotify”.

Use the comment to choose the right tone and wording (e.g. short for buttons, full sentence for descriptions).

## Adding or editing context

When adding new keys:

1. Use a **specific key** per context (e.g. `window_blocklist_skip` for “Skip” in the blocklist dialog, not a generic `common_skip` if the meaning differs elsewhere).
2. Add a **&lt;comment&gt;** in the RESX: e.g. `Context: Button in Blocklist dialog to skip / not block any.`

Example:

```xml
<data name="window_blocklist_skip" xml:space="preserve">
  <value>Skip</value>
  <comment>Context: Button in blocklist resolve dialog to skip / not block any.</comment>
</data>
```

This way translators see both the key (which window/area) and the comment (exact UI element and intent).
