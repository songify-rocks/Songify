using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Songify_Slim.Util.General
{
    public static class ServiceToolTip
    {
        public static ToolTip Build(
        string header,
        IEnumerable<(string Label, string Value)> rows,
        Style style = null,
        PackIconBoxIcons icon = null)
        {
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int r = 0;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header row with optional icon + text
            StackPanel headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };

            if (icon != null)
            {
                headerPanel.Children.Add(CloneIcon(icon));
            }

            headerPanel.Children.Add(new TextBlock
            {
                Text = header,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground =
                    Application.Current.TryFindResource("MahApps.Brushes.ThemeForeground") as Brush
                    ?? SystemColors.ControlTextBrush,
            });

            Grid.SetRow(headerPanel, r);
            Grid.SetColumnSpan(headerPanel, 2);
            grid.Children.Add(headerPanel);
            r++;

            // Data rows
            foreach ((string label, string value) in rows)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                TextBlock lbl = new TextBlock
                {
                    Text = label + ":",
                    Opacity = 0.8,
                    Margin = new Thickness(0, 0, 8, 2),
                    VerticalAlignment = VerticalAlignment.Top,
                    Foreground =
                        Application.Current.TryFindResource("MahApps.Brushes.ThemeForeground") as Brush
                        ?? SystemColors.ControlTextBrush,
                };
                Grid.SetRow(lbl, r);
                Grid.SetColumn(lbl, 0);

                TextBlock val = new TextBlock
                {
                    Text = value ?? "—",
                    VerticalAlignment = VerticalAlignment.Top
                    ,
                    Foreground =
                        Application.Current.TryFindResource("MahApps.Brushes.ThemeForeground") as Brush
                        ?? SystemColors.ControlTextBrush,
                };
                Grid.SetRow(val, r);
                Grid.SetColumn(val, 1);

                grid.Children.Add(lbl);
                grid.Children.Add(val);
                r++;
            }

            return new ToolTip { Content = grid, Style = style };
        }

        // Clone the passed icon so it can be used inside the tooltip
        private static PackIconBoxIcons CloneIcon(PackIconBoxIcons src)
        {
            // Height defaults to Width if not set; tweak as you like
            PackIconBoxIcons clone = new PackIconBoxIcons
            {
                Kind = src.Kind,
                Width = src.Width > 0 ? src.Width : 14,
                Height = src.Height > 0 ? src.Height : (src.Width > 0 ? src.Width : 14),
                Foreground =
                    Application.Current.TryFindResource("MahApps.Brushes.ThemeForeground") as Brush
                    ?? SystemColors.ControlTextBrush,
                Opacity = src.Opacity,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            // If you tint via Fill/Brushes elsewhere, mirror here as needed.

            return clone;
        }
    }
}
