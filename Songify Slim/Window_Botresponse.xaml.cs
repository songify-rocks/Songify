using System.Windows;
using Songify_Slim.UserControls;
using Songify_Slim.Util.Settings;

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

        }

        private void tgl_botcmd_pos_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdPos = (sender as MahApps.Metro.Controls.ToggleSwitch).IsOn;
        }

        private void tgl_botcmd_song_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdSong = (sender as MahApps.Metro.Controls.ToggleSwitch).IsOn;
        }

        private void tgl_botcmd_next_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.BotCmdNext = (sender as MahApps.Metro.Controls.ToggleSwitch).IsOn;
        }
    }
}