using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
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
using Songify_Slim.Util.Songify.YTMDesktop;
using Unosquare.Swan.Formatters;
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
            _timer.Tick += _timer_Tick;
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            if (GlobalObjects.CurrentSong == null) return;
            Grid grd = new Grid();
            if (!GlobalObjects.CurrentSong.IsPlaying)
                grd.Margin = new Thickness(3, 0, 0, 0);
            grd.Children.Add(GlobalObjects.CurrentSong.IsPlaying
                ? new PackIconBootstrapIcons { Kind = PackIconBootstrapIconsKind.PauseFill }
                : new PackIconBootstrapIcons { Kind = PackIconBootstrapIconsKind.PlayFill });
            BtnPlayPause.Content = grd;
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
            await WebHelper.QueueRequest(WebHelper.RequestMethod.Patch, Json.Serialize(payload));
            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                GlobalObjects.ReqList.Remove(req);
                GlobalObjects.SkipList.Add(req);
            }));
            GlobalObjects.QueueUpdateQueueWindow();
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
                await WebHelper.QueueRequest(WebHelper.RequestMethod.Clear, Json.Serialize(payload));
            }
            GlobalObjects.QueueUpdateQueueWindow();
        }

        private void dgv_Queue_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void btnFontSizeDown_Click(object sender, RoutedEventArgs e)
        {
            int fontSize = MathUtils.Clamp((int)dgv_Queue.FontSize - 2, 12, 72);
            Settings.FontsizeQueue = fontSize;
            dgv_Queue.FontSize = fontSize;
            tbFontSize.Text = fontSize.ToString();
        }

        private void btnFontSizeUp_Click(object sender, RoutedEventArgs e)
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
            await WebHelper.QueueRequest(WebHelper.RequestMethod.Patch, Json.Serialize(payload));
            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                GlobalObjects.ReqList.Remove(req);
                GlobalObjects.SkipList.Add(req);
            }));
            GlobalObjects.QueueUpdateQueueWindow();
        }

        private async void DgvButtonAddToFav_Click(object sender, RoutedEventArgs e)
        {
            RequestObject req = (RequestObject)dgv_Queue.SelectedItem;
            if (req == null)
                return;
            await SpotifyApiHandler.AddToPlaylist(req.Trackid);
            GlobalObjects.QueueUpdateQueueWindow();
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

        private void dgv_Queue_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {

        }

        private async void BtnBack_OnClick(object sender, RoutedEventArgs e)
        {
            switch (Settings.Player)
            {
                case 0:
                    DateTime currentTime = DateTime.Now;

                    // Check if the button was clicked within the last 3 seconds
                    if ((currentTime - _lastBackButtonClickTime).TotalSeconds < 3)
                    {
                        // If clicked again within 3 seconds, skip to the previous track
                        await SpotifyApiHandler.SkipPrevious();
                    }
                    else
                    {
                        // If not, restart the current track from the beginning
                        await SpotifyApiHandler.PlayFromStart();
                    }

                    // Update the last click time
                    _lastBackButtonClickTime = currentTime;
                    break;
                case 6:
                    await WebHelper.YTMDPrevious();
                    break;
            }
        }

        private async void BtnNext_OnClick(object sender, RoutedEventArgs e)
        {
            switch (Settings.Player)
            {
                case 0:
                    await SpotifyApiHandler.SkipSong();
                    break;
                case 6:
                    await WebHelper.YTMDNext();
                    break;
            }
            await SpotifyApiHandler.SkipSong();
        }

        private async void BtnPlayPause_OnClick(object sender, RoutedEventArgs e)
        {
            switch (Settings.Player)
            {
                case 0:
                    {
                        bool isPlaying = await SpotifyApiHandler.PlayPause();

                        Grid grd = new Grid();
                        if (!isPlaying)
                            grd.Margin = new Thickness(3, 0, 0, 0);
                        grd.Children.Add(isPlaying
                            ? new PackIconBootstrapIcons { Kind = PackIconBootstrapIconsKind.PauseFill }
                            : new PackIconBootstrapIcons { Kind = PackIconBootstrapIconsKind.PlayFill });
                        BtnPlayPause.Content = grd;
                        break;
                    }
                case 6:
                    await WebHelper.YTMDPlayPause();
                    break;
            }
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