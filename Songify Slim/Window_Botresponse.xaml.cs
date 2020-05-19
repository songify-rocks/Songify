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
            setPreview(sender as TextBox);
        }

        private void tb_SongInQueue_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Bot_Resp_IsInQueue = tb_SongInQueue.Text;
            setPreview(sender as TextBox);
        }

        private void tb_MaxSongs_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Bot_Resp_MaxReq = tb_MaxSongs.Text;
            setPreview(sender as TextBox);

        }

        private void tb_MaxLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Bot_Resp_Length = tb_MaxLength.Text;
            setPreview(sender as TextBox);

        }

        private void tb_Error_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Bot_Resp_Error = tb_Error.Text;
            setPreview(sender as TextBox);

        }

        private void tb_Success_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.Bot_Resp_Success = tb_Success.Text;
            setPreview(sender as TextBox);
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

        private void setPreview(TextBox tb)
        {
            string response;
            // if no track has been found inform the requester
            response = tb.Text;
            response = response.Replace("{user}", Settings.TwAcc);
            response = response.Replace("{artist}", "Rick Astley");
            response = response.Replace("{title}", "Never Gonna Give You Up");
            response = response.Replace("{maxreq}", Settings.TwSRMaxReq.ToString());
            response = response.Replace("{errormsg}", "Couldn't find a song matching your request.");
            lbl_Preview.Text = response;
        }

        private void tb__GotFocus(object sender, RoutedEventArgs e)
        {
            setPreview(sender as TextBox);
        }
    }
}
