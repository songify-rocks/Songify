using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TextBlock = System.Windows.Controls.TextBlock;
using System.Windows.Media;
using SymbolIcon = Wpf.Ui.Controls.SymbolIcon;

namespace Songify_Slim.Util.General
{
    public static class ServiceToolTip
    {
        public static ToolTip Build(
            string header,
            IEnumerable<(string Label, string Value)> rows,
            Style style = null,
            SymbolIcon icon = null)
        {
            Grid grid = new();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int r = 0;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            StackPanel headerPanel = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };

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
                    Application.Current.TryFindResource("TextFillColorPrimaryBrush") as Brush
                    ?? SystemColors.ControlTextBrush,
            });

            Grid.SetRow(headerPanel, r);
            Grid.SetColumnSpan(headerPanel, 2);
            grid.Children.Add(headerPanel);
            r++;

            foreach ((string label, string value) in rows)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                TextBlock lbl = new()
                {
                    Text = label + ":",
                    Opacity = 0.8,
                    Margin = new Thickness(0, 0, 8, 2),
                    VerticalAlignment = VerticalAlignment.Top,
                    Foreground =
                        Application.Current.TryFindResource("TextFillColorPrimaryBrush") as Brush
                        ?? SystemColors.ControlTextBrush,
                };
                Grid.SetRow(lbl, r);
                Grid.SetColumn(lbl, 0);

                TextBlock val = new()
                {
                    Text = value ?? "—",
                    VerticalAlignment = VerticalAlignment.Top,
                    Foreground =
                        Application.Current.TryFindResource("TextFillColorPrimaryBrush") as Brush
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

        private static SymbolIcon CloneIcon(SymbolIcon src)
        {
            return new SymbolIcon
            {
                Symbol = src.Symbol,
                Width = src.Width > 0 ? src.Width : 14,
                Height = src.Height > 0 ? src.Height : (src.Width > 0 ? src.Width : 14),
                Foreground =
                    Application.Current.TryFindResource("TextFillColorPrimaryBrush") as Brush
                    ?? SystemColors.ControlTextBrush,
                Opacity = src.Opacity,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Filled = src.Filled,
            };
        }
    }
}
