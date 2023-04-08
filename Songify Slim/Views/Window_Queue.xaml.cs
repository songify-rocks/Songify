using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using System;
using System.Windows;
using System.Windows.Threading;
using Unosquare.Swan.Formatters;

namespace Songify_Slim.Views
{
    /// <summary>
    ///     Queue Window to display the current song queue
    /// </summary>
    public partial class WindowQueue
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        public WindowQueue()
        {
            InitializeComponent();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += (sender, args) =>
            {
                dgv_Queue.Items.Refresh();
            };
            _timer.IsEnabled = true;
        }

        // This window shows the current Queue in a DataGrid
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // This loads in all the requestobjects
            dgv_Queue.ItemsSource = GlobalObjects.ReqList;
        }

        private void DgvItemDelete_Click(object sender, RoutedEventArgs e)
        {
            // This deletes the selected requestobject
            if (dgv_Queue.SelectedItem == null)
                return;

            RequestObject req = (RequestObject)dgv_Queue.SelectedItem;
            dynamic payload = new
            {
                uuid = Settings.Uuid,
                key = Settings.AccessKey,
                queueid = req.Queueid,
            };
            WebHelper.QueueRequest(WebHelper.RequestMethod.Patch, Json.Serialize(payload));
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                GlobalObjects.ReqList.Remove(req);
            }));
            dgv_Queue.Items.Refresh();
        }
    }
}