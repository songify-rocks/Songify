# 04.02-uwp-api-removal: Remove UWP API dependencies

# 04.02-uwp-api-removal: Remove UWP API dependencies

## Objective
Remove or replace UWP (Windows Runtime) API calls that are not available in .NET 10 WPF.

## Scope
**Affected files** (from Task 01 errors):
- `Util/General/WebServer.cs` - Windows.ApplicationModel.Resources.Core
- `Util/General/ThumbnailConverter.cs` - Windows.Graphics.Imaging, Windows.Storage.Streams, IRandomAccessStreamReference
- `Util/Songify/SongFetcher.cs` - Windows.Media.Control, Windows.Storage.Streams, GlobalSystemMediaTransportControls*, IRandomAccessStreamReference
- `Views/MainWindow.xaml.cs` - Windows.Media.Control, GlobalSystemMediaTransportControls*, ToastNotificationActivatedEventArgsCompat
- `Views/Window_Queue.xaml.cs` - Windows.UI.Xaml.Controls.Primitives
- `Views/Window_Userlist.xaml.cs` - Windows.UI.Xaml.Controls.Primitives
- `UserControls/UC_TwitchReward.xaml.cs` - Windows.UI.Xaml.Controls.Primitives
- `UserControls/UcUserLevelItem.xaml.cs` - Windows.UI.Composition

**API categories**:
1. **Windows.Media.Control** (GlobalSystemMediaTransportControls) - Media session management
2. **Windows.Storage.Streams** (IRandomAccessStreamReference) - UWP stream handling
3. **Windows.Graphics.Imaging** - UWP image decoding
4. **Windows.UI.*** - UWP UI primitives
5. **Windows.ApplicationModel** - UWP app lifecycle
6. **Toast notifications** - Microsoft.Toolkit.Uwp.Notifications APIs

## Strategy
- **GlobalSystemMediaTransportControls**: Check if Windows.Media.Control exists in .NET 10, or replace with alternative
- **IRandomAccessStreamReference**: Convert to standard .NET Stream
- **Windows.UI.* primitives**: Replace with WPF equivalents
- **Windows.Graphics.Imaging**: Use System.Drawing or SkiaSharp
- **Toast notifications**: Verify Microsoft.Toolkit.Uwp.Notifications 7.1.3 compatibility or update API usage

## Done when
- All Windows.* namespace errors resolved
- All GlobalSystemMediaTransportControls errors resolved
- Toast notification code compiles
- Build progresses to System.Web.UI errors
