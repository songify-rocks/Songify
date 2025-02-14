using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Songify_Slim.UserControls
{
    /// <summary>
    ///     Interaktionslogik für UC_BotResponses.xaml
    /// </summary>
    public partial class UcBotResponses
    {
        private readonly Dictionary<string, string> _textBoxDefaults = new()
        {
            {"TbArtistBlocked","@{user} the Artist: {artist} has been blacklisted by the broadcaster."},
            {"TbSrCooldown","The command is on cooldown. Try again in {cd} seconds."},
            {"TbError","@{user} there was an error adding your Song to the queue. Error message: {errormsg}"},
            {"TbExplicit","This Song containts explicit content and is not allowed."},
            {"TbSongInQueue","@{user} this song is already in the queue."},
            {"TbMaxLength","@{user} the song you requested exceeded the maximum song length ({maxlength})."},
            {"TbMaxSongs","@{user} maximum number of songs in queue reached ({maxreq})."},
            {"TbModSkip","@{user} skipped the current song."},
            {"TbNext","@{user} {song}"},
            {"TbNoSong","@{user} please specify a song to add to the queue."},
            {"TbnoTrackFound","No track found."},
            {"TbNotFoundInPlaylist","This song was not found in the allowed playlist.({playlist_name} {playlist_url})"},
            {"TbPos","@{user} {songs}{pos} {song}{/songs}"},
            {"TbRefund","Your points have been refunded."},
            {"TbRemove","{user} your previous request ({song}) will be skipped."},
            {"TbSong","@{user} {song}"},
            {"TbSongLike","The Song {song} has been added to the playlist."},
            {"TbSuccess","{artist} - {title} requested by @{user} has been added to the queue."},
            {"TbVoteSkip","@{user} voted to skip the current song. ({votes})"},
            {"TbSrUserCooldown","@{user} you have to wait {cd} before you can request a song again."},
        };

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
                {"{single_artist}", "Rick Astley"},
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

            response = replacements.Aggregate(response, (current, pair) => current.Replace(pair.Key, pair.Value));

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
            TbSrUserCooldown.Text = Settings.BotRespUserCooldown;

            TbTriggerSong.Text = $"!{Settings.BotCmdSongTrigger}";
            TbTriggerRemove.Text = $"!{Settings.BotCmdRemoveTrigger}";
            TbTriggerPos.Text = $"!{Settings.BotCmdPosTrigger}";
            TbTriggerNext.Text = $"!{Settings.BotCmdNextTrigger}";
            TbTriggerLike.Text = $"!{Settings.BotCmdSonglikeTrigger}";

            foreach (ComboBox box in GlobalObjects.FindVisualChildren<ComboBox>(this))
            {
                box.SelectedIndex = 0;
            }

            foreach (TextBox textBox in GlobalObjects.FindVisualChildren<TextBox>(this))
            {
                // Optionally store the default text in the Tag (or another property) if you want to reset later
                textBox.Tag = textBox.Text; // assuming initial text is your default

                // Create a new context menu for this TextBox
                ContextMenu contextMenu = new();

                // Create the Cut menu item and wire up its Click event
                MenuItem cutItem = new() { Header = "Cut", Command = ApplicationCommands.Cut, CommandTarget = textBox };
 
                // Create the Copy menu item
                MenuItem copyItem = new() { Header = "Copy", Command = ApplicationCommands.Copy, CommandTarget = textBox };

                // Create the Paste menu item
                MenuItem pasteItem = new() { Header = "Paste", Command = ApplicationCommands.Paste, CommandTarget = textBox };

                // Create the Reset menu item
                MenuItem resetItem = new() { Header = "Reset to default" };
                resetItem.Click += (_, _) =>
                {
                    // Reset the TextBox text to the default stored in Tag
                    textBox.Text = _textBoxDefaults[textBox.Name];
                };

                // Add the items to the context menu
                contextMenu.Items.Add(cutItem);
                contextMenu.Items.Add(copyItem);
                contextMenu.Items.Add(pasteItem);
                contextMenu.Items.Add(new Separator()); // optional: add a separator
                contextMenu.Items.Add(resetItem);

                // When the context menu opens, update the enabled state based on the TextBox state
                contextMenu.Opened += (_, _) =>
                {
                    // Enable Cut and Copy only if there is a text selection
                    bool hasSelection = !string.IsNullOrEmpty(textBox.SelectedText);
                    cutItem.IsEnabled = hasSelection;
                    copyItem.IsEnabled = hasSelection;

                    // Optionally, enable Paste only if the clipboard contains text
                    pasteItem.IsEnabled = Clipboard.ContainsText();
                };

                // Assign the context menu to the TextBox
                textBox.ContextMenu = contextMenu;
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

        private void TbSrUserCooldown_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.BotRespUserCooldown = TbSrUserCooldown.Text;
            SetPreview(sender as TextBox);
        }
    }
}