using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Views;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Songify_Slim.UserControls
{
    /// <summary>
    ///     Interaktionslogik für UC_BotResponses.xaml
    /// </summary>
    public partial class UcBotResponses
    {
        public UcBotResponses()
        {
            InitializeComponent();
        }

        private static string GetStringAndColor(string response, string newColor)
        {
            const int startIndex = 9;
            int endIndex = response.IndexOf("]", startIndex, StringComparison.Ordinal);
            string colorName = response.Substring(startIndex, endIndex - startIndex).ToLower().Trim();
            response = response.Replace($"[announce {colorName}]", $"[announce {newColor}]").Trim();
            return response;
        }

        private void AnnounceCheck_Checked(object sender, RoutedEventArgs e)
        {
            ComboBox cbx = GlobalObjects.FindChild<ComboBox>(this, ((CheckBox)sender).Name.Replace("check", "cb"));
            TextBox tbx = GlobalObjects.FindChild<TextBox>(this, ((CheckBox)sender).Name.Replace("check", "tb"));
            bool? isChecked = ((CheckBox)sender)?.IsChecked;
            if (isChecked != null && (bool)!isChecked)
            {

                if (cbx == null) return;
                if (!tbx.Text.StartsWith("[announce ")) return;
                cbx.SelectedIndex = 0;
                const int startIndex = 9;
                int endIndex = tbx.Text.IndexOf("]", startIndex, StringComparison.Ordinal);
                string colorName = tbx.Text.Substring(startIndex, endIndex - startIndex).ToLower().Trim();
                tbx.Text = tbx.Text.Replace($"[announce {colorName}]", string.Empty).Trim();
            }
            else
            {
                tbx.Text = "[announce " + ((ComboBoxItem)cbx.SelectedItem).Content.ToString().ToLower() + "]" + tbx.Text;
            }
        }

        private static void SetPreview(TextBox tb)
        {
            string response = tb.Text;


            Dictionary<string, string> replacements = new()
            {
                {"{user}", "SomeUser"},
                {"{artist}", "Rick Astley"},
                {"{title}", "Never Gonna Give You Up"},
                {"{maxreq}", "5"},
                {"{errormsg}", "Couldn't find a song matching your request."},
                {"{maxlength}", "300"},
                {"{votes}", "3/5"},
                {"{song}", "Rick Astley - Never Gonna Give You Up"},
                {"{req}", "John Doe"},
                {"{{", ""},
                {"}}", ""},
                {"{url}", "https://open.spotify.com/track/4cOdK2wGLETKBW3PvgPWqT?si=0633b850641d4bce"},
                {"{playlist_name}", "My Super Cool Playlist"},
                {"{playlist_url}", "https://open.spotify.com/playlist/2wKHJy4vO0pA1gXfACW8Qh?si=30184b3f0854459c"},
                {"{cd}", "5"}

            };
            foreach (KeyValuePair<string, string> pair in replacements)
            {
                response = response.Replace(pair.Key, pair.Value);
            }

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() != typeof(Window_Settings)) continue;
                    ((Window_Settings)window).LblPreview.Text = response;
                }
            }));
        }

        private static void SetTextBoxText(TextBox tb, Selector cb, ToggleButton check)
        {
            if (check.IsChecked != null && !(bool)check.IsChecked) return;
            if (tb.Text.StartsWith("[announce "))
                tb.Text = GetStringAndColor(tb.Text, ((ComboBoxItem)cb.SelectedItem).Content.ToString().ToLower());
            else
                tb.Text = "[announce " + ((ComboBoxItem)cb.SelectedItem).Content.ToString().ToLower() + "]" + tb.Text;

        }

        private void tb__GotFocus(object sender, RoutedEventArgs e)
        {
            SetPreview(sender as TextBox);
        }

        private void tb_ArtistBlocked_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespBlacklist = TbArtistBlocked.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_Error_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespError = TbError.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_MaxLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespLength = TbMaxLength.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_MaxSongs_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespMaxReq = TbMaxSongs.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_ModSkip_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespModSkip = TbModSkip.Text;
            SetPreview(sender as TextBox);
        }

        private void Tb_Next_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespNext = TbNext.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_NoSong_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespNoSong = TbNoSong.Text;
            SetPreview(sender as TextBox);
        }

        private void Tb_Pos_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespPos = TbPos.Text;
            SetPreview(sender as TextBox);
        }

        private void Tb_Refund_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespRefund = TbRefund.Text;
            SetPreview(sender as TextBox);
        }

        private void Tb_Song_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespSong = TbSong.Text;
            SetPreview(sender as TextBox);
        }

        private void tb_SongInQueue_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespIsInQueue = TbSongInQueue.Text;
            SetPreview(sender as TextBox);
        }
        private void tb_Success_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespSuccess = TbSuccess.Text;
            SetPreview(sender as TextBox);
        }
        private void tb_VoteSkip_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespVoteSkip = TbVoteSkip.Text;
            SetPreview(sender as TextBox);
        }
        private void Tb_SongLike_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespSongLike = TbSongLike.Text;
            SetPreview(sender as TextBox);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            TbArtistBlocked.Text = Settings.BotRespBlacklist;
            TbSongInQueue.Text = Settings.BotRespIsInQueue;
            TbMaxSongs.Text = Settings.BotRespMaxReq;
            TbMaxLength.Text = Settings.BotRespLength;
            TbError.Text = Settings.BotRespError;
            TbSuccess.Text = Settings.BotRespSuccess;
            TbNoSong.Text = Settings.BotRespNoSong;
            TbModSkip.Text = Settings.BotRespModSkip;
            TbVoteSkip.Text = Settings.BotRespVoteSkip;
            TbPos.Text = Settings.BotRespPos;
            TbNext.Text = Settings.BotRespNext;
            TbSong.Text = Settings.BotRespSong;
            TbRefund.Text = Settings.BotRespRefund;
            TbSongLike.Text = Settings.BotRespSongLike;
            TbNotFoundInPlaylist.Text = Settings.BotRespPlaylist;
            TbRemove.Text = Settings.BotRespRemove;
            TbExplicit.Text = Settings.BotRespTrackExplicit;
            TbSrCooldown.Text = Settings.BotRespCooldown;
            TbnoTrackFound.Text = Settings.BotRespNoTrackFound;

            foreach (ComboBox box in GlobalObjects.FindVisualChildren<ComboBox>(this))
            {
                box.SelectedIndex = 0;
            }
        }

        private void TbNotFoundInPlaylist_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespPlaylist = TbNotFoundInPlaylist.Text;
            SetPreview(sender as TextBox);
        }

        private void TbRemove_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespRemove = TbRemove.Text;
            SetPreview(sender as TextBox);
        }

        private void TbExplicit_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespTrackExplicit = TbExplicit.Text;
            SetPreview(sender as TextBox);
        }

        private void TbSrCooldown_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespCooldown = TbSrCooldown.Text;
            SetPreview(sender as TextBox);
        }

        private void TbnoTrackFound_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespNoTrackFound = TbnoTrackFound.Text;
            SetPreview(sender as TextBox);
        }
    }
}