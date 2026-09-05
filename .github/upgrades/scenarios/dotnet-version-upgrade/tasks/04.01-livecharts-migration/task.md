# 04.01-livecharts-migration: Migrate LiveCharts to LiveChartsCore

# 04.01-livecharts-migration: Migrate LiveCharts to LiveChartsCore

## Objective
Migrate charting functionality from legacy LiveCharts 0.9.7 to modern LiveChartsCore 2.0.0-rc4.5.

## Scope
**Files to modify**:
- `Views/ApiChart.xaml` - XAML namespace and control declarations
- `Views/ApiChart.xaml.cs` - Code-behind chart initialization
- `Views/ApiMetricsVm.cs` - ViewModel with Series/Values properties

**Migration changes**:
1. XAML namespace: `clr-namespace:LiveCharts.Wpf;assembly=LiveCharts.Wpf` → `http://livecharts.dev/livecharts2/xaml`
2. Control name may stay `CartesianChart` but with different properties
3. Series types: `LineSeries`/`ColumnSeries` → `LineSeries<T>`/`ColumnSeries<T>` with generics
4. ViewModel: Update chart data binding pattern

## Done when
- ApiChart.xaml compiles without MC3074 errors
- ApiChart.xaml.cs compiles
- ApiMetricsVm.cs compiles
- Build progresses past LiveCharts errors to reveal next set of errors
