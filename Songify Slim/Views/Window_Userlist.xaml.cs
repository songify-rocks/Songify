using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_Userlist.xaml
    /// </summary>
    public partial class WindowUserlist
    {
        public WindowUserlist()
        {
            InitializeComponent();

            ICollectionView view = CollectionViewSource.GetDefaultView(GlobalObjects.TwitchUsers);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(
                new SortDescription(
                    nameof(TwitchUser.UserLevel),
                    ListSortDirection.Descending
                    )
                );
            LbxUsers.ItemsSource = view;
            DgvViewers.Items.Clear();
            DgvViewers.ItemsSource = view;
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (LbxUsers.SelectedItem is not TwitchUser user)
                return;
            List<string> tempList = Settings.UserBlacklist;
            if (tempList.Any(o => o.Equals(user.DisplayName, StringComparison.OrdinalIgnoreCase)))
                return;
            tempList.Add(user.DisplayName);
            Settings.UserBlacklist = tempList;
        }
    }
}