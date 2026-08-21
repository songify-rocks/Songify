using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Songify_Slim.Util.General;
using Songify_Slim.ViewModels;
using Songify_Slim.Views;

namespace Songify_Slim.Views.WPFUI.Controls;

public partial class ConsoleView
{
    private static readonly FontFamily ConsoleFont = new("Consolas");
    private const double ConsoleFontSize = 12;
    private bool _pendingScrollToEnd;

    public bool IsFloatingHost
    {
        get => (bool)GetValue(IsFloatingHostProperty);
        set => SetValue(IsFloatingHostProperty, value);
    }

    public static readonly DependencyProperty IsFloatingHostProperty =
        DependencyProperty.Register(nameof(IsFloatingHost), typeof(bool), typeof(ConsoleView),
            new PropertyMetadata(false));

    public ConsoleView()
    {
        InitializeComponent();

        if (GlobalObjects.ApiMetrics != null)
            DataContext = GlobalObjects.ApiMetrics;

        Loaded += ConsoleView_Loaded;
        Unloaded += ConsoleView_Unloaded;

        if (RtbConsole != null)
        {
            RtbConsole.IsVisibleChanged += RtbConsole_IsVisibleChanged;
            RtbConsole.SizeChanged += RtbConsole_SizeChanged;
        }
    }

    public void TryAttach()
    {
        if (GlobalObjects.DetachConsole && !IsFloatingHost)
            return;
        AttachConsoleDocument();
        EnsureChart();
        RequestScrollConsoleToEnd();
    }

    public void ReleaseDocument()
    {
        if (RtbConsole != null && ReferenceEquals(RtbConsole.Document, GlobalObjects.ConsoleDocument))
            RtbConsole.Document = new FlowDocument();
    }

    private void ConsoleView_Loaded(object sender, RoutedEventArgs e)
    {
        TryAttach();
    }

    private void ConsoleView_Unloaded(object sender, RoutedEventArgs e)
    {
        ReleaseDocument();
    }

    private void AttachConsoleDocument()
    {
        if (RtbConsole == null || GlobalObjects.ConsoleDocument == null)
            return;

        FlowDocument doc = GlobalObjects.ConsoleDocument;
        if (!ReferenceEquals(RtbConsole.Document, doc))
        {
            if (doc.Parent is RichTextBox other)
                other.Document = new FlowDocument();

            RtbConsole.Document = doc;
        }

        ApplyConsoleTypography(doc);
    }

    private static void ApplyConsoleTypography(FlowDocument doc)
    {
        if (doc == null)
            return;

        doc.FontFamily = ConsoleFont;
        doc.FontSize = ConsoleFontSize;
        doc.PagePadding = new Thickness(6);

        foreach (Block block in doc.Blocks)
        {
            block.FontFamily = ConsoleFont;
            block.FontSize = ConsoleFontSize;
        }
    }

    private void RequestScrollConsoleToEnd()
    {
        _pendingScrollToEnd = true;
        TryScrollConsoleToEnd();
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, TryScrollConsoleToEnd);
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, TryScrollConsoleToEnd);
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, TryScrollConsoleToEnd);
    }

    private void TryScrollConsoleToEnd()
    {
        if (!_pendingScrollToEnd || RtbConsole == null)
            return;

        if (!RtbConsole.IsVisible || RtbConsole.ActualHeight <= 0)
            return;

        ApplyConsoleTypography(RtbConsole.Document);

        FlowDocument doc = RtbConsole.Document;
        if (doc?.Blocks.LastBlock != null)
            doc.Blocks.LastBlock.BringIntoView();

        try
        {
            RtbConsole.CaretPosition = doc?.ContentEnd;
        }
        catch
        {
            // ignored — caret can fail while the visual tree is still building
        }

        RtbConsole.UpdateLayout();
        RtbConsole.ScrollToEnd();

        ScrollViewer viewer = FindScrollViewer(RtbConsole);
        if (viewer == null)
            return;

        viewer.UpdateLayout();
        viewer.ScrollToBottom();
        viewer.ScrollToVerticalOffset(Math.Max(0, viewer.ExtentHeight - viewer.ViewportHeight));

        if (viewer.ExtentHeight > 0 || doc == null || !doc.Blocks.Any())
            _pendingScrollToEnd = false;
    }

    private void RtbConsole_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (RtbConsole.IsVisible)
            RequestScrollConsoleToEnd();
    }

    private void RtbConsole_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_pendingScrollToEnd && e.NewSize.Height > 0)
            TryScrollConsoleToEnd();
    }

    private static ScrollViewer FindScrollViewer(DependencyObject root)
    {
        if (root == null)
            return null;

        if (root is ScrollViewer sv)
            return sv;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            ScrollViewer child = FindScrollViewer(VisualTreeHelper.GetChild(root, i));
            if (child != null)
                return child;
        }

        return null;
    }

    private void EnsureChart()
    {
        if (ApiChartHost?.Content != null)
            return;
        try
        {
            ApiChartHost.Content = new ApiChart { DataContext = GlobalObjects.ApiMetrics };
        }
        catch (Exception)
        {
            // If chart dependencies aren't available, just leave it empty
        }
    }

    private void RtbConsole_TextChanged(object sender, TextChangedEventArgs e)
    {
        RtbConsole?.ScrollToEnd();
        ScrollViewer viewer = FindScrollViewer(RtbConsole);
        viewer?.ScrollToBottom();
    }

    private void BtnClearConsole_Click(object sender, RoutedEventArgs e)
    {
        GlobalObjects.ConsoleDocument?.Blocks?.Clear();
    }

    private void BtnRefreshMetrics_Click(object sender, RoutedEventArgs e)
    {
        RelayCommand.InvalidateRequerySuggested();
    }
}
