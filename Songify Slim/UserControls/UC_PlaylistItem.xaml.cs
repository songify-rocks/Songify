﻿using SpotifyAPI.Web;
using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for UC_PlaylistItem.xaml
    /// </summary>
    public partial class UcPlaylistItem
    {
        public FullPlaylist Playlist;
        public UcPlaylistItem(FullPlaylist playlist)
        {
            InitializeComponent();
            Playlist = playlist;
            if (playlist == null)
            {
                TbPlaylistName.Text = "";
                ImgBorder.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                PlaylistImage.Source = null;
                return;
            }


            TbPlaylistName.Text = playlist.Name;
            if (playlist.Images == null) return;
            if (playlist.Images.Count != 0)
                PlaylistImage.Source = new BitmapImage(new Uri(playlist.Images.First().Url));
        }
    }
}
