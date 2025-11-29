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
using System.Windows.Shapes;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Views
{
    /// <summary>
    /// Interaction logic for Window_CloudImportPreview.xaml
    /// </summary>
    public partial class Window_CloudImportPreview
    {
        public bool IsConfirmed { get; private set; } = false;
        public int DiffCount { get; private set; } = 0;

        public Window_CloudImportPreview(Configuration local, Configuration incoming)
        {
            InitializeComponent();
            PopulateDiff(local, incoming);
        }

        private void PopulateDiff(Configuration local, Configuration incoming)
        {
            List<string> diffs = ConfigComparer.GetDifferences(local, incoming);
            DiffCount = diffs.Count;

            DiffTextBox.Document.Blocks.Clear();

            if (diffs.Count == 0)
            {
                DiffTextBox.Document.Blocks.Add(new Paragraph(new Run("No differences detected.")));
                return;
            }

            foreach (string diff in diffs)
            {
                Paragraph paragraph = new()
                {
                    Margin = new Thickness(0, 0, 0, 5)
                };

                string[] parts = diff.Split([": "], 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    Run key = new(parts[0] + ": ")
                    {
                        FontWeight = FontWeights.Bold,
                    };
                    paragraph.Inlines.Add(key);

                    string[] valueParts = parts[1].Split([" → "], 2, StringSplitOptions.None);
                    if (valueParts.Length == 2)
                    {
                        // Helper: builds styled UI container
                        InlineUIContainer CreateStyledBlock(string text, Color bg, Color fg)
                        {
                            Border border = new()
                            {
                                Background = new SolidColorBrush(bg),
                                CornerRadius = new CornerRadius(4),
                                Padding = new Thickness(4, 0, 4, 0),
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

                        paragraph.Inlines.Add(CreateStyledBlock(valueParts[0], Color.FromRgb(255, 235, 235), Colors.DarkRed));
                        paragraph.Inlines.Add(new Run(" → "));
                        paragraph.Inlines.Add(CreateStyledBlock(valueParts[1], Color.FromRgb(230, 255, 230), Colors.DarkGreen));
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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.Close();
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.Close();
        }
    }
}