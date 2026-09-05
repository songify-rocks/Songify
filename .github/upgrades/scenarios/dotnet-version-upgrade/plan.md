# .NET 10 Upgrade Plan

## Strategy Declaration

**Scenario ID**: dotnet-version-upgrade  
**Strategy**: All-at-Once  
**Project Approach**: In-place  
**SDK-Style Conversion**: Yes  
**Package Management**: PackageReference

**Rationale**: Single-project solution allows for comprehensive upgrade in one coordinated effort. WPF is fully supported in .NET 10, making in-place migration the most efficient approach. SDK-style conversion is mandatory for modern .NET, and PackageReference provides better dependency management.

---

## Tasks

### 01-project-structure: Project Structure Modernization

Convert the legacy .csproj file to SDK-style format and retarget to .NET 10. This transforms the 839-line legacy project file into a modern SDK-style format (typically 20-30 lines). The conversion eliminates verbose XML declarations, consolidates assembly references, updates the target framework to net10.0-windows, and enables WPF-specific properties.

The current project uses .NET Framework 4.8 with extensive manual configurations. Migration to SDK-style is mandatory for .NET 10. The project uses Resource.Embedder and Costura.Fody which may require specific SDK-style configurations. Primary risk is loss of custom build configurations. Compilation will fail initially (expected ~8,308 errors) due to API incompatibilities.

Research: Use `dotnet upgrade-assistant analyze` to preview conversion. Review Microsoft's "Upgrade a WPF App to .NET" guide. Check Resource.Embedder and Costura.Fody documentation for .NET 10 patterns.

**Done when**: Project file reduced to <50 lines, solution loads in Visual Studio, initial compilation attempted (errors expected but project structure is valid).

### 02-package-migration: Dependency Modernization

Migrate all 73 NuGet package references from packages.config to PackageReference format. This enables transitive dependency resolution and automatic version conflict resolution. Evaluate and remove 40+ framework reference packages (Microsoft.NETCore.Platforms, System.* packages, NETStandard.Library) that are now included implicitly with .NET 10.

The project currently uses packages.config, incompatible with SDK-style projects. Assessment shows 61 packages are already .NET 10 compatible. Several packages provide functionality now built into .NET 10. The migration enables NuGet's improved conflict resolution, critical given the 7 binding redirect issues identified.

Package migration can expose hidden version conflicts. The 7 binding redirect issues indicate existing assembly version mismatches. Visual Studio's automatic migration tool can fail with complex dependency graphs; manual review essential.

Research: Use Visual Studio's "Migrate packages.config to PackageReference" tool but verify output carefully. Review each incompatible package's documentation.

**Done when**: No packages.config file remains, all packages present as PackageReference in .csproj, ~40 redundant packages removed, package restore succeeds.

### 03-incompatible-packages: Incompatible Package Resolution

Replace or upgrade 6 incompatible packages: LiveCharts/LiveCharts.Wpf → LiveChartsCore 2.0+, Microsoft.Xaml.Behaviors.Wpf 1.1.142 → 1.1.39, NHttp → alternative, MahApps.Metro.IconPacks 6.2.1 → 4.11.0, WPF-UI downgrade, System.ServiceModel → CoreWCF 1.9.1.

Assessment identified 6 incompatible packages. LiveCharts represents highest risk due to widespread UI charting usage (ApiChart.xaml, ApiMetricsVm.cs). NHttp incompatibility may conflict with existing EmbedIO usage. These packages impact UI rendering, charting, HTTP serving, and XAML behaviors.

LiveCharts → LiveChartsCore involves significant API breaking changes. NHttp replacement may require rewriting WebServer.cs. IconPacks downgrade might remove icons currently in use. Budget significant testing time for UI components.

Research: LiveChartsCore migration guide, MahApps.Metro.Icon Packs changelog, EmbedIO vs NHttp comparison, GitHub issues for migration patterns.

**Done when**: All 6 incompatible packages resolved, solution builds (API errors from next task expected), no package-related errors.

### 04-api-compatibility: WPF API Compatibility Remediation

Resolve 7,979 binary-incompatible WPF API usages across the codebase. Focus on binary incompatibilities preventing compilation: namespace changes, removed APIs, and signature modifications. Common changes include System.Windows.* namespace updates, BitmapImage constructor changes, Dispatcher usage updates, control template binding syntax, and data binding expression updates.

The 7,979 binary incompatible APIs affect 91 of 142 files (64%). WPF represents 4,630 issues concentrated in Views/*.xaml.cs, UserControls/UC_*.xaml.cs, ThemeHandler.cs, ImageConverter.cs, ThumbnailConverter.cs. Windows Forms issues (79) stem from legacy interop. GDI+ issues (42) affect custom rendering. Source incompatibilities (132) require recompilation testing.

High risk of introducing subtle bugs. WPF threading model changes may cause runtime exceptions. Custom controls may break visually. XAML binding expressions may compile but fail at runtime. The 21% codebase impact means nearly every UI component needs testing.

Research: Microsoft's ".NET Framework to .NET 10 API differences" and "Breaking changes in .NET" documentation. Use .NET Upgrade Assistant's compatibility reports. Leverage Roslyn analyzers for bulk pattern detection.

**Done when**: Solution compiles with zero errors, zero build warnings related to API incompatibility, all 8,308 identified issues addressed, application launches.

### 05-package-updates: Extended Package Updates

Update Microsoft.Extensions.* packages (Hosting, Hosting.Abstractions, Logging, Options) from 10.0.7 to 10.0.10. Evaluate all 73 packages for newer versions. Consolidate package versions where multiple packages from the same family exist.

Assessment identifies 6 recommended updates. These packages form the application's service hosting infrastructure (WebSocketHostedService.cs, Twitch integration). Upgrading ensures compatibility with .NET 10 runtime improvements.

Microsoft.Extensions.* packages often have subtle breaking changes. Hosted services (Twitch, WebSocket servers) are critical; service lifecycle bugs are difficult to detect. Version mismatches can cause runtime exceptions.

Research: Microsoft.Extensions release notes for 10.0.7 → 10.0.10 changes. Use `dotnet list package --outdated` for additional candidates.

**Done when**: All 6 recommended packages updated, no version conflict warnings, application compiles and restores successfully.

### 06-configuration: Configuration and Runtime Compatibility

Resolve 7 binding redirect issues and update runtime configuration. Replace app.config binding redirects with runtime configuration. Update App.config to modern format, removing obsolete sections. Configure runtime options for performance (TieredCompilation, ReadyToRun). Update manifest files for .NET 10 requirements.

Binding redirect issues show assembly version conflicts (3 mandatory, 4 potential). Current App.config contains .NET Framework-specific sections (<system.web>, <system.serviceModel>, binding redirects) requiring transformation. The app.manifest may need .NET 10 runtime version updates.

Incorrect binding redirect resolution can cause MissingMethodException, FileLoadException, or TypeLoadException at runtime. Configuration migration isn't well-documented for complex scenarios. Settings persistence (ConfigHandler.cs, Settings.cs with YAML) may interact unexpectedly.

Research: Microsoft's "Configuration in .NET" and "Migrating app.config" documentation. Review .NET 10 Runtime Configuration Options, examine binding conflicts through Fusion Log Viewer.

**Done when**: Zero binding redirect issues, application launches and loads configuration, settings persistence works, no configuration-related runtime exceptions.

### 07-testing: Comprehensive Testing and Validation

Execute comprehensive test suite covering all features: Spotify integration (auth, playlists, playback detection), Twitch integration (OAuth, chat commands, channel points, polls), YouTube/YTM via Pear API, queue management, request handling, blocklist, configuration persistence, web server/WebSocket endpoints, localization (8 languages), UI theming, notifications, auto-updates. Create test cases targeting the 197 behavioral changes. Perform visual UI testing. Load test WebSocket connections and HTTP endpoints. Validate third-party API integrations.

The 21% codebase impact across 91 files means nearly every feature was touched. The 197 behavioral changes include subtle runtime differences that won't surface as compilation errors. Technologies requiring focus: WPF UI (4,630 API issues), Windows Forms interop (79), GDI+ (42). Application complexity (39,199 LOC, 142 files) with multiple third-party integrations demands exhaustive testing.

High probability of runtime-only failures. Behavioral changes are insidious - features may "work" but behave differently. Performance regressions possible. Threading/concurrency issues may only manifest under load. Third-party API integrations may fail due to OAuth flow changes. UI theme switching and custom control rendering require manual verification.

Research: Create structured test plan. Use Application Insights for telemetry. Leverage WPF debugging tools (Snoop, WPF Inspector) for visual issues. Set up stress testing for hosted services. Review .NET 10 behavioral changes documentation.

**Done when**: All features tested and functional, zero runtime exceptions during test scenarios, UI renders identically to .NET Framework version, performance meets or exceeds baseline, all 197 behavioral changes validated.

### 08-deployment: Deployment Pipeline and Production Release

Update build infrastructure for .NET 10 SDK. Update CI/CD configurations to use .NET 10 SDK (10.0.100+). Configure self-contained or framework-dependent deployment. Update installer/deployment packages (AutoUpdater.NET.Official integration) with .NET 10 dependencies. Configure code signing and publishing. Update documentation with .NET 10 prerequisites. Create rollback plan. Package and distribute production release.

Current application uses AutoUpdater.NET.Official (1.9.2) for updates. .NET 10 applications require different runtime deployment than .NET Framework. Self-contained deployment bundles runtime but increases package size. Framework-dependent deployment requires users install .NET 10 Desktop Runtime. Current build process needs updates for SDK-style projects and .NET 10 targets.

Deployment changes have highest risk for user impact. Self-contained increases installer from ~10MB to ~200MB but eliminates user runtime dependency. Framework-dependent requires user action. AutoUpdater updates .NET Framework installations to .NET 10 version, requiring careful version gating. Code signing certificates may need renewal. .NET 10 requires Windows 10 1607+ or Windows 11.

Research: Review ".NET application publishing overview" and "Deploy .NET Windows desktop apps" documentation. Understand self-contained vs framework-dependent tradeoffs. Research AutoUpdater.NET.Official .NET 10 compatibility. Test deployment on clean Windows installations.

**Done when**: Installer successfully deploys on target Windows versions, application launches on fresh installations, AutoUpdater successfully updates existing installations, documentation complete, rollback plan tested, release published.