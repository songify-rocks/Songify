using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.Twitch;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using Wpf.Ui.Controls;

namespace Songify_Slim.Views;

/// <summary>
/// Modal dialog to create a Twitch channel-point reward.
/// </summary>
public partial class WindowCreateCustomReward
{
    private bool _isBusy;

    public WindowCreateCustomReward()
    {
        InitializeComponent();
        ThemeHandler.ApplyTheme();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public async Task<CreateCustomRewardsResponse> CreateReward(string name, string prompt, int cost)
    {
        CreateCustomRewardsResponse response;
        try
        {
            response = await TwitchHandler.TwitchApi.Helix.ChannelPoints.CreateCustomRewardsAsync(
                Settings.TwitchChannelId,
                new CreateCustomRewardsRequest
                {
                    Title = name,
                    Prompt = prompt,
                    Cost = cost,
                    IsEnabled = true,
                    BackgroundColor = "#1ed760",
                    IsUserInputRequired = TglUserInputRequired.IsChecked == true,
                    IsMaxPerStreamEnabled = false,
                    MaxPerStream = null,
                    IsMaxPerUserPerStreamEnabled = false,
                    MaxPerUserPerStream = null,
                    IsGlobalCooldownEnabled = false,
                    GlobalCooldownSeconds = null,
                    ShouldRedemptionsSkipRequestQueue = false
                },
                Settings.TwitchAccessToken);
        }
        catch (Exception)
        {
            SetStatus(Properties.Resources.window_createreward_error, isError: true);
            return null;
        }

        SetStatus(
            Properties.Resources.window_createreward_success.Replace("{name}", name),
            isError: false);
        ShellHelper.OpenUrl("https://dashboard.twitch.tv/viewer-rewards/channel-points/rewards");
        return response;
    }

    private async void BtnCreateReward_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            if (string.IsNullOrWhiteSpace(TbRewardName.Text)
                || string.IsNullOrWhiteSpace(TbRewardPrompt.Text)
                || NudRewardCost.Value == null)
            {
                SetStatus("Name, prompt, and cost are required.", isError: true);
                return;
            }

            SetBusy(true);
            CreateCustomRewardsResponse response =
                await CreateReward(TbRewardName.Text.Trim(), TbRewardPrompt.Text.Trim(), (int)NudRewardCost.Value);

            if (response == null)
            {
                SetStatus("Unable to create Reward.", isError: true);
                return;
            }

            Settings.TwRewardId.Add(response.Data[0].Id);
            Settings.TwRewardId = Settings.TwRewardId;
            await SettingsUi.RefreshAsync(fullReload: false, loadRewards: true);
        }
        catch (Exception ex)
        {
            SetStatus(Properties.Resources.window_createreward_error + " " + ex.Message, isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        BtnCreateReward.IsEnabled = !busy;
        BtnCancel.IsEnabled = !busy;
        TbRewardName.IsEnabled = !busy;
        TbRewardPrompt.IsEnabled = !busy;
        NudRewardCost.IsEnabled = !busy;
        TglUserInputRequired.IsEnabled = !busy;
    }

    private void SetStatus(string text, bool isError)
    {
        LblStatus.Text = text ?? "";
        LblStatus.Foreground = isError
            ? Brushes.IndianRed
            : TryFindResource("SystemFillColorSuccessBrush") as Brush ?? Brushes.ForestGreen;
    }
}
