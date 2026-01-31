using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Songify_Slim.Models.Pear;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.Songify.APIs;
using Songify_Slim.Util.Songify.Pear;
using Songify_Slim.Util.Spotify;
using Swan.Formatters;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using DataGridColumn = System.Windows.Controls.DataGridColumn;

namespace Songify_Slim.Views
{
    /// <summary>
    ///     Queue Window to display the current song queue
    /// </summary>
    public partial class WindowQueue
    {
        private readonly DispatcherTimer _timer = new();
        private DateTime _lastBackButtonClickTime = DateTime.MinValue;

        public WindowQueue()
        {
            InitializeComponent();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (GlobalObjects.CurrentSong == null) return;
            switch (Settings.Player)
            {
                case Enums.PlayerType.Spotify:
                    SetButtonContent(!GlobalObjects.CurrentSong.IsPlaying);
                    break;

                case Enums.PlayerType.WindowsPlayback:
                    break;

                case Enums.PlayerType.FooBar2000:
                    break;

                case Enums.PlayerType.Vlc:
                    break;

                case Enums.PlayerType.BrowserCompanion:
                    break;

                case Enums.PlayerType.Pear:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // This window shows the current Queue in a DataGrid
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalObjects.QueueUpdateQueueWindow();

            foreach (DataGridColumn dataGridColumn in dgv_Queue.Columns)
            {
                //if ((string)dataGridColumn.Header == "Action")
                //{
                //    dataGridColumn.Visibility = Visibility.Visible;
                //    continue;
                //}

                dataGridColumn.Visibility = Visibility.Collapsed;
            }
            foreach (int queueWindowColumn in Settings.QueueWindowColumns)
            {
                foreach (CheckBox child in GlobalObjects.FindVisualChildren<CheckBox>(stackCols))
                {
                    if (child.Tag.ToString() == queueWindowColumn.ToString())
                        child.IsChecked = true;
                }
                dgv_Queue.Columns[queueWindowColumn].Visibility = Visibility.Visible;
            }

            int fSize = MathUtils.Clamp(Settings.FontsizeQueue, 12, 72);
            tbFontSize.Text = $"{fSize}";
            dgv_Queue.FontSize = fSize;
            _timer.IsEnabled = true;

            if (!Settings.SpotifyControlVisible)
            {
                BorderPlayerControls.Visibility = Visibility.Collapsed;
                BtnPlayerControlsVisibility.Margin = new Thickness(0, 0, 20, 6);
                BtnPlayerControlsVisibility.Content = new PackIconBootstrapIcons
                { Kind = PackIconBootstrapIconsKind.ChevronUp };
            }
            else
            {
                BorderPlayerControls.Visibility = Visibility.Visible;
                BtnPlayerControlsVisibility.Margin = new Thickness(0, 0, 20, 22);
                BtnPlayerControlsVisibility.Content = new PackIconBootstrapIcons
                { Kind = PackIconBootstrapIconsKind.ChevronDown };
            }
        }

        private async void DgvItemDelete_Click(object sender, RoutedEventArgs e)
        {
            // This deletes the selected requestobject
            if (dgv_Queue.SelectedItem == null)
                return;

            RequestObject req = (RequestObject)dgv_Queue.SelectedItem;
            if (req.Queueid == 0 || req.Requester == "Spotify") return;
            dynamic payload = new
            {
                uuid = Settings.Uuid,
                key = Settings.AccessKey,
                queueid = req.Queueid,
            };
            await SongifyApi.PatchQueueAsync(Json.Serialize(payload));
            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                GlobalObjects.ReqList.Remove(req);
                GlobalObjects.SkipList.Add(req);
            }));
            await GlobalObjects.QueueUpdateQueueWindow();
        }

        private void ColVisChecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            int index = int.Parse((sender as CheckBox)?.Tag.ToString() ?? "-1");
            if (index < 0) return;
            bool? isChecked = (sender as CheckBox)?.IsChecked;
            dgv_Queue.Columns[index].Visibility = isChecked != null && (bool)isChecked ? Visibility.Visible : Visibility.Collapsed;
            List<int> cols = (from UIElement item in stackCols.Children let @checked = ((CheckBox)item).IsChecked where @checked != null && (bool)@checked select int.Parse((item as CheckBox)?.Tag.ToString())).ToList();
            Settings.QueueWindowColumns = cols;
        }

        private async void BtnClearQueue_Click(object sender, RoutedEventArgs e)
        {
            // After user confirmation sends a command to the webserver which clears the queue
            MessageDialogResult msgResult = await this.ShowMessageAsync(Properties.Resources.s_Warning,
                Properties.Resources.mw_clearQueueDisclaimer, MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = Properties.Resources.msgbx_Yes, NegativeButtonText = Properties.Resources.msgbx_No });
            if (msgResult == MessageDialogResult.Affirmative)
            {
                //GlobalObjects.ReqList.Clear();
                //WebHelper.UpdateWebQueue("", "", "", "", "", "1", "c");
                GlobalObjects.ReqList.Clear();
                dynamic payload = new
                {
                    uuid = Settings.Uuid,
                    key = Settings.AccessKey
                };
                await SongifyApi.ClearQueueAsync(Json.Serialize(payload));
            }
            await GlobalObjects.QueueUpdateQueueWindow();
        }

        private void Dgv_Queue_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void BtnFontSizeDown_Click(object sender, RoutedEventArgs e)
        {
            int fontSize = MathUtils.Clamp((int)dgv_Queue.FontSize - 2, 12, 72);
            Settings.FontsizeQueue = fontSize;
            dgv_Queue.FontSize = fontSize;
            tbFontSize.Text = fontSize.ToString();
        }

        private void BtnFontSizeUp_Click(object sender, RoutedEventArgs e)
        {
            int fontSize = MathUtils.Clamp((int)dgv_Queue.FontSize + 2, 12, 72);
            Settings.FontsizeQueue = fontSize;
            dgv_Queue.FontSize = fontSize;
            tbFontSize.Text = fontSize.ToString();
        }

        private async void DgvButtonSkip_Click(object sender, RoutedEventArgs e)
        {
            // This deletes the selected requestobject
            if (dgv_Queue.SelectedItem == null)
                return;
            RequestObject req = (RequestObject)dgv_Queue.SelectedItem;

            if (req.Trackid == GlobalObjects.CurrentSong.SongId)
            {
                await SpotifyApiHandler.SkipSong();
                return;
            }

            //if (req.Queueid == 0 || req.Requester == "Spotify") return;
            dynamic payload = new
            {
                uuid = Settings.Uuid,
                key = Settings.AccessKey,
                queueid = req.Queueid,
            };
            await SongifyApi.PatchQueueAsync(Json.Serialize(payload));
            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                GlobalObjects.ReqList.Remove(req);
                GlobalObjects.SkipList.Add(req);
            }));
            await GlobalObjects.QueueUpdateQueueWindow();
        }

        private async void DgvButtonAddToFav_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RequestObject req = (RequestObject)dgv_Queue.SelectedItem;
                if (req == null)
                    return;
                await SpotifyApiHandler.AddToPlaylist(req.Trackid);
                await GlobalObjects.QueueUpdateQueueWindow();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        private void BtnUpdateQueue_OnClick(object sender, RoutedEventArgs e)
        {
            dgv_Queue.Items.Refresh();
        }

        private void Dgv_Queue_OnSourceUpdated(object sender, DataTransferEventArgs e)
        {
            foreach (object item in dgv_Queue.Items)
            {
                // Check if the item is a RequestObject and cast it
                if (item is not RequestObject request) continue;
                // Get the index of the item in the DataGrid
                int rowIndex = dgv_Queue.Items.IndexOf(request);
                DataGridRow row = (DataGridRow)dgv_Queue.ItemContainerGenerator.ContainerFromIndex(rowIndex);

                if (row == null) continue;
                // Find the button inside the DataGridTemplateColumn
                IEnumerable<Button> buttons = GlobalObjects.FindVisualChildren<Button>(row);

                foreach (Button button in buttons)
                {
                    if (button.Tag.ToString() == "like")
                    {
                        button.Content = request.IsLiked ?
                            new PackIconBootstrapIcons { Kind = PackIconBootstrapIconsKind.HeartFill } :
                            new PackIconBootstrapIcons { Kind = PackIconBootstrapIconsKind.Heart };
                    }
                }
            }
        }

        public void UpdateQueueIcons()
        {
            dgv_Queue.Items.Refresh();
            dgv_Queue.UpdateLayout();

            foreach (object item in dgv_Queue.Items)
            {
                // Check if the item is a RequestObject and cast it
                if (item is not RequestObject request) continue;
                // Get the index of the item in the DataGrid
                int rowIndex = dgv_Queue.Items.IndexOf(request);
                DataGridRow row = (DataGridRow)dgv_Queue.ItemContainerGenerator.ContainerFromIndex(rowIndex);

                if (row == null) continue;
                // Find the button inside the DataGridTemplateColumn
                IEnumerable<Button> buttons = GlobalObjects.FindVisualChildren<Button>(row);

                foreach (Button button in buttons)
                {
                    if (button.Tag != null && button.Tag.ToString() == "like")
                    {
                        button.Content = request.IsLiked ?
                            new PackIconBootstrapIcons { Kind = PackIconBootstrapIconsKind.HeartFill } :
                            new PackIconBootstrapIcons { Kind = PackIconBootstrapIconsKind.Heart };
                    }
                }
            }
        }

        private void Dgv_Queue_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
        }

        private async void BtnBack_OnClick(object sender, RoutedEventArgs e)
        {
            DateTime currentTime = DateTime.Now;

            // Check if the button was clicked within the last 3 seconds
            if ((currentTime - _lastBackButtonClickTime).TotalSeconds < 3)
            {
                switch (Settings.Player)
                {
                    // If clicked again within 3 seconds, skip to the previous track
                    case Enums.PlayerType.Spotify:
                        await SpotifyApiHandler.SkipPrevious();
                        break;

                    case Enums.PlayerType.Pear:
                        await PearApi.Previous();
                        break;

                    case Enums.PlayerType.WindowsPlayback:
                    case Enums.PlayerType.FooBar2000:
                    case Enums.PlayerType.Vlc:
                    case Enums.PlayerType.BrowserCompanion:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                switch (Settings.Player)
                {
                    // If clicked again within 3 seconds, skip to the previous track
                    case Enums.PlayerType.Spotify:
                        await SpotifyApiHandler.PlayFromStart();
                        break;

                    case Enums.PlayerType.Pear:
                        await PearApi.SeekTo(0);
                        break;

                    case Enums.PlayerType.WindowsPlayback:
                    case Enums.PlayerType.FooBar2000:
                    case Enums.PlayerType.Vlc:
                    case Enums.PlayerType.BrowserCompanion:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
                // If not, restart the current track from the beginning
            }

            // Update the last click time
            _lastBackButtonClickTime = currentTime;
        }

        private async void BtnNext_OnClick(object sender, RoutedEventArgs e)
        {
            switch (Settings.Player)
            {
                case 0:
                    await SpotifyApiHandler.SkipSong();
                    break;

                case Enums.PlayerType.Pear:
                    await PearApi.Next();
                    break;

                case Enums.PlayerType.WindowsPlayback:
                case Enums.PlayerType.FooBar2000:
                case Enums.PlayerType.Vlc:
                case Enums.PlayerType.BrowserCompanion:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void BtnPlayPause_OnClick(object sender, RoutedEventArgs e)
        {
            switch (Settings.Player)
            {
                case Enums.PlayerType.Spotify:
                    {
                        bool isPlaying = await SpotifyApiHandler.PlayPause();
                        SetButtonContent(!isPlaying);
                        break;
                    }
                case Enums.PlayerType.WindowsPlayback:
                case Enums.PlayerType.FooBar2000:
                case Enums.PlayerType.Vlc:
                case Enums.PlayerType.BrowserCompanion:
                case Enums.PlayerType.Pear:
                    {
                        PearResponse nowPlaying = await PearApi.GetNowPlayingAsync();
                        bool isPlaying = !nowPlaying.IsPaused;

                        if (isPlaying)
                            await PearApi.Pause();
                        else
                            await PearApi.Play();

                        SetButtonContent(isPlaying);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetButtonContent(bool isPlaying)
        {
            Grid grd = new();

            if (!isPlaying)
                grd.Margin = new Thickness(0, 0, 0, 0);
            grd.Children.Add(!isPlaying
                ? new PackIconBootstrapIcons { Kind = PackIconBootstrapIconsKind.PauseFill }
                : new PackIconBootstrapIcons { Kind = PackIconBootstrapIconsKind.PlayFill });
            BtnPlayPause.Content = grd;
        }

        private void BtnPlayerControlsVisibility_OnClick(object sender, RoutedEventArgs e)
        {
            if (BorderPlayerControls.Visibility == Visibility.Visible)
            {
                BorderPlayerControls.Visibility = Visibility.Collapsed;
                BtnPlayerControlsVisibility.Margin = new Thickness(0, 0, 20, 6);
                BtnPlayerControlsVisibility.Content = new PackIconBootstrapIcons
                { Kind = PackIconBootstrapIconsKind.ChevronUp };
                Settings.SpotifyControlVisible = false;
            }
            else
            {
                BorderPlayerControls.Visibility = Visibility.Visible;
                BtnPlayerControlsVisibility.Margin = new Thickness(0, 0, 20, 22);
                BtnPlayerControlsVisibility.Content = new PackIconBootstrapIcons
                { Kind = PackIconBootstrapIconsKind.ChevronDown };
                Settings.SpotifyControlVisible = true;
            }
        }
    }
}