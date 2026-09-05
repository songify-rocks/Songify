# 07-testing: Comprehensive Testing and Validation

Execute comprehensive test suite covering all features: Spotify integration (auth, playlists, playback detection), Twitch integration (OAuth, chat commands, channel points, polls), YouTube/YTM via Pear API, queue management, request handling, blocklist, configuration persistence, web server/WebSocket endpoints, localization (8 languages), UI theming, notifications, auto-updates. Create test cases targeting the 197 behavioral changes. Perform visual UI testing. Load test WebSocket connections and HTTP endpoints. Validate third-party API integrations.

The 21% codebase impact across 91 files means nearly every feature was touched. The 197 behavioral changes include subtle runtime differences that won't surface as compilation errors. Technologies requiring focus: WPF UI (4,630 API issues), Windows Forms interop (79), GDI+ (42). Application complexity (39,199 LOC, 142 files) with multiple third-party integrations demands exhaustive testing.

High probability of runtime-only failures. Behavioral changes are insidious - features may "work" but behave differently. Performance regressions possible. Threading/concurrency issues may only manifest under load. Third-party API integrations may fail due to OAuth flow changes. UI theme switching and custom control rendering require manual verification.

Research: Create structured test plan. Use Application Insights for telemetry. Leverage WPF debugging tools (Snoop, WPF Inspector) for visual issues. Set up stress testing for hosted services. Review .NET 10 behavioral changes documentation.

**Done when**: All features tested and functional, zero runtime exceptions during test scenarios, UI renders identically to .NET Framework version, performance meets or exceeds baseline, all 197 behavioral changes validated.
