using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;

namespace Songify_Slim.Views;

/// <summary>
/// Preview of cloud settings differences before import.
/// </summary>
public partial class Window_CloudImportPreview
{
    public bool IsConfirmed { get; private set; }
    public int DiffCount { get; private set; }

    public Window_CloudImportPreview(Configuration local, Configuration incoming)
    {
        InitializeComponent();
        ThemeHandler.ApplyTheme();
        PopulateDiff(local, incoming);
    }

    private static string Loc(string key, string fallback)
        => Application.Current?.TryFindResource(key) as string ?? fallback;

    private void PopulateDiff(Configuration local, Configuration incoming)
    {
        List<string> diffs = ConfigComparer.GetDifferences(local, incoming);
        DiffCount = diffs.Count;

        List<string> permissionWarnings = ConfigComparer.GetPermissionWideningWarnings(local, incoming);
        if (permissionWarnings.Count > 0)
        {
            PermissionWarningBanner.Visibility = Visibility.Visible;
            string header = Loc(
                "window_cloudimport_permission_body",
                "This import widens who can use some Twitch commands or song requests:");
            TbPermissionWarnings.Text = header + "\n• " + string.Join("\n• ", permissionWarnings);
        }
        else
        {
            PermissionWarningBanner.Visibility = Visibility.Collapsed;
            TbPermissionWarnings.Text = "";
        }

        DiffTextBox.Document.Blocks.Clear();

        if (diffs.Count == 0)
        {
            DiffTextBox.Document.Blocks.Add(new Paragraph(new Run(
                Loc("window_cloudimport_no_differences", "No differences detected."))));
            BtnImport.IsEnabled = false;
            return;
        }

        bool dark = IsDarkTheme();
        Color oldBg = dark ? Color.FromRgb(0x4A, 0x1C, 0x1C) : Color.FromRgb(0xFF, 0xEB, 0xEB);
        Color oldFg = dark ? Color.FromRgb(0xFF, 0x8A, 0x80) : Color.FromRgb(0xB7, 0x1C, 0x1C);
        Color newBg = dark ? Color.FromRgb(0x1B, 0x3A, 0x1B) : Color.FromRgb(0xE6, 0xFF, 0xE6);
        Color newFg = dark ? Color.FromRgb(0xA5, 0xD6, 0xA7) : Color.FromRgb(0x1B, 0x5E, 0x20);

        foreach (string diff in diffs)
        {
            Paragraph paragraph = new()
            {
                Margin = new Thickness(0, 0, 0, 6)
            };

            string[] parts = diff.Split([": "], 2, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                paragraph.Inlines.Add(new Run(parts[0] + ": ")
                {
                    FontWeight = FontWeights.SemiBold
                });

                string[] valueParts = parts[1].Split([" → "], 2, StringSplitOptions.None);
                if (valueParts.Length == 2)
                {
                    paragraph.Inlines.Add(CreateStyledBlock(valueParts[0], oldBg, oldFg));
                    paragraph.Inlines.Add(new Run(" → "));
                    paragraph.Inlines.Add(CreateStyledBlock(valueParts[1], newBg, newFg));
                }
                else
                {
                    paragraph.Inlines.Add(new Run(parts[1]));
                }
            }
            else
            {
                paragraph.Inlines.Add(new Run(diff));
            }

            DiffTextBox.Document.Blocks.Add(paragraph);
        }
    }

    private static bool IsDarkTheme()
    {
        try
        {
            return Settings.Theme is "Dark" or "BaseDark";
        }
        catch
        {
            return true;
        }
    }

    private static InlineUIContainer CreateStyledBlock(string text, Color bg, Color fg)
    {
        Border border = new()
        {
            Background = new SolidColorBrush(bg),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 1, 6, 1),
            Margin = new Thickness(2, 0, 2, 0),
            Child = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(fg),
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        return new InlineUIContainer(border)
        {
            BaselineAlignment = BaselineAlignment.Center
        };
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close();
    }

    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = true;
        Close();
    }
}
