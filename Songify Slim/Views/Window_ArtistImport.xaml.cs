using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Songify_Slim.Models.Blocklist;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Logger = Songify_Slim.Util.General.Logger;
using Task = System.Threading.Tasks.Task;

namespace Songify_Slim.Views
{
    public partial class Window_ArtistImport
    {
        public event EventHandler ImportCompleted;

        private List<string> _importHeaders = [];
        private List<string[]> _importRows = [];
        private readonly ObservableCollection<ArtistImportPreviewRow> _importPreviewRows = [];
        public ObservableCollection<ArtistImportPreviewRow> ImportPreviewRows => _importPreviewRows;

        public Window_ArtistImport()
        {
            InitializeComponent();
            DataContext = this;
            ImportSource_Checked(null, null);
        }

        private void ImportSource_Checked(object sender, RoutedEventArgs e)
        {
            bool useFile = RbImportFile?.IsChecked == true;
            if (TbImportFilePath != null)
                TbImportFilePath.IsEnabled = useFile;
            if (BtnImportBrowse != null)
                BtnImportBrowse.IsEnabled = useFile;
            if (TbImportUrl != null)
                TbImportUrl.IsEnabled = !useFile;
        }

        private void ImportBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select artist CSV"
            };

            if (dlg.ShowDialog(this) == true)
                TbImportFilePath.Text = dlg.FileName;
        }

        private async void ImportLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnImportConfirm.IsEnabled = false;
                TbImportStatus.Text = "Loading…";

                string csvText;
                if (RbImportFile.IsChecked == true)
                {
                    string path = TbImportFilePath.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    {
                        TbImportStatus.Text = "Choose a valid CSV file first.";
                        return;
                    }

                    csvText = await Task.Run(() => File.ReadAllText(path));
                }
                else
                {
                    csvText = await ArtistCsvImport.DownloadCsvAsync(TbImportUrl.Text?.Trim());
                }

                if (!ArtistCsvImport.TryParse(csvText, out List<string> headers, out List<string[]> rows, out string error))
                {
                    TbImportStatus.Text = error;
                    return;
                }

                _importHeaders = headers;
                _importRows = rows;

                List<ArtistCsvColumnOption> nameOptions = headers
                    .Select((h, i) => new ArtistCsvColumnOption { Index = i, Header = h, Display = $"{i + 1}: {h}" })
                    .ToList();

                List<ArtistCsvColumnOption> idOptions =
                [
                    new ArtistCsvColumnOption
                    {
                        Index = -1,
                        Header = ArtistCsvImport.NoneColumn,
                        Display = ArtistCsvImport.NoneColumn
                    }
                ];
                idOptions.AddRange(nameOptions);

                CbxImportNameColumn.ItemsSource = nameOptions;
                CbxImportNameColumn.SelectedValue = ArtistCsvImport.GuessColumnIndex(headers, ArtistCsvImport.NameColumnHints);
                if (CbxImportNameColumn.SelectedIndex < 0 && nameOptions.Count > 0)
                    CbxImportNameColumn.SelectedIndex = 0;

                CbxImportIdColumn.ItemsSource = idOptions;
                int guessedId = ArtistCsvImport.GuessColumnIndex(headers, ArtistCsvImport.IdColumnHints);
                CbxImportIdColumn.SelectedValue = guessedId >= 0 ? guessedId : -1;

                RefreshImportPreview();
                TbImportStatus.Text =
                    $"Loaded {_importRows.Count} row(s), {_importHeaders.Count} column(s). Map columns, then Import.";
                BtnImportConfirm.IsEnabled = _importRows.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Failed to load artist CSV", ex);
                TbImportStatus.Text = "Failed to load CSV. Check the URL/file and try again.";
                BtnImportConfirm.IsEnabled = false;
            }
        }

        private void ImportColumnMapping_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_importRows.Count == 0)
                return;

            RefreshImportPreview();
        }

        private void RefreshImportPreview()
        {
            _importPreviewRows.Clear();

            int nameIdx = CbxImportNameColumn?.SelectedValue is int n ? n : -1;
            int idIdx = CbxImportIdColumn?.SelectedValue is int i ? i : -1;

            foreach (string[] row in _importRows.Take(25))
            {
                string name = ArtistCsvImport.GetCell(row, nameIdx);
                string id = ArtistCsvImport.NormalizeSpotifyArtistId(ArtistCsvImport.GetCell(row, idIdx)) ?? "";
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(id))
                    continue;

                _importPreviewRows.Add(new ArtistImportPreviewRow
                {
                    Name = name,
                    Id = id
                });
            }
        }

        private async void ImportConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int nameIdx = CbxImportNameColumn?.SelectedValue is int n ? n : -1;
                int idIdx = CbxImportIdColumn?.SelectedValue is int i ? i : -1;

                if (nameIdx < 0 && idIdx < 0)
                {
                    await this.ShowMessageAsync("Import", "Select at least a Name or Id column.");
                    return;
                }

                List<BlockedArtist> list = Settings.ArtistBlacklist;
                ArtistCsvMergeResult merge = ArtistCsvImport.MergeRows(list, _importRows, nameIdx, idIdx);
                Settings.ArtistBlacklist = list;
                ImportCompleted?.Invoke(this, EventArgs.Empty);

                await this.ShowMessageAsync(
                    "Import complete",
                    $"Added {merge.Added} artist(s).\nSkipped {merge.SkippedDuplicate} duplicate(s), {merge.SkippedEmpty} empty row(s).");

                Close();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Failed to import artists from CSV", ex);
                await this.ShowMessageAsync("Error", "Import failed. Check the logs for details.");
            }
        }

        private void ImportCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class ArtistImportPreviewRow
    {
        public string Name { get; set; } = "";
        public string Id { get; set; } = "";
    }
}
