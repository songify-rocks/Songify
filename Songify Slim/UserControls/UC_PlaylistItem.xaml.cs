using SpotifyAPI.Web.Models;
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
using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for UC_PlaylistItem.xaml
    /// </summary>
    public partial class UC_PlaylistItem
    {
        public SimplePlaylist _playlist;
        public UC_PlaylistItem(SimplePlaylist playlist)
        {
            InitializeComponent();
            _playlist = playlist;
            if (playlist == null)
            {
                TbPlaylistName.Text = "";
                ImgBorder.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                PlaylistImage.Source = null;
                return;
            }

            TbPlaylistName.Text = playlist.Name;
            if (playlist.Images.Count != 0)
                PlaylistImage.Source = new BitmapImage(new Uri(playlist.Images.First().Url));
        }
    }
}
