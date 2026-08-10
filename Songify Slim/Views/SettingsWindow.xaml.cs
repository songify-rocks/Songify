using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Songify_Slim.Views;

/// <summary>
/// Thin WPF host for <see cref="WPFUI.Controls.SettingsPanel"/> (dialog / fallback entry).
/// Shell navigation uses SettingsPage which hosts the same panel in-process.
/// </summary>
// ReSharper disable once InconsistentNaming
public partial class Window_Settings
{
    public Window_Settings()
    {
        InitializeComponent();
        if (SettingsLanguageNeedsWiderWindow())
            Width = MinWidth = 830;
    }

    private static bool SettingsLanguageNeedsWiderWindow()
    {
        try
        {
            return Util.Configuration.Settings.Language != "en";
        }
        catch
        {
            return false;
        }
    }

    public TextBox LblPreview => Panel.LblPreview;

    public Task SetControls() => Panel.SetControls();
    public Task LoadRewards() => Panel.LoadRewards();
    public Task LoadCommands() => Panel.LoadCommands();
    public Task ResetTwitchConnection() => Panel.ResetTwitchConnection();
    public void SelectTab(string tabName, string elementName = "") => Panel.SelectTab(tabName, elementName);

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        bool allowClose = await Panel.ConfirmCloseAsync();
        if (!allowClose) return;
        Closing -= Window_Closing;
        Dispatcher.BeginInvoke(Close);
    }

    public void Window_LocationChanged(object sender, EventArgs e)
        => Panel.SyncResponseParamsPosition();

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        => Panel.SyncResponseParamsSize();

    private void BtnResponseParams_OnClick(object sender, RoutedEventArgs e)
        => Panel.OpenResponseParams();
}
