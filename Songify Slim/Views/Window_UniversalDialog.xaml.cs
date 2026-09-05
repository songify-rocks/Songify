using System.Windows;
using Songify_Slim.Models.Responses;
using Songify_Slim.UserControls;
using Songify_Slim.Util.General;

namespace Songify_Slim.Views;

/// <summary>Fluent dialog hosting a full PSA / notification message.</summary>
public partial class WindowUniversalDialog
{
    public WindowUniversalDialog(Psa psa, string title)
    {
        InitializeComponent();
        ThemeHandler.ApplyTheme();
        Title = string.IsNullOrWhiteSpace(title)
            ? TryFindResource("window_universaldialog_title") as string ?? "Notification"
            : title;
        if (DlgTitleBar != null)
            DlgTitleBar.Title = Title;
        ContentControl.Content = new PsaControl(psa, byPassLimit: true);
    }

    private void BtnClose_OnClick(object sender, RoutedEventArgs e) => Close();
}
