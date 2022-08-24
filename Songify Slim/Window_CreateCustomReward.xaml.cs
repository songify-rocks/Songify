using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Songify_Slim.Util.Songify;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;

namespace Songify_Slim
{
    /// <summary>
    /// Interaction logic for Window_CreateCustomReward.xaml
    /// </summary>
    public partial class Window_CreateCustomReward
    {
        public Window_CreateCustomReward()
        {
            InitializeComponent();
        }

        public async void CreateReward()
        {
            CreateCustomRewardsResponse response = await TwitchHandler._twitchApi.Helix.ChannelPoints.CreateCustomRewardsAsync(Settings.TwitchChannelId,
                new CreateCustomRewardsRequest
                {
                    Title = null,
                    Prompt = null,
                    Cost = 0,
                    IsEnabled = false,
                    BackgroundColor = null,
                    IsUserInputRequired = false,
                    IsMaxPerStreamEnabled = false,
                    MaxPerStream = null,
                    IsMaxPerUserPerStreamEnabled = false,
                    MaxPerUserPerStream = null,
                    IsGlobalCooldownEnabled = false,
                    GlobalCooldownSeconds = null,
                    ShouldRedemptionsSkipRequestQueue = false
                }, Settings.TwitchAccessToken);
            if (response != null)
                Debug.WriteLine(response);
        }
    }
}
