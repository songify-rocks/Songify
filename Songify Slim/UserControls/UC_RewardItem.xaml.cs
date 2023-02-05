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
    public partial class UC_RewardItem
    {
        public CustomReward Reward;
        public bool IsManagable;

        public UC_RewardItem(CustomReward customReward, bool managable)
        {
            InitializeComponent();
            Reward = customReward;
            IsManagable = managable;
            if (Reward == null)
            {
                TbRewardCost.Text = "";
                TbRewardName.Text = "";
                ImgBorder.Background = new SolidColorBrush(Color.FromArgb(0,0,0,0));
                RewardImage.Source = null;
                return;
            }

            TbRewardName.Text = customReward.Title;
            TbRewardCost.Text = customReward.Cost.ToString();
            ImgBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(customReward.BackgroundColor));
            if (customReward.Image != null)
                RewardImage.Source = new BitmapImage(new Uri(customReward.Image.Url1x));
            if (managable)
                IconManagable.Visibility = Visibility.Visible;
        }
    }
}
