using System.Windows;
using System.Windows.Controls;
using Songify_Slim.Util.General;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class HelpPage : Page
{
    public HelpPage()
    {
        InitializeComponent();
        Loaded += HelpPage_Loaded;
        Unloaded += HelpPage_Unloaded;
        IsVisibleChanged += HelpPage_IsVisibleChanged;
    }

    private void HelpPage_Loaded(object sender, RoutedEventArgs e)
    {
        ConsoleWindow.DetachedChanged += OnDetachedChanged;
        UpdateDetachedUi();
    }

    private void HelpPage_Unloaded(object sender, RoutedEventArgs e)
    {
        ConsoleWindow.DetachedChanged -= OnDetachedChanged;
        ConsoleHost?.ReleaseDocument();
    }

    private void HelpPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            UpdateDetachedUi();
        else
            ConsoleHost?.ReleaseDocument();
    }

    private void OnDetachedChanged()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(UpdateDetachedUi);
            return;
        }

        UpdateDetachedUi();
    }

    private void UpdateDetachedUi()
    {
        bool detached = ConsoleWindow.IsOpen || GlobalObjects.DetachConsole;
        if (ConsoleHost != null)
        {
            ConsoleHost.Visibility = detached ? Visibility.Collapsed : Visibility.Visible;
            if (detached)
                ConsoleHost.ReleaseDocument();
            else if (IsVisible)
                ConsoleHost.TryAttach();
        }

        if (CardDetached != null)
            CardDetached.Visibility = detached ? Visibility.Visible : Visibility.Collapsed;
        if (BtnDetachConsole != null)
            BtnDetachConsole.Visibility = detached ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BtnDetachConsole_Click(object sender, RoutedEventArgs e) =>
        ConsoleWindow.ShowOrActivate();

    private void BtnShowConsoleWindow_Click(object sender, RoutedEventArgs e) =>
        ConsoleWindow.ShowOrActivate();

    private void BtnPatchNotes_Click(object sender, RoutedEventArgs e) => AppActions.OpenPatchNotes();

    private async void BtnSetupWizard_Click(object sender, RoutedEventArgs e)
    {
        Window owner = Window.GetWindow(this);
        if (owner == null)
            return;
        bool startTour = await GuidedSetup.ShowWizardAsync(owner);
        OverviewPage.RefreshChecklist();
        if (startTour && owner is ShellWindow shell)
            await shell.StartSetupTourAsync();
    }

    private void BtnFaq_Click(object sender, RoutedEventArgs e) => AppActions.OpenFaq();

    private void BtnGitHub_Click(object sender, RoutedEventArgs e) => AppActions.OpenGitHubIssues();

    private void BtnDiscord_Click(object sender, RoutedEventArgs e) => AppActions.OpenDiscord();

    private void BtnLogFolder_Click(object sender, RoutedEventArgs e) => AppActions.OpenLogFolder();

    private void BtnAppFolder_Click(object sender, RoutedEventArgs e) => AppActions.OpenAppFolder();

    private void BtnCheckUpdates_Click(object sender, RoutedEventArgs e) => AppActions.CheckForUpdates();
}
