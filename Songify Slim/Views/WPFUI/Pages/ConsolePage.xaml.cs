using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using Songify_Slim.ViewModels;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class ConsolePage : Page
{
    public ConsolePage()
    {
        InitializeComponent();

        // Reuse the same document as the legacy console window.
        // FlowDocument can only belong to one RichTextBox, so detach first if needed.
        if (RtbConsole != null && GlobalObjects.ConsoleDocument != null)
        {
            var doc = GlobalObjects.ConsoleDocument;
            if (doc.Parent is RichTextBox other)
            {
                // Detach without losing content.
                other.Document = new FlowDocument();
            }
            RtbConsole.Document = doc;
        }

        // Reuse the same metrics VM as the legacy console window
        if (GlobalObjects.ApiMetrics != null)
            DataContext = GlobalObjects.ApiMetrics;

        Loaded += (_, __) => EnsureChart();
        Unloaded += (_, __) =>
        {
            // Detach so other views/windows can host the shared document later.
            if (RtbConsole != null && ReferenceEquals(RtbConsole.Document, GlobalObjects.ConsoleDocument))
                RtbConsole.Document = new FlowDocument();
        };
    }

    private void EnsureChart()
    {
        if (ApiChartHost?.Content != null)
            return;
        try
        {
            // Reuse the existing chart view (it binds to GlobalObjects.ApiMetrics)
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
    }

    private void BtnClearConsole_Click(object sender, RoutedEventArgs e)
    {
        GlobalObjects.ConsoleDocument?.Blocks?.Clear();
    }

    private void BtnRefreshMetrics_Click(object sender, RoutedEventArgs e)
    {
        // ApiMetricsVm auto-refreshes on timer; this just nudges command requery and UI
        RelayCommand.InvalidateRequerySuggested();
    }
}