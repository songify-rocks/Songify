using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;

namespace Songify_Slim.Views;

public enum ImportPreviewKind
{
    Cloud,
    Backup
}

/// <summary>
/// Preview of settings differences before import, with per-item selection.
/// </summary>
public partial class Window_CloudImportPreview
{
    public bool IsConfirmed { get; private set; }
    public int DiffCount { get; private set; }
    public IReadOnlyList<string> SelectedPaths { get; private set; } = [];

    private readonly List<ConfigDiffItem> _items = [];

    public Window_CloudImportPreview(Configuration local, Configuration incoming, ImportPreviewKind kind = ImportPreviewKind.Cloud)
    {
        InitializeComponent();
        ThemeHandler.ApplyTheme();
        ApplyKind(kind);
        PopulateDiff(local, incoming, includeCredentials: kind == ImportPreviewKind.Backup);
    }

    private static string Loc(string key, string fallback)
        => Application.Current?.TryFindResource(key) as string ?? fallback;

    private void ApplyKind(ImportPreviewKind kind)
    {
        if (kind == ImportPreviewKind.Backup)
        {
            string title = Loc("window_backupimport_title", "Restore backup");
            Title = title;
            DlgTitleBar.Title = title;
            TbIntro.Text = Loc(
                "window_backupimport_intro",
                "Choose which settings from this backup to overwrite. Unchecked items stay as they are.");
        }
        else
        {
            TbIntro.Text = Loc(
                "window_cloudimport_intro",
                "Choose which cloud settings to overwrite. Unchecked items stay as they are.");
        }
    }

    private void PopulateDiff(Configuration local, Configuration incoming, bool includeCredentials)
    {
        _items.Clear();
        _items.AddRange(ConfigComparer.GetDiffItems(local, incoming, includeCredentials));
        DiffCount = _items.Count;

        foreach (ConfigDiffItem item in _items)
            item.PropertyChanged += DiffItemOnPropertyChanged;

        List<string> permissionWarnings = ConfigComparer.GetPermissionWideningWarnings(local, incoming);
        if (permissionWarnings.Count > 0)
        {
            PermissionWarningBanner.Visibility = Visibility.Visible;
            string header = Loc(
                "window_cloudimport_permission_body",
                "This import widens who can use some Twitch commands or song requests:");
            TbPermissionWarnings.Text = header + "\n• " + string.Join("\n• ", permissionWarnings);
        }
        else
        {
            PermissionWarningBanner.Visibility = Visibility.Collapsed;
            TbPermissionWarnings.Text = "";
        }

        if (_items.Count == 0)
        {
            DiffList.ItemsSource = null;
            BtnImport.IsEnabled = false;
            TbSelectionCount.Text = Loc("window_cloudimport_no_differences", "No differences detected.");
            return;
        }

        DiffList.ItemsSource = ConfigComparer.GroupDiffs(_items);
        RefreshSelectionUi();
    }

    private void DiffItemOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConfigDiffItem.IsSelected))
            RefreshSelectionUi();
    }

    private void RefreshSelectionUi()
    {
        int selected = _items.Count(i => i.IsSelected);
        TbSelectionCount.Text = string.Format(
            Loc("window_import_selected_count", "{0} of {1} selected"),
            selected,
            _items.Count);
        BtnImport.IsEnabled = selected > 0;
    }

    private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (ConfigDiffItem item in _items)
            item.IsSelected = true;
    }

    private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
    {
        foreach (ConfigDiffItem item in _items)
            item.IsSelected = false;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close();
    }

    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        SelectedPaths = _items.Where(i => i.IsSelected).Select(i => i.Path).ToList();
        if (SelectedPaths.Count == 0)
            return;

        IsConfirmed = true;
        Close();
    }
}
