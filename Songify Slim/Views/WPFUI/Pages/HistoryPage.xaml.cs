using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Views.WPFUI.ViewModels;

namespace Songify_Slim.Views.WPFUI.Pages;

public partial class HistoryPage : Page
{
    private HistoryViewModel _viewModel;
    private FileSystemWatcher _watcher;

    public HistoryPage()
    {
        InitializeComponent();
        _viewModel = new HistoryViewModel();
        DataContext = _viewModel;
        Loaded += HistoryPage_Loaded;
        Unloaded += HistoryPage_Unloaded;
    }

    private async void HistoryPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (TxtTitle != null)
            TxtTitle.Text = Properties.Resources.window_history_title;

        Window owner = Window.GetWindow(this);
        await HistoryStore.MigrateLegacyIfNeededAsync(owner);

        _viewModel.ApplySettings();
        _viewModel.LoadFile();

        try
        {
            string dir = Path.GetDirectoryName(_viewModel.HistoryPath);
            if (string.IsNullOrEmpty(dir)) return;
            _watcher?.Dispose();
            _watcher = new FileSystemWatcher
            {
                Path = dir,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(_viewModel.HistoryPath),
                EnableRaisingEvents = true
            };
            _watcher.Changed += (s, args) =>
            {
                System.Threading.Thread.Sleep(500);
                _viewModel.LoadFromFile();
            };
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private void HistoryPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}
