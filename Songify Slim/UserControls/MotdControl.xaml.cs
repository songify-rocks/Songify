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
using MahApps.Metro.IconPacks;
using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for MotdControl.xaml
    /// </summary>
    public partial class MotdControl : UserControl
    {
        private Motd _motd;

        PackIconMaterial readIcon = new()
        {
            Kind = PackIconMaterialKind.Check,
            Width = 12,
            Height = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        public MotdControl(Motd motd)
        {
            InitializeComponent();
            this._motd = motd;
            TbAuthor.Text = _motd.Author;
            TbDate.Text = _motd.CreatedAtDateTime?.ToString("dd.MM.yyyy HH:mm");
            TbMessage.Text = IOManager.InterpretEscapeCharacters(_motd.MessageText);
            TbSeverity.Text = _motd.Severity;

            Brush severitybrush = _motd.Severity switch
            {
                "Low" => Brushes.ForestGreen,
                "Medium" => Brushes.DarkOrange,
                "High" => Brushes.IndianRed,
                _ => throw new ArgumentOutOfRangeException()
            };

            BorderSeverity.BorderBrush = severitybrush;
            BorderSeverity.Background = severitybrush;

            if (_motd.Severity == "High")
            {
                BorderMotd.BorderBrush = severitybrush;
            }

            if (Settings.ReadNotificationIds != null && Settings.ReadNotificationIds.Contains(motd.Id))
            {

                btnRead.Content = readIcon;
            }
        }

        private void BtnRead_OnClick(object sender, RoutedEventArgs e)
        {
            btnRead.Content = readIcon;
            List<int> readNotificationIds = Settings.ReadNotificationIds;
            if (readNotificationIds != null && readNotificationIds.Contains(_motd.Id))
                return;
            readNotificationIds ??= [];
            readNotificationIds.Add(_motd.Id);
            Settings.ReadNotificationIds = readNotificationIds;

        }
    }
}
