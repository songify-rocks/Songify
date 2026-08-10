using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Songify_Slim.Util.General;
using Songify_Slim.Views.WPFUI.ViewModels;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class QueuePage : Page
{
    private QueueWindowViewModel _viewModel;
    private DispatcherTimer _playPauseTimer;

    public QueuePage()
    {
        InitializeComponent();
        _viewModel = new QueueWindowViewModel();
        DataContext = _viewModel;
        Loaded += QueuePage_Loaded;
    }

    private void QueueScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta == 0) return;
        e.Handled = true;
        var scrollViewer = (ScrollViewer)sender;
        const double scrollAmount = 24; // pixels per wheel notch
        double newOffset = scrollViewer.VerticalOffset - (Math.Sign(e.Delta) * scrollAmount);
        newOffset = Math.Max(0, Math.Min(newOffset, scrollViewer.ScrollableHeight));
        scrollViewer.ScrollToVerticalOffset(newOffset);
    }

    private void QueuePage_Loaded(object sender, RoutedEventArgs e)
    {
        BtnClearQueue.Content = "Clear Queue";
        GlobalObjects.QueueUpdateQueueWindow();
        _viewModel.RefreshPlayPauseState(); // refresh so "now playing" row shows (queue + CurrentSong fallback)
        _playPauseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _playPauseTimer.Tick += (_, __) => _viewModel.RefreshPlayPauseState();
        _playPauseTimer.Start();
    }
}