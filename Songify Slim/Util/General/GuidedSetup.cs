using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Views;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Util.General;

internal sealed record SetupChecklistItem(string Id, string Title, bool IsDone, bool IsRequired, string SettingsTab);

/// <summary>First-launch wizard + Overview checklist state.</summary>
internal static class GuidedSetup
{
    public const int CurrentWizardVersion = 1;

    public static bool ShouldShowWizard()
    {
        MigrateExistingUsersIfNeeded();
        return !Settings.SetupCompleted;
    }

    public static void MigrateExistingUsersIfNeeded()
    {
        if (Settings.SetupCompleted)
            return;

        if (!LooksLikeExistingInstall())
            return;

        Settings.SetupCompleted = true;
        Settings.SetupWizardVersion = CurrentWizardVersion;
    }

    public static bool LooksLikeExistingInstall() =>
        Settings.UseOwnApp ||
        !string.IsNullOrWhiteSpace(Settings.ClientId) ||
        !string.IsNullOrWhiteSpace(Settings.TwitchAccessToken) ||
        !string.IsNullOrWhiteSpace(Settings.SpotifyRefreshToken);

    public static void MarkCompleted()
    {
        Settings.UseOwnApp = true;
        Settings.SetupCompleted = true;
        Settings.SetupWizardVersion = CurrentWizardVersion;
    }

    public static string DefaultOutputFilePath()
    {
        if (string.IsNullOrEmpty(Settings.Directory))
            return Path.Combine(AppPaths.GetAppDirectory(), "Songify.txt");
        return Path.Combine(Settings.Directory, "Songify.txt");
    }

    public static bool IsOutputReady() =>
        !string.IsNullOrWhiteSpace(DefaultOutputFilePath());

    public static IReadOnlyList<SetupChecklistItem> GetChecklistItems()
    {
        bool spotifyPlayer = Settings.Player == PlayerType.Spotify;
        string Loc(string key, string fallback) =>
            Application.Current?.TryFindResource(key) as string ?? fallback;

        List<SetupChecklistItem> items = [];
        if (spotifyPlayer)
        {
            items.Add(new SetupChecklistItem(
                "spotify",
                Loc("setup_checklist_spotify", "Link Spotify"),
                AccountLinking.IsSpotifyLinked(),
                IsRequired: true,
                "Spotify"));
        }

        items.Add(new SetupChecklistItem(
            "twitch",
            Loc("setup_checklist_twitch", "Link Twitch (for song requests)"),
            AccountLinking.IsTwitchMainLinked(),
            IsRequired: false,
            "Twitch"));

        items.Add(new SetupChecklistItem(
            "output",
            Loc("setup_checklist_output", "Song output file (OBS)"),
            IsOutputReady(),
            IsRequired: false,
            "Output"));

        return items;
    }

    public static bool ShouldShowChecklist()
    {
        if (Settings.SetupChecklistDismissed)
            return false;

        foreach (SetupChecklistItem item in GetChecklistItems())
        {
            if (!item.IsDone && (item.IsRequired || item.Id == "twitch"))
                return true;
        }

        return false;
    }

    /// <returns><c>true</c> if the user asked to start the in-app orientation tour.</returns>
    public static Task<bool> ShowWizardAsync(Window owner)
    {
        Window existing = null;
        if (Application.Current != null)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is WindowSetupWizard found)
                {
                    existing = found;
                    break;
                }
            }
        }

        if (existing != null)
        {
            existing.Activate();
            return Task.FromResult(false);
        }

        WindowSetupWizard wizard = new()
        {
            Owner = owner ?? Application.Current?.MainWindow
        };
        wizard.ShowDialog();
        return Task.FromResult(wizard.StartTourRequested);
    }
}
