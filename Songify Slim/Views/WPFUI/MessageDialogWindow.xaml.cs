using System.Windows;
using Songify_Slim.Util.General;

namespace Songify_Slim.Views.WPFUI;

public partial class MessageDialogWindow
{
    public AppDialogResult Result { get; private set; } = AppDialogResult.None;

    public MessageDialogWindow(
        string title,
        string message,
        AppDialogStyle style,
        AppDialogSettings settings)
    {
        InitializeComponent();
        ThemeHandler.ApplyTheme();

        settings ??= new AppDialogSettings();
        string windowTitle = string.IsNullOrWhiteSpace(title) ? "Songify" : title;

        Title = windowTitle;
        DlgTitleBar.Title = windowTitle;
        TxtTitle.Text = windowTitle;
        TxtMessage.Text = message ?? "";

        BtnPrimary.Content = string.IsNullOrWhiteSpace(settings.PrimaryButtonText)
            ? "OK"
            : settings.PrimaryButtonText;

        if (style == AppDialogStyle.Primary)
        {
            BtnSecondary.Visibility = Visibility.Collapsed;
        }
        else
        {
            BtnSecondary.Content = string.IsNullOrWhiteSpace(settings.SecondaryButtonText)
                ? "Cancel"
                : settings.SecondaryButtonText;
            BtnSecondary.Visibility = Visibility.Visible;
        }
    }

    private void BtnPrimary_OnClick(object sender, RoutedEventArgs e)
    {
        Result = AppDialogResult.Primary;
        DialogResult = true;
        Close();
    }

    private void BtnSecondary_OnClick(object sender, RoutedEventArgs e)
    {
        Result = AppDialogResult.Secondary;
        DialogResult = false;
        Close();
    }
}
