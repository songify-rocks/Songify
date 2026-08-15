using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Views.WPFUI.ViewModels;

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
        ThemeHandler.ApplyTheme();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Localized strings from resources (no Loc in XAML = no designer errors)
        if (BtnClearQueue != null)
        {
            BtnClearQueue.Content = TryFindResource("menu_queue_clear") as string
                                    ?? Properties.Resources.menu_queue_clear;
            BtnClearQueue.ToolTip = BtnClearQueue.Content;
        }
        if (ChkQueueId != null) ChkQueueId.Content = Properties.Resources.window_queue_queue_id;
        if (ChkArtist != null) ChkArtist.Content = Properties.Resources.common_artist;
        if (ChkTitle != null) ChkTitle.Content = Properties.Resources.window_queue_song_request;
        if (ChkLength != null) ChkLength.Content = Properties.Resources.common_length;
        if (ChkRequester != null) ChkRequester.Content = Properties.Resources.common_requester;
        if (ChkActions != null) ChkActions.Content = Properties.Resources.window_queue_actions;

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