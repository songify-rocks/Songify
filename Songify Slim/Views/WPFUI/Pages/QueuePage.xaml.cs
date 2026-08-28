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
        Unloaded += QueuePage_Unloaded;
    }

    private bool IsFloatingHost => Window.GetWindow(this) is QueueWindow;

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
        QueueWindow.DetachedChanged += OnDetachedChanged;
        ApplyHostLayout();
        UpdateDetachedUi();
        GlobalObjects.QueueUpdateQueueWindow();
        _viewModel.RefreshPlayPauseState(); // refresh so "now playing" row shows (queue + CurrentSong fallback)
        StopPlayPauseTimer();
        _playPauseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _playPauseTimer.Tick += OnPlayPauseTick;
        _playPauseTimer.Start();
    }

    private void QueuePage_Unloaded(object sender, RoutedEventArgs e)
    {
        QueueWindow.DetachedChanged -= OnDetachedChanged;
        StopPlayPauseTimer();
    }

    private void OnPlayPauseTick(object sender, EventArgs e) => _viewModel.RefreshPlayPauseState();

    private void StopPlayPauseTimer()
    {
        if (_playPauseTimer == null)
            return;
        _playPauseTimer.Tick -= OnPlayPauseTick;
        _playPauseTimer.Stop();
        _playPauseTimer = null;
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

    private void ApplyHostLayout()
    {
        bool floating = IsFloatingHost;
        if (RootGrid != null)
            RootGrid.Margin = floating ? new Thickness(8) : new Thickness(24);
        if (HeaderGrid != null)
            HeaderGrid.Margin = floating ? new Thickness(0, 0, 0, 8) : new Thickness(0, 0, 0, 16);
        if (TxtQueueTitle != null)
            TxtQueueTitle.Visibility = floating ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateDetachedUi()
    {
        bool floating = IsFloatingHost;
        bool detached = !floating && (QueueWindow.IsOpen || GlobalObjects.DetachQueue);

        if (NowPlayingBorder != null)
            NowPlayingBorder.Visibility = detached ? Visibility.Collapsed : Visibility.Visible;
        if (QueueScrollViewer != null)
            QueueScrollViewer.Visibility = detached ? Visibility.Collapsed : Visibility.Visible;
        if (CardDetached != null)
            CardDetached.Visibility = detached ? Visibility.Visible : Visibility.Collapsed;
        if (BtnDetachQueue != null)
            BtnDetachQueue.Visibility = (detached || floating) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BtnDetachQueue_Click(object sender, RoutedEventArgs e)
    {
        QueueWindow.ShowOrActivate();
    }

    private void BtnShowQueueWindow_Click(object sender, RoutedEventArgs e)
    {
        QueueWindow.ShowOrActivate();
    }
}
