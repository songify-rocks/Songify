# 04.03-systemweb-removal: Remove System.Web.UI dependencies

# 04.03-systemweb-removal: Remove System.Web.UI dependencies

## Objective
Remove System.Web.UI.WebControls namespace usage - not available in .NET 10.

## Scope
**Affected files**:
- `Models/Twitch/TwitchPollSettings.cs`
- `Views/Window_Queue.xaml.cs`
- `Util/Songify/SongFetcher.cs`

All three files have `using System.Web.UI.WebControls;` which doesn't exist in .NET 10.

## Strategy
1. Check if System.Web.UI.WebControls types are actually used in code
2. If unused: Remove the using statement
3. If used: Identify which WebControls types and replace with WPF equivalents or remove functionality

## Done when
- All System.Web.UI namespace errors resolved
- Files compile successfully
- Build completes with zero errors (or reveals final set of errors)
