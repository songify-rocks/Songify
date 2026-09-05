using System;
using System.Windows;
using Songify_Slim.Util.General;
using Songify_Slim.Views.WPFUI.Pages;

namespace Songify_Slim.Views.WPFUI;

public partial class QueueWindow
{
    public static QueueWindow Current { get; private set; }

    public static bool IsOpen => Current != null;

    public static event Action DetachedChanged;

    public QueueWindow()
    {
        InitializeComponent();
        MinWidth = 0;
        MinHeight = 0;
        ThemeHandler.ApplyTheme();
        QueueHost.Navigate(new QueuePage());
        Closed += OnClosed;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        MinWidth = 0;
        MinHeight = 0;
        UiScaleHandler.SetUnscaledMinSize(this, 0, 0);
    }

    public static void ShowOrActivate()
    {
        if (Current != null)
        {
            if (Current.WindowState == WindowState.Minimized)
                Current.WindowState = WindowState.Normal;
            Current.Show();
            Current.Activate();
            Current.Focus();
            return;
        }

        GlobalObjects.DetachQueue = true;
        Current = new QueueWindow();
        DetachedChanged?.Invoke();
        Current.Show();
    }

    public static void CloseIfOpen()
    {
        Current?.Close();
    }

    private void OnClosed(object sender, EventArgs e)
    {
        Closed -= OnClosed;
        if (ReferenceEquals(Current, this))
            Current = null;
        GlobalObjects.DetachQueue = false;
        DetachedChanged?.Invoke();
    }
}
