using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for UC_RewardItem.xaml
    /// </summary>
    public partial class UcRewardItem
    {
        public CustomReward Reward;
        public bool IsManagable;

        public UcRewardItem(CustomReward customReward, bool managable)
        {
            InitializeComponent();
            Reward = customReward;
            IsManagable = managable;
            if (Reward == null)
            {
                TbRewardCost.Text = "";
                TbRewardName.Text = "";
                ImgBorder.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                RewardImage.Source = null;
                return;
            }

            TbRewardName.Text = Reward.Title;
            TbRewardCost.Text = Reward.Cost.ToString();
            if (Reward.BackgroundColor != null)
            {
                try
                {
                    ImgBorder.Background = Reward is { BackgroundColor: not null } ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(Reward.BackgroundColor)!) : GetRandomSolidColorBrush();
                }
                catch (Exception)
                {
                    ImgBorder.Background = GetRandomSolidColorBrush();
                }
            }

            if (Reward.Image != null)
                RewardImage.Source = new BitmapImage(new Uri(Reward.Image.Url1x));
            if (managable)
                IconManagable.Visibility = Visibility.Visible;
        }

        public static SolidColorBrush GetRandomSolidColorBrush()
        {
            Random random = new();
            byte r = (byte)random.Next(256);
            byte g = (byte)random.Next(256);
            byte b = (byte)random.Next(256);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }
}
