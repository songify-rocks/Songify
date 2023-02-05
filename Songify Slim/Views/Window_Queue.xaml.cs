using System.Windows;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;

namespace Songify_Slim
{
    /// <summary>
    ///     Queue Window to display the current song queue
    /// </summary>
    public partial class Window_Queue
    {
        public Window_Queue()
        {
            InitializeComponent();
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
            GlobalObjects.ReqList.Remove(req);
            WebHelper.UpdateWebQueue(req.TrackID, "", "", "", "", "1", "u");
            dgv_Queue.Items.Refresh();
        }
    }
}