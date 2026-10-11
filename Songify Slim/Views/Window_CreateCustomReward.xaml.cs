using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.Twitch;
using TwitchLib.Api.Core.Exceptions;
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
        CreateCustomRewardsResponse response = await TwitchHandler.TwitchApi.Helix.ChannelPoints.CreateCustomRewardsAsync(
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
                SetStatus(
                    Application.Current.TryFindResource("window_createreward_fields_required") as string
                    ?? "Name, prompt, and cost are required.",
                    isError: true);
                return;
            }

            SetBusy(true);
            CreateCustomRewardsResponse response =
                await CreateReward(TbRewardName.Text.Trim(), TbRewardPrompt.Text.Trim(), (int)NudRewardCost.Value);

            if (response?.Data == null || response.Data.Length == 0)
            {
                SetStatus(FormatCreateError(null), isError: true);
                return;
            }

            Settings.TwRewardId.Add(response.Data[0].Id);
            Settings.TwRewardId = Settings.TwRewardId;
            await SettingsUi.RefreshAsync(fullReload: false, loadRewards: true);
        }
        catch (Exception ex)
        {
            string reason = await ReadCreateFailureReason(ex);
            HttpStatusCode? status = ex is HttpResponseException httpEx
                ? httpEx.HttpResponse?.StatusCode
                : null;
            Logger.Error(
                LogSource.Twitch,
                status == null
                    ? "Create custom reward failed. " + (reason ?? "")
                    : $"Create custom reward failed ({(int)status} {status}). {reason}",
                ex);
            SetStatus(FormatCreateError(reason), isError: true);
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

    /// <summary>
    /// Twitch returns one reason in the JSON <c>message</c> field, for example
    /// "the title must be unique amongst all of the broadcaster's custom rewards".
    /// TwitchLib also copies that text into the exception after " - ".
    /// </summary>
    private static async Task<string> ReadCreateFailureReason(Exception ex)
    {
        if (ex is HttpResponseException httpEx && httpEx.HttpResponse?.Content != null)
        {
            try
            {
                string body = await httpEx.HttpResponse.Content.ReadAsStringAsync();
                string message = TryReadTwitchMessage(body);
                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
            catch (Exception)
            {
                // The body was already consumed. Fall through to the exception text.
            }
        }

        string fromBody = TryReadTwitchMessage(ex?.Message);
        if (!string.IsNullOrWhiteSpace(fromBody))
            return fromBody;

        string detail = ex?.Message?.Trim();
        if (string.IsNullOrWhiteSpace(detail))
            return null;

        const string separator = " - ";
        int split = detail.IndexOf(separator, StringComparison.Ordinal);
        if (split >= 0)
        {
            string tail = detail[(split + separator.Length)..].Trim();
            if (!string.IsNullOrWhiteSpace(tail))
                return tail;
        }

        return detail;
    }

    private static string TryReadTwitchMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.TrimStart()[0] != '{')
            return null;

        try
        {
            using JsonDocument doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out JsonElement message)
                && message.ValueKind == JsonValueKind.String)
            {
                string text = message.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static string FormatCreateError(string reason)
    {
        string unable = Application.Current?.TryFindResource("window_createreward_unable") as string
            ?? "Unable to create reward.";
        unable = unable.Trim();
        if (!unable.EndsWith('.'))
            unable += ".";

        if (string.IsNullOrWhiteSpace(reason))
            return unable;

        reason = reason.Trim();
        if (reason.Length > 0 && char.IsLower(reason[0]))
            reason = char.ToUpperInvariant(reason[0]) + reason[1..];
        if (reason.Length > 0 && reason[^1] is not ('.' or '!' or '?'))
            reason += ".";

        return unable + " " + reason;
    }

    private void SetStatus(string text, bool isError)
    {
        LblStatus.Text = text ?? "";
        LblStatus.Foreground = isError
            ? Brushes.IndianRed
            : TryFindResource("SystemFillColorSuccessBrush") as Brush ?? Brushes.ForestGreen;
    }
}
