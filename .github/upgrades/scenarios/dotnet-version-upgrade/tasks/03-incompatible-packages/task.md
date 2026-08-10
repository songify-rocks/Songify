# 03-incompatible-packages: Incompatible Package Resolution

Replace or upgrade 6 incompatible packages identified in assessment. After Task 02 removed polyfill packages, the actual remaining incompatible packages are:

**Incompatible packages requiring action**:
1. LiveCharts 0.9.7 + LiveCharts.Wpf 0.9.7 → LiveChartsCore.SkiaSharpView.WPF 2.0+
2. MahApps.Metro.IconPacks 6.2.1 → 4.11.0 (downgrade)
3. Microsoft.Xaml.Behaviors.Wpf 1.1.142 → 1.1.39 (downgrade)
4. NHttp 0.1.9 → Remove (not used in code)
5. WPF-UI 4.3.0 → 2.0.3 (downgrade)
6. System.ServiceModel.Http + System.ServiceModel.NetTcp → Remove (client packages, not used)

Assessment identified 6 incompatible packages. LiveCharts represents highest risk due to widespread UI charting usage (ApiChart.xaml, ApiMetricsVm.cs). The migration involves complete API rewrite from LiveCharts to LiveChartsCore with breaking changes in XAML and ViewModel code.

LiveCharts → LiveChartsCore involves significant API breaking changes requiring code migration. IconPacks/WPF-UI/Behaviors downgrades are safer - these are version compatibility adjustments rather than full replacements.

Research: LiveChartsCore migration guide, MahApps.Metro.Icon Packs changelog 4.11→6.2, WPF-UI 2.0→4.3 changelog, verify NHttp/ServiceModel not used in code.

**Done when**: All 6 incompatible packages resolved, LiveChartsCore installed (code migration deferred to Task 04), solution builds with API errors (not package errors).

## Research Findings

### Package Analysis

**NHttp 0.1.9**:
- Usage check: No code references found (grepped *.cs files)
- Decision: Remove entirely - was likely added for testing but never used
- EmbedIO 3.5.2 is already present and actively used for HTTP serving

**System.ServiceModel.Http + System.ServiceModel.NetTcp**:
- Usage check: No code references to ServiceModel found
- These were client-side WCF packages (System.ServiceModel.* v10.0.652802)
- Decision: Remove - not actually used, possibly transitive dependencies from migration

**MahApps.Metro.IconPacks 6.2.1 → 4.11.0**:
- Usage: iconPacks namespace used in 4 XAML files (PsaControl, UcUserLevelItem, UC_BlacklistEntry, SettingsWindow)
- Uses: PackIconMaterial, PackIconFont Awesome, FontAwesome controls
- Decision: Downgrade to 4.11.0 as recommended by assessment
- Risk: Low - no breaking API changes expected in downgrade

**Microsoft.Xaml.Behaviors.Wpf 1.1.142 → 1.1.39**:
- Decision: Downgrade to 1.1.39 as recommended by assessment
- Risk: Low - XAML behaviors downgrade for compatibility

**WPF-UI 4.3.0 → 2.0.3**:
- Decision: Downgrade to 2.0.3 as recommended by assessment
- Risk: Medium - significant version drop, may affect WPF-UI control usage

**LiveCharts 0.9.7 + LiveCharts.Wpf 0.9.7 → LiveChartsCore.SkiaSharpView.WPF 2.0.0-rc4.5**:
- Usage: 2 files - ApiChart.xaml.cs, ApiMetricsVm.cs
- LiveCharts → LiveChartsCore is a complete rewrite with breaking changes:
  - Namespace change: `LiveCharts.Wpf` → `LiveChartsCore.SkiaSharpView.WPF`
  - Control names: `CartesianChart` → `CartesianChart` (same name, different API)
  - Series API completely different: `LineSeries`, `ColumnSeries` → `ISeries` implementations
  - ViewModel pattern changes
- Decision: Install LiveChartsCore package, defer code migration to Task 04 (API Compatibility)
- Rationale: Chart API migration belongs with other API fixes, not package maintenance

### Migration Strategy

**Phase 1 (This Task)**: Package changes only
1. ✅ Remove NHttp (not used)
2. ✅ Remove System.ServiceModel.Http + NetTcp (not used)
3. ✅ Downgrade MahApps.Metro.IconPacks 6.2.1 → 4.11.0
4. ✅ Downgrade Microsoft.Xaml.Behaviors.Wpf 1.1.142 → 1.1.39
5. ✅ Downgrade WPF-UI 4.3.0 → 2.0.3
6. ✅ Remove LiveCharts + LiveCharts.Wpf
7. ✅ Add LiveChartsCore.SkiaSharpView.WPF 2.0.0-rc4.5

**Phase 2 (Task 04)**: Code migration for LiveCharts
- Migrate ApiChart.xaml XAML markup
- Migrate ApiChart.xaml.cs code-behind
- Migrate ApiMetricsVm.cs ViewModel
- Test charting functionality

**Expected Build State After This Task**:
- Package restore: Success (all packages compatible with .NET 10)
- Build: Fail with MC3074 XAML error in ApiChart.xaml (LiveCharts control not found)
- This is expected - LiveCharts code migration deferred to Task 04
