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
using Windows.UI.Composition;
using MahApps.Metro.IconPacks;
using Songify_Slim.Util.General;
using YamlDotNet.Core.Tokens;
using Songify_Slim.Util.Settings;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

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

        public bool LongName
        {
            get => (bool)GetValue(LongNameProperty);
            set => SetValue(LongNameProperty, value);
        }

        public string UserName
        {
            get => (string)GetValue(UserNameProperty);
            set => SetValue(UserNameProperty, value);
        }

        public string UserId { get; set; }

        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(
                nameof(UserName),
                typeof(string),
                typeof(UcUserLevelItem),
                new FrameworkPropertyMetadata("", null, null));

        public static readonly DependencyProperty LongNameProperty =
            DependencyProperty.Register(
                nameof(LongName),
                typeof(bool),
                typeof(UcUserLevelItem),
                new FrameworkPropertyMetadata(false, null, null));

        public static readonly DependencyProperty UserLevelProperty =
            DependencyProperty.Register(
                nameof(UserLevel),
                typeof(int),
                typeof(UcUserLevelItem),
                new FrameworkPropertyMetadata(0, null, CoerceUserLevel),
                ValidateUserLevel);

        private static object CoerceUserLevel(DependencyObject d, object baseValue)
        {
            int value = (int)baseValue;
            return value switch
            {
                < -2 => -2,
                > 7 => 7,
                _ => value
            };
        }

        private static bool ValidateUserLevel(object value)
        {
            int level = (int)value;
            return level is >= -2 and <= 7;
        }

        private void BtnRemoveBadge_OnClick(object sender, RoutedEventArgs e)
        {
            // Inside your child UserControl
            UC_CommandItem parent = FindParent<UC_CommandItem>(this);
            if (parent == null) return;
            if (UserLevel == -2)
            {
                if (parent.Command.AllowedUsers.All(u => u.Id != UserId)) return;
                List<User> list = [.. parent.Command.AllowedUsers];
                list.RemoveAll(u => u.Id == UserId);
                parent.Command.AllowedUsers = list;
            }
            else
            {
                if (!parent.Command.AllowedUserLevels.Contains(UserLevel)) return;
                List<int> list = [.. parent.Command.AllowedUserLevels];
                list.Remove(UserLevel);
                parent.Command.AllowedUserLevels = list;
            }
            Settings.UpdateCommand(parent.Command);
            parent.UpdateUserLevelbadges();
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            while (parentObject != null && parentObject is not T)
            {
                parentObject = VisualTreeHelper.GetParent(parentObject);
            }

            return parentObject as T;
        }

    }
}