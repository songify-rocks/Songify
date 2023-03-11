using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using Songify_Slim.Util.Settings;

namespace Songify_Slim.Views
{
    /// <summary>
    ///     Interaktionslogik für Window_Botresponse.xaml
    /// </summary>
    public partial class WindowBotresponse
    {
        public WindowBotresponse()
        {
            InitializeComponent();
        }
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Cctrl.Content = new UC_BotResponses();
            TglBotcmdPos.IsOn = Settings.BotCmdPos;
            TglBotcmdSong.IsOn = Settings.BotCmdSong;
            TglBotcmdNext.IsOn = Settings.BotCmdNext;
            TglBotcmdSkip.IsOn = Settings.BotCmdSkip;
            TglBotcmdSkipvote.IsOn = Settings.BotCmdSkipVote;
            TglBotcmdSsr.IsOn = Settings.TwSrCommand;
            NudSkipVoteCount.Value = Settings.BotCmdSkipVoteCount;
            TextBoxTriggerSong.Text = string.IsNullOrWhiteSpace(Settings.BotCmdSongTrigger) ? "song" : Settings.BotCmdSongTrigger;
            TextBoxTriggerPos.Text = string.IsNullOrWhiteSpace(Settings.BotCmdPosTrigger) ? "pos" : Settings.BotCmdPosTrigger;
            TextBoxTriggerNext.Text = string.IsNullOrWhiteSpace(Settings.BotCmdNextTrigger) ? "next" : Settings.BotCmdNextTrigger;
            TextBoxTriggerSkip.Text = string.IsNullOrWhiteSpace(Settings.BotCmdSkipTrigger) ? "skip" : Settings.BotCmdSkipTrigger;
            TextBoxTriggerVoteskip.Text = string.IsNullOrWhiteSpace(Settings.BotCmdVoteskipTrigger) ? "voteskip" : Settings.BotCmdVoteskipTrigger;
            TextBoxTriggerSsr.Text = string.IsNullOrWhiteSpace(Settings.BotCmdSsrTrigger) ? "ssr" : Settings.BotCmdSsrTrigger;
        }

        private void tgl_botcmd_pos_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdPos = ((ToggleSwitch)sender).IsOn;
        }

        private void tgl_botcmd_song_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdSong = ((ToggleSwitch)sender).IsOn;
        }

        private void tgl_botcmd_next_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdNext = ((ToggleSwitch)sender).IsOn;
        }
        private void tgl_botcmd_skip_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdSkip = ((ToggleSwitch)sender).IsOn;
        }
        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            //ConfigHandler.WriteXml(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml", true);
            Settings.BotCmdSongTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerSong.Text) ? "song" : TextBoxTriggerSong.Text;
            Settings.BotCmdPosTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerPos.Text) ? "pos" : TextBoxTriggerPos.Text;
            Settings.BotCmdNextTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerNext.Text) ? "next" : TextBoxTriggerNext.Text;
            Settings.BotCmdSkipTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerSkip.Text) ? "skip" : TextBoxTriggerSkip.Text;
            Settings.BotCmdVoteskipTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerVoteskip.Text) ? "voteskip" : TextBoxTriggerVoteskip.Text;
            Settings.BotCmdSsrTrigger = string.IsNullOrWhiteSpace(TextBoxTriggerSsr.Text) ? "ssr" : TextBoxTriggerSsr.Text;
        }

        private void tgl_botcmd_skipvote_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdSkipVote = ((ToggleSwitch)sender).IsOn;
        }

        private void NudSkipVoteCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            double? value = ((NumericUpDown)sender).Value;
            if (value != null)
                Settings.BotCmdSkipVoteCount = (int)value;
        }

        private void TextBoxTrigger_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            switch ((sender as TextBox)?.Tag.ToString())
            {
                case "song":
                    Settings.BotCmdSongTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "song"
                        : ((TextBox)sender).Text;
                    break;
                case "pos":
                    Settings.BotCmdPosTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "pos"
                        : ((TextBox)sender).Text;
                    break;
                case "next":
                    Settings.BotCmdNextTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "next"
                        : ((TextBox)sender).Text;
                    break;
                case "skip":
                    Settings.BotCmdSkipTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "skip"
                        : ((TextBox)sender).Text;
                    break;
                case "voteskip":
                    Settings.BotCmdVoteskipTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "voteskip"
                        : ((TextBox)sender).Text;
                    break;
                case "ssr":
                    Settings.BotCmdSsrTrigger = string.IsNullOrWhiteSpace(((TextBox)sender).Text)
                        ? "ssr"
                        : ((TextBox)sender).Text;
                    break;
            }
        }

        private void TextBoxTrigger_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
            base.OnPreviewKeyDown(e);
        }

        private void Tgl_botcmd_ssr_OnToggled_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.TwSrCommand = ((ToggleSwitch)sender).IsOn;
        }
    }
}