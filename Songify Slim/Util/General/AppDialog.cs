using System.Threading.Tasks;

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

    public static Task<AppDialogResult> ShowMessageBoxAsync(
        string title,
        string message,
        AppDialogStyle style = AppDialogStyle.Primary,
        AppDialogSettings settings = null)
    {
        settings ??= new AppDialogSettings();
        System.Windows.MessageBoxButton buttons = style == AppDialogStyle.Primary
            ? System.Windows.MessageBoxButton.OK
            : System.Windows.MessageBoxButton.YesNo;

        System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
            message,
            title ?? "Songify",
            buttons);

        AppDialogResult mapped = result is System.Windows.MessageBoxResult.Yes or System.Windows.MessageBoxResult.OK
            ? AppDialogResult.Primary
            : AppDialogResult.Secondary;
        return Task.FromResult(mapped);
    }
}
