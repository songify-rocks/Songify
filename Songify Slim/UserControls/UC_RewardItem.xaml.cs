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
using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for UC_RewardItem.xaml
    /// </summary>
    public partial class UC_RewardItem : UserControl
    {
        public CustomReward Reward;

        public UC_RewardItem(CustomReward customReward)
        {
            InitializeComponent();
            this.Reward = customReward;
            TbRewardName.Text = customReward.Title;
            TbRewardCost.Text = customReward.Cost.ToString();
            ImgBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(customReward.BackgroundColor));
            if (customReward.Image != null)
                RewardImage.Source = new BitmapImage(new Uri(customReward.Image.Url1x));
        }
    }
}
