using Songify_Slim.Util.Settings;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Songify_Slim.UserControls
{
    /// <summary>
    ///     Interaktionslogik für UC_BotResponses.xaml
    /// </summary>
    public partial class UC_BotResponses : UserControl
    {
        public UC_BotResponses()
        {
            InitializeComponent();
        }

        private void tb_ArtistBlocked_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespBlacklist = tb_ArtistBlocked.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_SongInQueue_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespIsInQueue = tb_SongInQueue.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_MaxSongs_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespMaxReq = tb_MaxSongs.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_MaxLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespLength = tb_MaxLength.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_Error_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespError = tb_Error.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_Success_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespSuccess = tb_Success.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_NoSong_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespNoSong = tb_NoSong.Text;
            SetPreview(sender as TextBox);
        }
        private void tb_VoteSkip_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespVoteSkip = tb_VoteSkip.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_ModSkip_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespModSkip = tb_ModSkip.Text;
            SetPreview(sender as TextBox);
        }
        private void tb__GotFocus(object sender, RoutedEventArgs e)
        {
            SetPreview(sender as TextBox);
        }

        private void SetPreview(TextBox tb)
        {
            string response;
            // if no track has been found inform the requester
            response = tb.Text;
            response = response.Replace("{user}", Settings.TwAcc);
            response = response.Replace("{artist}", "Rick Astley");
            response = response.Replace("{title}", "Never Gonna Give You Up");
            response = response.Replace("{maxreq}", Settings.TwSrMaxReq.ToString());
            response = response.Replace("{errormsg}", "Couldn't find a song matching your request.");
            response = response.Replace("{maxlength}", Settings.MaxSongLength.ToString());
            response = response.Replace("{votes}", "3/5");

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() != typeof(Window_Botresponse)) continue;
                    ((Window_Botresponse)window).lbl_Preview.Text = response;
                }
            }));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            tb_ArtistBlocked.Text = Settings.BotRespBlacklist;
            tb_SongInQueue.Text = Settings.BotRespIsInQueue;
            tb_MaxSongs.Text = Settings.BotRespMaxReq;
            tb_MaxLength.Text = Settings.BotRespLength;
            tb_Error.Text = Settings.BotRespError;
            tb_Success.Text = Settings.BotRespSuccess;
            tb_NoSong.Text = Settings.BotRespNoSong;
            tb_ModSkip.Text = Settings.BotRespModSkip;
            tb_VoteSkip.Text = Settings.BotRespVoteSkip;
            tb_Pos.Text = Settings.BotRespPos;
            tb_Next.Text = Settings.BotRespNext;

        }

        private void Tb_Pos_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespPos = tb_Pos.Text;
            SetPreview(sender as TextBox);
        }

        private void Tb_Next_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespNext = tb_Next.Text;
            SetPreview(sender as TextBox);
        }
    }
}