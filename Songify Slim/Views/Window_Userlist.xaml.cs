using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.UI.Xaml.Controls.Primitives;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Settings;
using Songify_Slim.Util.Songify;
using Songify_Slim.Util.Songify.Twitch;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_Userlist.xaml
    /// </summary>
    public partial class WindowUserlist
    {
        public WindowUserlist()
        {
            InitializeComponent();

            ICollectionView view = CollectionViewSource.GetDefaultView(GlobalObjects.TwitchUsers);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(
                new SortDescription(
                    nameof(TwitchUser.HighestUserLevel),
                    ListSortDirection.Descending
                    )
                );
            DgvViewers.Items.Clear();
            DgvViewers.ItemsSource = view;
        }

        private async void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            DgvViewers.IsEnabled = false;
            GrdLoading.Visibility = Visibility.Visible;
            // Play Button animation
            await TwitchHandler.RunTwitchUserSync();
            // Stop Button Animation
            DgvViewers.IsEnabled = true;
            GrdLoading.Visibility = Visibility.Hidden;
        }

        private void BtnRefresh_MouseEnter(object sender, MouseEventArgs e)
        {
            // Remove any lingering finishing animation so the infinite spin can start fresh.
            if (IconContainer.RenderTransform is RotateTransform rt)
            {
                rt.BeginAnimation(RotateTransform.AngleProperty, null);
            }
        }

        private void BtnRefresh_MouseLeave(object sender, MouseEventArgs e)
        {
            if (IconContainer.RenderTransform is RotateTransform rt)
            {
                // Stop the infinite spin storyboard.
                SpinBeginStoryboard.Storyboard.Stop(IconContainer);

                // Get the current angle.
                double currentAngle = rt.Angle;

                // Calculate how much remains to reach the next full rotation.
                double remainder = currentAngle % 360;
                double additionalRotation = (360 - remainder) % 360; // if remainder is 0, no extra rotation needed

                // If the icon is nearly complete, no finishing animation is needed.
                if (additionalRotation < 0.5)
                    return;

                // Animate from the current angle to the next full rotation.
                DoubleAnimation finishingAnimation = new DoubleAnimation
                {
                    From = currentAngle,
                    To = currentAngle + additionalRotation,
                    Duration = TimeSpan.FromMilliseconds(300),
                    // Using FillBehavior.HoldEnd will hold the final value until cleared.
                    FillBehavior = FillBehavior.HoldEnd
                };

                rt.BeginAnimation(RotateTransform.AngleProperty, finishingAnimation);
            }
        }

        private void MenuItem_BlockSr_Click(object sender, RoutedEventArgs e)
        {
            if (DgvViewers.SelectedItem is not TwitchUser selectedItem) return;
            if (Settings.UserBlacklist.Contains(selectedItem.DisplayName.ToLower()))
            {
                Settings.UserBlacklist.Remove(selectedItem.DisplayName.ToLower());
                selectedItem.IsSrBlocked = false;
            }
            else
            {
                Settings.UserBlacklist.Add(selectedItem.DisplayName.ToLower());
                selectedItem.IsSrBlocked = true;
            }

            Settings.UserBlacklist = Settings.UserBlacklist;
        }
    }
}