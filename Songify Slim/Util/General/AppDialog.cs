using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Songify_Slim.Views.WPFUI;

namespace Songify_Slim.Util.General;

/// <summary>App-owned dialog result (replaces MahApps MessageDialogResult).</summary>
public enum AppDialogResult
{
    None = 0,
    Primary = 1,
    Secondary = 2,
    Affirmative = Primary,
    Negative = Secondary,
    Canceled = None,
    FirstAuxiliary = Secondary,
    SecondAuxiliary = Secondary
}

/// <summary>Dialog button layout (replaces MahApps MessageDialogStyle).</summary>
public enum AppDialogStyle
{
    Primary = 0,
    PrimaryAndSecondary = 1,
    Affirmative = Primary,
    AffirmativeAndNegative = PrimaryAndSecondary,
    AffirmativeAndNegativeAndSingleAuxiliary = PrimaryAndSecondary
}

/// <summary>Optional dialog chrome (replaces MahApps MetroDialogSettings).</summary>
public sealed class AppDialogSettings
{
    public string PrimaryButtonText { get; set; } = "OK";
    public string SecondaryButtonText { get; set; } = "Cancel";
    public bool AnimateShow { get; set; }
    public bool AnimateHide { get; set; }

    public string AffirmativeButtonText
    {
        get => PrimaryButtonText;
        set => PrimaryButtonText = value;
    }

    public string NegativeButtonText
    {
        get => SecondaryButtonText;
        set => SecondaryButtonText = value;
    }
}

public static class AppDialog
{
    public static Task<AppDialogResult> ShowAsync(
        string title,
        string message,
        AppDialogStyle style = AppDialogStyle.Primary,
        AppDialogSettings settings = null)
    {
        if (AppShellBridge.Current != null)
            return AppShellBridge.Current.ShowMessageAsync(title, message, style, settings);

        return ShowMessageBoxAsync(title, message, style, settings);
    }

    /// <summary>Themed Fluent dialog (replaces Win32 MessageBox).</summary>
    public static Task<AppDialogResult> ShowMessageBoxAsync(
        string title,
        string message,
        AppDialogStyle style = AppDialogStyle.Primary,
        AppDialogSettings settings = null)
    {
        settings ??= new AppDialogSettings();

        if (Application.Current?.Dispatcher == null)
            return Task.FromResult(AppDialogResult.None);

        if (!Application.Current.Dispatcher.CheckAccess())
        {
            return Application.Current.Dispatcher.InvokeAsync(() =>
                ShowThemedDialog(title, message, style, settings)).Task;
        }

        return Task.FromResult(ShowThemedDialog(title, message, style, settings));
    }

    private static AppDialogResult ShowThemedDialog(
        string title,
        string message,
        AppDialogStyle style,
        AppDialogSettings settings)
    {
        var dialog = new MessageDialogWindow(title, message, style, settings);

        Window owner = Application.Current?.Windows.OfType<Window>()
            .FirstOrDefault(w => w.IsActive && w is not MessageDialogWindow);
        owner ??= Application.Current?.MainWindow is { IsLoaded: true } main ? main : null;
        if (owner != null && !ReferenceEquals(owner, dialog))
            dialog.Owner = owner;

        bool? closed = dialog.ShowDialog();
        if (dialog.Result != AppDialogResult.None)
            return dialog.Result;

        return closed == true ? AppDialogResult.Primary : AppDialogResult.Secondary;
    }
}
