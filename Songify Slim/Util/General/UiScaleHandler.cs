using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.General;

/// <summary>
/// App-wide UI zoom on top of Windows DPI. WPF layout uses a shared
/// <see cref="ScaleTransform"/> so windows, menus, and popups stay in sync.
/// Window min/current size is grown by the same factor so 4K does not just
/// crop a zoomed 900×500 layout.
/// </summary>
internal static class UiScaleHandler
{
    public const double Min = 1.0;
    public const double Max = 2.0;
    public const double Step = 0.05;
    public const double Default = 1.0;

    private static readonly ScaleTransform Transform = new(Default, Default);
    private static readonly ConditionalWeakTable<Window, WindowScaleState> States = new();
    private static bool _hooked;

    public static double Clamp(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            return Default;

        double snapped = Math.Round(value / Step) * Step;
        return Math.Clamp(snapped, Min, Max);
    }

    public static void Initialize()
    {
        if (_hooked)
            return;

        _hooked = true;
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWindowLoaded));
        EventManager.RegisterClassHandler(typeof(ContextMenu), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnPopupLikeLoaded));
        EventManager.RegisterClassHandler(typeof(ToolTip), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnPopupLikeLoaded));
        EventManager.RegisterClassHandler(typeof(Popup), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnPopupLoaded));

        double scale = Clamp(Settings.UiScale);
        Transform.ScaleX = scale;
        Transform.ScaleY = scale;
    }

    public static void Apply(double scale)
    {
        scale = Clamp(scale);
        Transform.ScaleX = scale;
        Transform.ScaleY = scale;

        if (Application.Current?.Windows == null)
            return;

        foreach (Window window in Application.Current.Windows)
            ApplyToWindow(window, scale);
    }

    public static void ApplyToWindow(Window window, double scale)
    {
        if (window == null)
            return;

        scale = Clamp(scale);
        AssignTransform(window.Content as FrameworkElement);
        ApplyWindowSize(window, scale);
    }

    /// <summary>
    /// Replaces the unscaled min size used by zoom (0 = no minimum).
    /// Call after changing a window's designed min size at runtime.
    /// </summary>
    public static void SetUnscaledMinSize(Window window, double minWidth, double minHeight)
    {
        if (window == null)
            return;

        minWidth = minWidth < 0 ? 0 : minWidth;
        minHeight = minHeight < 0 ? 0 : minHeight;
        WindowScaleState state = States.GetValue(window, static w => new WindowScaleState
        {
            AppliedScale = Default,
            OriginalMinWidth = 0,
            OriginalMinHeight = 0
        });

        state.OriginalMinWidth = minWidth;
        state.OriginalMinHeight = minHeight;
        double scale = state.AppliedScale > 0 ? state.AppliedScale : Clamp(Settings.UiScale);
        window.MinWidth = minWidth <= 0 ? 0 : minWidth * scale;
        window.MinHeight = minHeight <= 0 ? 0 : minHeight * scale;
    }

    public static void SetUnscaledMinWidth(Window window, double minWidth)
    {
        if (window == null)
            return;

        minWidth = minWidth < 0 ? 0 : minWidth;
        WindowScaleState state = States.GetValue(window, static w => new WindowScaleState
        {
            AppliedScale = Default,
            OriginalMinWidth = 0,
            OriginalMinHeight = SafeMin(w.MinHeight)
        });

        state.OriginalMinWidth = minWidth;
        double scale = state.AppliedScale > 0 ? state.AppliedScale : Clamp(Settings.UiScale);
        window.MinWidth = minWidth <= 0 ? 0 : minWidth * scale;
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Loaded bubbles; only handle the window's own Loaded.
        if (sender is Window window && ReferenceEquals(e.OriginalSource, window))
            ApplyToWindow(window, Clamp(Settings.UiScale));
    }

    private static void OnPopupLikeLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
            AssignTransform(fe);
    }

    private static void OnPopupLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Popup popup)
            return;

        popup.Opened -= PopupOnOpened;
        popup.Opened += PopupOnOpened;
        if (popup.Child is FrameworkElement child)
            AssignTransform(child);
    }

    private static void PopupOnOpened(object sender, EventArgs e)
    {
        if (sender is Popup { Child: FrameworkElement child })
            AssignTransform(child);
    }

    private static void AssignTransform(FrameworkElement element)
    {
        if (element == null)
            return;

        if (!ReferenceEquals(element.LayoutTransform, Transform))
            element.LayoutTransform = Transform;
    }

    private static void ApplyWindowSize(Window window, double scale)
    {
        WindowScaleState state = States.GetValue(window, static w => new WindowScaleState
        {
            AppliedScale = Default,
            OriginalMinWidth = SafeMin(w.MinWidth),
            OriginalMinHeight = SafeMin(w.MinHeight)
        });

        if (Math.Abs(state.AppliedScale - scale) < 0.001)
            return;

        double ratio = scale / state.AppliedScale;
        state.AppliedScale = scale;

        if (state.OriginalMinWidth > 0)
            window.MinWidth = state.OriginalMinWidth * scale;
        if (state.OriginalMinHeight > 0)
            window.MinHeight = state.OriginalMinHeight * scale;

        if (window.WindowState != WindowState.Normal)
            return;

        bool autoWidth = window.SizeToContent is SizeToContent.Width or SizeToContent.WidthAndHeight;
        bool autoHeight = window.SizeToContent is SizeToContent.Height or SizeToContent.WidthAndHeight;

        if (!autoWidth)
        {
            double width = double.IsNaN(window.Width) || window.Width <= 0 ? window.ActualWidth : window.Width;
            if (width > 0)
                window.Width = Math.Max(width * ratio, window.MinWidth);
        }

        if (!autoHeight)
        {
            double height = double.IsNaN(window.Height) || window.Height <= 0 ? window.ActualHeight : window.Height;
            if (height > 0)
                window.Height = Math.Max(height * ratio, window.MinHeight);
        }
    }

    private static double SafeMin(double value)
        => double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value >= 10000
            ? 0
            : value;

    private sealed class WindowScaleState
    {
        public double AppliedScale;
        public double OriginalMinWidth;
        public double OriginalMinHeight;
    }
}
