using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Songify_Slim.Models.Blocklist;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for UC_BlacklistEntry.xaml
    /// </summary>
    public partial class UC_BlacklistEntry : UserControl
    {
        // Parent can subscribe and remove the item from its collection.
        public event EventHandler<IBlacklistItem> DeleteRequested;

        public UC_BlacklistEntry()
        {
            InitializeComponent();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is IBlacklistItem value)
                DeleteRequested?.Invoke(this, value);
        }
    }
}