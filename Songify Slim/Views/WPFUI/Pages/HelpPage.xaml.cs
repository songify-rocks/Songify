using System.Windows;
using System.Windows.Controls;
using Songify_Slim.Util.General;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class HelpPage : Page
{
    public HelpPage() => InitializeComponent();

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
