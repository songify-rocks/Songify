# 02-package-migration: Dependency Modernization

The project was already converted to PackageReference format during SDK-style conversion in Task 01. This task focuses on removing redundant framework packages that are now included implicitly with .NET 10.

Current state: 73 packages in PackageReference format. Many are .NET Standard 1.x polyfill packages (System.*, Microsoft.NETCore.Platforms, NETStandard.Library) that were required for .NET Framework 4.8 but are now built into .NET 10. These redundant packages add noise, can cause version conflicts, and should be removed.

Packages to evaluate for removal:
- Microsoft.NETCore.Platforms  
- NETStandard.Library
- System.*packages (v4.3.0-4.3.1) - .NET Standard polyfills
- Microsoft.Win32.Primitives
- Microsoft.CSharp (now in-box with .NET 10)

Research: Check each System.* package to confirm it's a polyfill and not a newer extension library. System.Resources.Extensions and System.Text.Json are extension packages and should be KEPT.

**Done when**: All redundant polyfill packages removed (~40 packages), current application packages retained, package restore succeeds, build succeeds (same errors as Task 01 - API compatibility issues).

## Research Findings

### Package Analysis
Identified 41 redundant .NET Standard 1.x polyfill packages for removal:

**Framework packages** (3):
- Microsoft.CSharp 4.7.0 - now in-box with .NET 10
- Microsoft.NETCore.Platforms 7.0.4 - metapackage, not needed
- Microsoft.Win32.Primitives 4.3.0 - built into .NET 10
- NETStandard.Library 2.0.3 - metapackage for .NET Standard, not needed in .NET 10

**System.* polyfills** (37):
All version 4.3.0-4.3.1 packages that provide APIs now built directly into .NET 10:
- System.AppContext, System.Collections, System.Collections.Concurrent
- System.Console, System.Diagnostics.*, System.Globalization.*
- System.IO.*, System.Linq, System.Linq.Expressions
- System.Net.*, System.ObjectModel, System.Reflection.*
- System.Resources.ResourceManager, System.Runtime.*
- System.Security.Cryptography.*, System.Text.*
- System.Threading.*, System.Xml.*

**Packages to KEEP** (32 application packages + 2 extensions):
- Application packages: AutoUpdater, Costura.Fody, EmbedIO, HtmlAgilityPack, LiveCharts, MahApps.Metro, SpotifyAPI, TwitchLib, WPF-UI, YamlDotNet, etc.
- Extension packages: System.Resources.Extensions 10.0.7, System.Text.Json 10.0.7 (modern extension libraries, not polyfills)
