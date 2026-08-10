using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ControlzEx.Theming;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Views.WPFUI.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Songify_Slim.Views.WPFUI;

public partial class QueueWindow
{
    private QueueWindowViewModel _viewModel;
    private DispatcherTimer _playPauseTimer;

    public QueueWindow()
    {
        InitializeComponent();
        _viewModel = new QueueWindowViewModel();
        DataContext = _viewModel;
        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Localized strings from resources (no Loc in XAML = no designer errors)
        if (BtnClearQueue != null)
        {
            BtnClearQueue.Content = "Clear Queue";
            BtnClearQueue.ToolTip = "Clear Queue";
        }
        if (ChkQueueId != null) ChkQueueId.Content = Properties.Resources.Window_Queue_QueueId;
        if (ChkArtist != null) ChkArtist.Content = Properties.Resources.bw_cbArtist;
        if (ChkTitle != null) ChkTitle.Content = Properties.Resources.crw_RewardTitle;
        if (ChkLength != null) ChkLength.Content = Properties.Resources.s_Length;
        if (ChkRequester != null) ChkRequester.Content = Properties.Resources.s_Requester;
        if (ChkActions != null) ChkActions.Content = Properties.Resources.WinQueue_Actions;

        GlobalObjects.QueueUpdateQueueWindow();
        _viewModel.LoadColumnVisibility();
        ApplyColumnVisibility();
        _viewModel.RefreshPlayPauseState();

        _playPauseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _playPauseTimer.Tick += (_, __) => _viewModel.RefreshPlayPauseState();
        _playPauseTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _playPauseTimer?.Stop();
        base.OnClosed(e);
    }

    private void ColumnVisibility_Changed(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveColumnVisibility();
        ApplyColumnVisibility();
    }

    private void ApplyColumnVisibility()
    {
        if (QueueDataGrid == null) return;
        ColQueueId.Visibility = _viewModel.ColQueueIdVisible ? Visibility.Visible : Visibility.Collapsed;
        ColArtist.Visibility = _viewModel.ColArtistVisible ? Visibility.Visible : Visibility.Collapsed;
        ColTitle.Visibility = _viewModel.ColTitleVisible ? Visibility.Visible : Visibility.Collapsed;
        ColLength.Visibility = _viewModel.ColLengthVisible ? Visibility.Visible : Visibility.Collapsed;
        ColRequester.Visibility = _viewModel.ColRequesterVisible ? Visibility.Visible : Visibility.Collapsed;
        ColActions.Visibility = _viewModel.ColActionsVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        e.Cancel = true;
    }

    private void ContextMenu_RemoveFromQueue_Click(object sender, RoutedEventArgs e)
    {
        var item = _viewModel.SelectedQueueItem;
        if (item == null) return;
        _viewModel.SkipCommand.Execute(item);
    }
}