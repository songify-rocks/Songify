﻿using MahApps.Metro.Controls.Dialogs;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Unosquare.Swan.Formatters;

namespace Songify_Slim.Views
{
    /// <summary>
    ///     Queue Window to display the current song queue
    /// </summary>
    public partial class WindowQueue
    {
        private readonly DispatcherTimer _timer = new();
        public WindowQueue()
        {
            InitializeComponent();
        }

        // This window shows the current Queue in a DataGrid
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalObjects.QueueUpdateQueueWindow();

            foreach (DataGridColumn dataGridColumn in dgv_Queue.Columns)
            {
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
            List<int> cols = (from UIElement item in stackCols.Children where (bool)(item as CheckBox).IsChecked select int.Parse((item as CheckBox)?.Tag.ToString())).ToList();
            Settings.QueueWindowColumns = cols;
        }

        private async void BtnClearQueue_Click(object sender, RoutedEventArgs e)
        {
            // After user confirmation sends a command to the webserver which clears the queue
            MessageDialogResult msgResult = await this.ShowMessageAsync("Notification",
                "Do you really want to clear the queue?", MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
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
    }
}