# 04-api-compatibility: WPF API Compatibility Remediation

Resolve API incompatibilities preventing compilation after .NET 10 migration. Current build stops on LiveCharts XAML errors. After LiveCharts migration, will reveal the underlying Windows.* and System.Web.UI namespace errors identified in Task 01.

## Current Known Errors

From Task 01 build validation, we have 19 CS* errors across 10 files:
1. **System.Web.UI namespace** (3 files) - Not available in .NET 10
2. **Windows.UI.* namespaces** (3 files) - UWP APIs not available in WPF/.NET 10
3. **Windows.Media.Control** (2 files) - UWP MediaControl APIs
4. **Windows.Storage.Streams** (2 files) - UWP storage APIs
5. **Windows.ApplicationModel** (1 file) - UWP app model
6. **Windows.Graphics.Imaging** (1 file) - UWP imaging APIs
7. **Toast notification types** (1 file) - Microsoft.Toolkit.Uwp.Notifications migration
8. **GlobalSystemMediaTransportControls*** (6 occurrences) - Windows media control APIs
9. **LiveCharts → LiveChartsCore** (2 files + ViewModel) - From Task 03

## Assessment Context

The 7,979 binary-incompatible APIs in assessment represent potential issues - most are WPF types that compile fine in .NET 10 with the correct references. The actual compilation blockers are the 19 errors above plus any issues revealed after fixing them.

High risk of introducing subtle bugs. WPF threading model changes may cause runtime exceptions. Custom controls may break visually. XAML binding expressions may compile but fail at runtime.

Research: Microsoft's ".NET Framework to .NET 10 API differences" and "Breaking changes in .NET" documentation. Focus on UWP API removal, System.Web removal, and Windows.Media.Control alternatives.

**Done when**: Solution compiles with zero errors, zero build warnings related to API incompatibility, all blocking issues addressed, application launches.

## Complexity Analysis

This task requires decomposition:
1. **LiveCharts → LiveChartsCore migration** (2 C# files + 1 XAML + 1 ViewModel)
2. **UWP API removal** (Windows.*, toast notifications, media controls) - 9 files
3. **System.Web.UI removal** (3 files)
4. **Build validation** and remaining error fixes

Each subtask should be isolated, testable, and buildable independently where possible.
