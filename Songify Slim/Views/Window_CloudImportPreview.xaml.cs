using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    public Configuration SelectedConfiguration { get; private set; }

    private readonly List<ConfigDiffItem> _items = [];
    private readonly bool _includeCredentials;
    private Configuration _local;
    private Configuration _incoming;
    private List<CloudSettingsRevision> _cloudRevisions;
    private bool _suppressRevisionChange;
    private int _revisionDecodeGeneration;

    public Window_CloudImportPreview(Configuration local, Configuration incoming, ImportPreviewKind kind = ImportPreviewKind.Cloud)
        : this(local, incoming, kind, null)
    {
    }

    internal Window_CloudImportPreview(
        Configuration local,
        Configuration incoming,
        ImportPreviewKind kind,
        IReadOnlyList<CloudSettingsRevision> cloudRevisions)
    {
        InitializeComponent();
        ThemeHandler.ApplyTheme();
        Closed += OnClosed;

        _local = local;
        _incoming = incoming;
        SelectedConfiguration = incoming;
        _includeCredentials = kind == ImportPreviewKind.Backup;
        ApplyKind(kind);
        ApplyRevisionPicker(kind, cloudRevisions);

        if (incoming == null && HasTooNewSelection())
            ShowSchemaTooNew();
        else
            PopulateDiff(local, incoming, _includeCredentials);
    }

    internal void ReleaseCloudRevisions()
    {
        _cloudRevisions?.Clear();
        _cloudRevisions = null;
        _suppressRevisionChange = true;
        if (CbxRevisions == null)
            return;
        CbxRevisions.ItemsSource = null;
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
            return;
        }

        TbIntro.Text = Loc(
            "window_cloudimport_intro",
            "Choose which cloud settings to overwrite. Unchecked items stay as they are.");
        TbRevisionLabel.Text = Loc("window_cloudimport_revision_label", "Saved version");
        TbSchemaWarning.Text = Loc(
            "window_cloudimport_schema_too_new",
            "This cloud save was created by a newer Songify version. Update Songify to restore it.");
    }

    private void ApplyRevisionPicker(ImportPreviewKind kind, IReadOnlyList<CloudSettingsRevision> cloudRevisions)
    {
        if (kind != ImportPreviewKind.Cloud || cloudRevisions == null || cloudRevisions.Count == 0)
        {
            RevisionPickerPanel.Visibility = Visibility.Collapsed;
            return;
        }

        _cloudRevisions = [.. cloudRevisions];
        List<CloudRevisionChoice> choices = _cloudRevisions
            .Select(revision => new CloudRevisionChoice
            {
                Revision = revision,
                Label = FormatRevisionLabel(revision)
            })
            .ToList();

        _suppressRevisionChange = true;
        CbxRevisions.ItemsSource = choices;
        CbxRevisions.SelectedIndex = 0;
        _suppressRevisionChange = false;
        RevisionPickerPanel.Visibility = Visibility.Visible;
    }

    internal async Task ReloadCloudRevisionsAsync(IReadOnlyList<CloudSettingsRevision> revisions)
    {
        ApplyRevisionPicker(ImportPreviewKind.Cloud, revisions);
        CloudSettingsRevision selected = _cloudRevisions?.Count > 0 ? _cloudRevisions[0] : null;
        if (selected == null)
        {
            _incoming = null;
            SelectedConfiguration = null;
            PopulateDiff(_local, null, _includeCredentials);
            return;
        }

        if (ConfigHandler.IsCloudRevisionTooNew(selected))
        {
            _incoming = null;
            SelectedConfiguration = null;
            ShowSchemaTooNew();
            return;
        }

        int generation = ++_revisionDecodeGeneration;
        Configuration decoded = await Task.Run(() => ConfigHandler.DecodeCloudRevisionSettings(selected))
            .ConfigureAwait(true);
        if (generation != _revisionDecodeGeneration || _local == null || !IsLoaded)
            return;

        _incoming = decoded;
        SelectedConfiguration = decoded;
        PopulateDiff(_local, decoded, _includeCredentials);
    }

    private static string FormatRevisionLabel(CloudSettingsRevision revision)
    {
        if (revision == null)
            return "";

        DateTime created = revision.CreatedAt;
        if (created != default)
        {
            if (created.Kind == DateTimeKind.Unspecified)
                created = DateTime.SpecifyKind(created, DateTimeKind.Utc);
            return created.ToLocalTime().ToString("g");
        }

        return revision.Id > 0
            ? $"#{revision.Id}"
            : Loc("window_cloudimport_latest_revision", "Latest save");
    }

    private async void CbxRevisions_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressRevisionChange)
            return;
        if (CbxRevisions.SelectedItem is not CloudRevisionChoice choice)
            return;

        int generation = ++_revisionDecodeGeneration;
        CloudSettingsRevision revision = choice.Revision;
        if (ConfigHandler.IsCloudRevisionTooNew(revision))
        {
            Logger.Warning(LogSource.Api,
                $"Cloud restore: schema version {revision.SchemaVersion} is newer than {ConfigHandler.CloudSettingsSchemaVersion}. Update Songify.");
            if (generation != _revisionDecodeGeneration)
                return;
            _incoming = null;
            SelectedConfiguration = null;
            ShowSchemaTooNew();
            return;
        }

        SchemaWarningBanner.Visibility = Visibility.Collapsed;
        BtnImport.IsEnabled = false;
        Configuration decoded = await Task.Run(() => ConfigHandler.DecodeCloudRevisionSettings(revision))
            .ConfigureAwait(true);
        if (generation != _revisionDecodeGeneration || _local == null || !IsLoaded)
            return;

        if (decoded == null)
        {
            _incoming = null;
            SelectedConfiguration = null;
            ClearDiff();
            DiffCount = 0;
            BtnImport.IsEnabled = false;
            TbSelectionCount.Text = Loc(
                "window_cloudimport_revision_invalid",
                "This saved version could not be read.");
            return;
        }

        _incoming = decoded;
        SelectedConfiguration = decoded;
        PopulateDiff(_local, decoded, _includeCredentials);
    }

    private bool HasTooNewSelection()
        => CbxRevisions.SelectedItem is CloudRevisionChoice choice &&
           ConfigHandler.IsCloudRevisionTooNew(choice.Revision);

    private void ShowSchemaTooNew()
    {
        SchemaWarningBanner.Visibility = Visibility.Visible;
        PermissionWarningBanner.Visibility = Visibility.Collapsed;
        TbPermissionWarnings.Text = "";
        ClearDiff();
        DiffCount = 0;
        BtnImport.IsEnabled = false;
        TbSelectionCount.Text = "";
    }

    private void PopulateDiff(Configuration local, Configuration incoming, bool includeCredentials)
    {
        SchemaWarningBanner.Visibility = Visibility.Collapsed;
        ClearDiff();

        if (incoming == null)
        {
            DiffCount = 0;
            BtnImport.IsEnabled = false;
            TbSelectionCount.Text = Loc("window_cloudimport_no_differences", "No differences detected.");
            return;
        }

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

    private void ClearDiff()
    {
        foreach (ConfigDiffItem item in _items)
            item.PropertyChanged -= DiffItemOnPropertyChanged;
        _items.Clear();
        DiffList.ItemsSource = null;
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

        SelectedConfiguration = _incoming;
        IsConfirmed = true;
        Close();
    }

    private void OnClosed(object sender, EventArgs e)
    {
        Closed -= OnClosed;
        ReleaseCloudRevisions();
        _local = null;
        _incoming = null;
    }

    private sealed class CloudRevisionChoice
    {
        public CloudSettingsRevision Revision { get; init; }
        public string Label { get; set; }
    }
}
