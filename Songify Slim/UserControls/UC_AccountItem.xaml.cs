using System.Windows.Controls;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for UC_AccountItem.xaml
    /// </summary>
    public partial class UcAccountItem : UserControl
    {
        public string Username;
        public string OAuth;

        public UcAccountItem(string username, string oauth)
        {
            InitializeComponent();
            Username = username;
            OAuth = oauth;
            TbUserName.Text = Username;
        }
    }
}
