# 06-configuration: Configuration and Runtime Compatibility

Resolve 7 binding redirect issues and update runtime configuration. Replace app.config binding redirects with runtime configuration. Update App.config to modern format, removing obsolete sections. Configure runtime options for performance (TieredCompilation, ReadyToRun). Update manifest files for .NET 10 requirements.

Binding redirect issues show assembly version conflicts (3 mandatory, 4 potential). Current App.config contains .NET Framework-specific sections (<system.web>, <system.serviceModel>, binding redirects) requiring transformation. The app.manifest may need .NET 10 runtime version updates.

Incorrect binding redirect resolution can cause MissingMethodException, FileLoadException, or TypeLoadException at runtime. Configuration migration isn't well-documented for complex scenarios. Settings persistence (ConfigHandler.cs, Settings.cs with YAML) may interact unexpectedly.

Research: Microsoft's "Configuration in .NET" and "Migrating app.config" documentation. Review .NET 10 Runtime Configuration Options, examine binding conflicts through Fusion Log Viewer.

**Done when**: Zero binding redirect issues, application launches and loads configuration, settings persistence works, no configuration-related runtime exceptions.
