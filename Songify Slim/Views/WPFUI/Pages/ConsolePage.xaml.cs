using System;
using System.Windows;
using Songify_Slim.Util.General;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class ConsolePage
{
    public ConsolePage()
    {
        InitializeComponent();
        Loaded += ConsolePage_Loaded;
        Unloaded += ConsolePage_Unloaded;
        IsVisibleChanged += ConsolePage_IsVisibleChanged;
    }

    private void ConsolePage_Loaded(object sender, RoutedEventArgs e)
    {
        ConsoleWindow.DetachedChanged += OnDetachedChanged;
        UpdateDetachedUi();
    }

    private void ConsolePage_Unloaded(object sender, RoutedEventArgs e)
    {
        ConsoleWindow.DetachedChanged -= OnDetachedChanged;
        ConsoleHost?.ReleaseDocument();
    }

    private void ConsolePage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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

    private void BtnDetachConsole_Click(object sender, RoutedEventArgs e)
    {
        ConsoleWindow.ShowOrActivate();
    }

    private void BtnShowConsoleWindow_Click(object sender, RoutedEventArgs e)
    {
        ConsoleWindow.ShowOrActivate();
    }
}
