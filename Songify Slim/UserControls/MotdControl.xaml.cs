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
using Songify_Slim.Models;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for MotdControl.xaml
    /// </summary>
    public partial class MotdControl : UserControl
    {
        public MotdControl(Motd motd)
        {
            InitializeComponent();
            TbAuthor.Text = motd.Author;
            TbDate.Text = motd.CreatedAtDateTime?.ToString("dd.MM.yyyy HH:mm");
            TbMessage.Text = motd.MessageText;
            TbSeverity.Text = motd.Severity;

            Brush severitybrush = motd.Severity switch
            {
                "Low" => Brushes.ForestGreen,
                "Medium" => Brushes.DarkOrange,
                "High" => Brushes.IndianRed,
                _ => throw new ArgumentOutOfRangeException()
            };

            BorderSeverity.BorderBrush = severitybrush;
            BorderSeverity.Background = severitybrush;

            if (motd.Severity == "High")
            {
                BorderMotd.BorderBrush = severitybrush;
            }
        }
    }
}
