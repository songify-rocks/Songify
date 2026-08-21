using System;
using System.Linq;
using System.Windows;
using Songify_Slim.Util.General;

namespace Songify_Slim.Views.WPFUI;

public partial class ConsoleWindow
{
    public static ConsoleWindow Current { get; private set; }

    public static bool IsOpen => Current != null;

    public static event Action DetachedChanged;

    public ConsoleWindow()
    {
        InitializeComponent();
        ThemeHandler.ApplyTheme();
        Closed += OnClosed;
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

        GlobalObjects.DetachConsole = true;
        DetachedChanged?.Invoke();

        Current = new ConsoleWindow();
        Current.Show();
    }

    private void OnClosed(object sender, EventArgs e)
    {
        Closed -= OnClosed;
        if (ReferenceEquals(Current, this))
            Current = null;
        GlobalObjects.DetachConsole = false;
        DetachedChanged?.Invoke();
    }
}
