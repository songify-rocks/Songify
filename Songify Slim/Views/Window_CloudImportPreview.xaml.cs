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
using Songify_Slim.Util.Settings;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_CloudImportPreview.xaml
    /// </summary>
    public partial class Window_CloudImportPreview
    {
        public bool IsConfirmed { get; private set; } = false;

        public Window_CloudImportPreview(Configuration local, Configuration incoming)
        {
            InitializeComponent();
            PopulateDiff(local, incoming);
        }

        private void PopulateDiff(Configuration local, Configuration incoming)
        {
            List<string> diffs = ConfigComparer.GetDifferences(local, incoming);
            ChangesList.ItemsSource = diffs.Count > 0 ? diffs : ["No differences detected."];
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.Close();
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.Close();
        }
    }
}
