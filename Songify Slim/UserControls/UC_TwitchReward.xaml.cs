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
using Songify_Slim.Util.Settings;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace Songify_Slim.UserControls
{
    /// <summary>
    /// Interaction logic for UC_TwitchReward.xaml
    /// </summary>
    public partial class UcTwitchReward
    {
        private readonly CustomReward _reward;

        public UcTwitchReward(CustomReward reward)
        {
            InitializeComponent();
            _reward = reward;
            TxtRewardname.Text = _reward.Title;
            TxtRewardcost.Text = _reward.Cost.ToString();

            if (_reward.BackgroundColor != null)
            {
                try
                {
                    ImgBorder.Background = _reward is { BackgroundColor: not null } ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(_reward.BackgroundColor)!) : GetRandomSolidColorBrush();
                }
                catch (Exception)
                {
                    ImgBorder.Background = GetRandomSolidColorBrush();
                }
            }

            if (_reward.Image != null)
                ImgReward.Source = new BitmapImage(new Uri(_reward.Image.Url1x));
            TglRewardActive.IsOn = Settings.TwRewardId.Any(o => o == _reward.Id);
        }

        private void TglRewardActive_Toggled(object sender, RoutedEventArgs e)
        {
            List<string> tmp = Settings.TwRewardId;
            string rewardId = _reward.Id;
            if (TglRewardActive.IsOn)
            {
                // Only add if it's not already in the list
                if (!tmp.Contains(rewardId))
                {
                    tmp.Add(rewardId);
                }
            }
            else
            {
                tmp.Remove(rewardId);
            }

            Settings.TwRewardId = Settings.TwRewardId;
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