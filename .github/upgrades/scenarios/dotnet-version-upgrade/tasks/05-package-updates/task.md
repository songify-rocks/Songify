# 05-package-updates: Extended Package Updates

Update Microsoft.Extensions.* packages (Hosting, Hosting.Abstractions, Logging, Options) from 10.0.7 to 10.0.10. Evaluate all 73 packages for newer versions. Consolidate package versions where multiple packages from the same family exist.

Assessment identifies 6 recommended updates. These packages form the application's service hosting infrastructure (WebSocketHostedService.cs, Twitch integration). Upgrading ensures compatibility with .NET 10 runtime improvements.

Microsoft.Extensions.* packages often have subtle breaking changes. Hosted services (Twitch, WebSocket servers) are critical; service lifecycle bugs are difficult to detect. Version mismatches can cause runtime exceptions.

Research: Microsoft.Extensions release notes for 10.0.7 → 10.0.10 changes. Use `dotnet list package --outdated` for additional candidates.

**Done when**: All 6 recommended packages updated, no version conflict warnings, application compiles and restores successfully.
