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
using System.Windows.Shapes;

namespace Songify_Slim
{
    /// <summary>
    /// Interaktionslogik für Window_Botresponse.xaml
    /// </summary>
    public partial class Window_Botresponse
    {
        public Window_Botresponse()
        {
            InitializeComponent();
        }

        private void tb_ArtistBlocked_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Bot_Resp_Blacklist = tb_ArtistBlocked.Text;
        }

        private void tb_SongInQueue_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Bot_Resp_IsInQueue = tb_SongInQueue.Text;
        }

        private void tb_MaxSongs_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Bot_Resp_MaxReq = tb_MaxSongs.Text;
        }

        private void tb_MaxLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Bot_Resp_Length = tb_MaxLength.Text;
        }

        private void tb_Error_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Bot_Resp_Error = tb_Error.Text;
        }

        private void tb_Success_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Bot_Resp_Success = tb_Success.Text;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tb_ArtistBlocked.Text = Settings.Bot_Resp_Blacklist;
            tb_SongInQueue.Text = Settings.Bot_Resp_IsInQueue;
            tb_MaxSongs.Text = Settings.Bot_Resp_MaxReq;
            tb_MaxLength.Text = Settings.Bot_Resp_Length;
            tb_Error.Text = Settings.Bot_Resp_Error;
            tb_Success.Text = Settings.Bot_Resp_Success;
        }
    }
}
