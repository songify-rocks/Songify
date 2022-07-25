using System.IO;
using System.Reflection;
using Songify_Slim.UserControls;
using Songify_Slim.Util.Settings;
using System.Windows;
using MahApps.Metro.Controls;

namespace Songify_Slim
{
    /// <summary>
    ///     Interaktionslogik für Window_Botresponse.xaml
    /// </summary>
    public partial class Window_Botresponse
    {
        public Window_Botresponse()
        {
            InitializeComponent();
        }
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Cctrl.Content = new UC_BotResponses();
            tgl_botcmd_pos.IsOn = Settings.BotCmdPos;
            tgl_botcmd_song.IsOn = Settings.BotCmdSong;
            tgl_botcmd_next.IsOn = Settings.BotCmdNext;
            tgl_botcmd_skip.IsOn = Settings.BotCmdSkip;
            tgl_botcmd_skipvote.IsOn = Settings.BotCmdSkipVote;
            NudSkipVoteCount.Value = Settings.BotCmdSkipVoteCount;
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
        private void MetroWindow_Closed(object sender, System.EventArgs e)
        {
            ConfigHandler.WriteXml(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/config.xml", true);
        }

        private void tgl_botcmd_skipvote_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdSkipVote = ((ToggleSwitch)sender).IsOn;
        }

        private void NudSkipVoteCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            Settings.BotCmdSkipVoteCount = (int)((NumericUpDown)sender).Value;
        }
    }
}