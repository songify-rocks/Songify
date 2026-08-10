# 08-deployment: Deployment Pipeline and Production Release

Update build infrastructure for .NET 10 SDK. Update CI/CD configurations to use .NET 10 SDK (10.0.100+). Configure self-contained or framework-dependent deployment. Update installer/deployment packages (AutoUpdater.NET.Official integration) with .NET 10 dependencies. Configure code signing and publishing. Update documentation with .NET 10 prerequisites. Create rollback plan. Package and distribute production release.

Current application uses AutoUpdater.NET.Official (1.9.2) for updates. .NET 10 applications require different runtime deployment than .NET Framework. Self-contained deployment bundles runtime but increases package size. Framework-dependent deployment requires users install .NET 10 Desktop Runtime. Current build process needs updates for SDK-style projects and .NET 10 targets.

Deployment changes have highest risk for user impact. Self-contained increases installer from ~10MB to ~200MB but eliminates user runtime dependency. Framework-dependent requires user action. AutoUpdater updates .NET Framework installations to .NET 10 version, requiring careful version gating. Code signing certificates may need renewal. .NET 10 requires Windows 10 1607+ or Windows 11.

Research: Review ".NET application publishing overview" and "Deploy .NET Windows desktop apps" documentation. Understand self-contained vs framework-dependent tradeoffs. Research AutoUpdater.NET.Official .NET 10 compatibility. Test deployment on clean Windows installations.

**Done when**: Installer successfully deploys on target Windows versions, application launches on fresh installations, AutoUpdater successfully updates existing installations, documentation complete, rollback plan tested, release published.
