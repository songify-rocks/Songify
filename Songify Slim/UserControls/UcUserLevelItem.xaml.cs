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
using Songify_Slim.Util.General;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for UcUserLevelItem.xaml
    /// </summary>
    public partial class UcUserLevelItem : UserControl
    {
        public UcUserLevelItem()
        {
            InitializeComponent();
        }

        public int UserLevel
        {
            get => (int)GetValue(UserLevelProperty);
            set => SetValue(UserLevelProperty, value);
        }

        public static readonly DependencyProperty UserLevelProperty =
            DependencyProperty.Register(
                nameof(UserLevel),
                typeof(int),
                typeof(UcUserLevelItem),
                new FrameworkPropertyMetadata(0, null, CoerceUSerLevel),
                ValidateUserLevel);

        private static object CoerceUSerLevel(DependencyObject d, object baseValue)
        {
            int value = (int)baseValue;
            return value switch
            {
                < -1 => -1,
                > 7 => 7,
                _ => value
            };
        }

        private static bool ValidateUserLevel(object value)
        {
            int level = (int)value;
            return level is >= -1 and <= 7;
        }
    }
}