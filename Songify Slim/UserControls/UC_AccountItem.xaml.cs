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

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for UC_AccountItem.xaml
    /// </summary>
    public partial class UC_AccountItem : UserControl
    {
        public string Username;
        public string OAuth;

        public UC_AccountItem(string username, string oauth)
        {
            InitializeComponent();
            Username = username;
            OAuth = oauth;
            TbUserName.Text = Username;
        }
    }
}
