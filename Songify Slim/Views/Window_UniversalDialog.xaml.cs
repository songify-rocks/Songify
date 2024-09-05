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
using Songify_Slim.Models;
using Songify_Slim.UserControls;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_UniversalDialog.xaml
    /// </summary>
    public partial class WindowUniversalDialog
    {
        public WindowUniversalDialog(PSA psa, string title)
        {
            InitializeComponent();
            this.Title = title;
            this.ContentControl.Content = new PsaControl(psa, true);
        }
    }
}
