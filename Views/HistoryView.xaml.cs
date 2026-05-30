// ============================================================
// File: HistoryView.xaml.cs
// Project: OpticCli
// Namespace: OpticCli.Views
// Description: Code-behind for the History screen. Loads, filters,
//              renders, and exports past command entries. Supports
//              re-running or modifying any previous command.
// ============================================================
using Microsoft.Win32;
using OpticCli.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpticCli.Views
{
    public partial class HistoryView : Window
    {
        private readonly List<HistoryEntry> _allEntries;

        public HistoryView()
        {
            InitializeComponent();
            _allEntries = OpticCli.HistoryStore.Entries; // assign first
            SearchBox.Text = "";                          // clear placeholder after init
            RenderHistory(_allEntries);
            ExportLogBtn.IsEnabled = _allEntries.Count > 0;
        }


        // Clears and redraws the history list based on the given entries
        private void RenderHistory(List<HistoryEntry> entries)
        {
            HistoryPanel.Children.Clear();

            EntryCountBadge.Text = $"{entries.Count} entries";
            StatusBarEntryCount.Text = $"History · {_allEntries.Count} stored entries · Local DB";

            // Show a message if there's nothing to display
            if (entries.Count == 0)
            {
                HistoryPanel.Children.Add(new TextBlock
                {
                    Text = "No commands executed yet",
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6478")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0),
                });
                return;
            }

            // Warn the user if the 100-entry limit has been reached
            if (entries.Count >= 100 && entries == _allEntries)
            {
                var limitBanner = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1AF59E0B")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4DF59E0B")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 10, 16, 10),
                    Margin = new Thickness(0, 0, 0, 8),
                };
                limitBanner.Child = new TextBlock
                {
                    Text = "⚠  History limit reached (100 entries). Oldest commands are automatically removed.",
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                };
                HistoryPanel.Children.Add(limitBanner);
            }

            // Group entries by date (Today, Yesterday, etc.) and render each group
            var groups = entries.GroupBy(e => e.DateGroup);

            bool isFirst = true;
            foreach (var group in groups)
            {
                // Date group header separator
                var sep = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#070910")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E2430")),
                    BorderThickness = new Thickness(0, isFirst ? 0 : 1, 0, 1),
                    Padding = new Thickness(20, 8, 20, 8),
                };
                var sepText = new TextBlock
                {
                    Text = group.Key,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6478")),
                };
                sep.Child = sepText;
                HistoryPanel.Children.Add(sep);
                isFirst = false;

                foreach (var e in group)
                    HistoryPanel.Children.Add(BuildEntryRow(e));
            }

            EntryCountBadge.Text = $"{entries.Count} entries";
            StatusBarEntryCount.Text = $"History · {_allEntries.Count} stored entries · Local DB";

        }

        // Builds a single history row with hover effects and action buttons
        private UIElement BuildEntryRow(HistoryEntry e)
        {
            // Pick colors based on risk level
            var accentHex = e.Risk switch
            {
                "safe" => "#22C55E",
                "medium" => "#F59E0B",
                "high" => "#EF4444",
                _ => "#5A6478"
            };
            var statusColor = e.Status.StartsWith("✓") ? "#22C55E" : "#EF4444";
            var riskBgHex = e.Risk switch
            {
                "safe" => "#1A22C55E",
                "medium" => "#1AF59E0B",
                "high" => "#1AEF4444",
                _ => "#1AFFFFFF"
            };
            var riskFgHex = e.Risk switch
            {
                "safe" => "#22C55E",
                "medium" => "#F59E0B",
                "high" => "#EF4444",
                _ => "#FFFFFF"
            };
            var riskLabel = e.Risk switch
            {
                "safe" => "Safe",
                "medium" => "Medium",
                "high" => "High",
                _ => "—"
            };

            var row = new Border
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E2430")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Cursor = Cursors.Hand,
            };

            // Hover effect
            row.MouseEnter += (_, _) =>
                row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06FFFFFF"));
            row.MouseLeave += (_, _) =>
                row.Background = new SolidColorBrush(Colors.Transparent);

            var innerGrid = new Grid();
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left accent strip
            var strip = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentHex)),
                Width = 4,
                Opacity = 0.0,   // hidden until hover
            };
            row.MouseEnter += (_, _) => strip.Opacity = 1.0;
            row.MouseLeave += (_, _) => strip.Opacity = 0.0;
            Grid.SetColumn(strip, 0);
            innerGrid.Children.Add(strip);

            // Time column
            var timeBlock = new TextBlock
            {
                Text = e.Time,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6478")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 14, 16, 14),
                MinWidth = 72,
            };
            Grid.SetColumn(timeBlock, 1);
            innerGrid.Children.Add(timeBlock);

            // Content: query + command + status chips
            var content = new StackPanel
            {
                Margin = new Thickness(0, 13, 14, 13),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(content, 2);

            var queryText = new TextBlock
            {
                Text = e.Query,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8D0E0")),
                Margin = new Thickness(0, 0, 0, 4),
            };
            content.Children.Add(queryText);

            var cmdRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            cmdRow.Children.Add(new TextBlock
            {
                Text = "PS› ",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5500E5FF")),
            });
            cmdRow.Children.Add(new TextBlock
            {
                Text = e.Command,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6478")),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 480,
            });
            content.Children.Add(cmdRow);

            // Status, risk, and summary chips
            var metaRow = new StackPanel { Orientation = Orientation.Horizontal };

            metaRow.Children.Add(MakeChip(e.Status, statusColor,
                e.Status.StartsWith("✓") ? "#1A22C55E" : "#1AEF4444",
                e.Status.StartsWith("✓") ? "#4D22C55E" : "#4DEF4444"));

            metaRow.Children.Add(MakeChip($"{char.ToUpper(e.Risk[0])}{e.Risk[1..]} Risk",
                riskFgHex, riskBgHex, riskBgHex.Replace("1A", "4D")));

            metaRow.Children.Add(new TextBlock
            {
                Text = $"  {e.Duration} · {e.ResultSummary}",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6478")),
                VerticalAlignment = VerticalAlignment.Center,
            });

            content.Children.Add(metaRow);
            innerGrid.Children.Add(content);

            // Action buttons (visible on hover)
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0),
                Opacity = 0,
            };
            row.MouseEnter += (_, _) => actions.Opacity = 1;
            row.MouseLeave += (_, _) => actions.Opacity = 0;

            actions.Children.Add(MakeActionBtn("↺  Re-run", "#00E5FF", "#1400E5FF", "#4400E5FF",
    (_, _) => { new OutputView(e.Command, e.Query, e.Risk).Show(); Close(); }
));
            actions.Children.Add(MakeActionBtn("⚙  Modify", "#5A6478", "#08FFFFFF", "#2A3040",
    (_, _) => { new SuggestionsView(e.Query).Show(); Close(); }));
            actions.Children.Add(MakeActionBtn("⎘  Copy", "#5A6478", "#08FFFFFF", "#2A3040",
                (_, _) => Clipboard.SetText(e.Command)));

            Grid.SetColumn(actions, 3);
            innerGrid.Children.Add(actions);

            row.Child = innerGrid;
            return row;
        }

        // ── Helpers ──────────────────────────────────────────

        // Creates a small colored badge/chip for status and risk display
        private static Border MakeChip(string text, string fg, string bg, string border)
        {
            var b = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(border)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            b.Child = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)),
            };
            return b;
        }

        // Creates a styled button used in the hover action panel
        private static Button MakeActionBtn(string label, string fg, string bg, string border,
                                            RoutedEventHandler onClick)
        {
            var btn = new Button
            {
                Content = label,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(border)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 6, 0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Cursor = Cursors.Hand,
            };
            btn.Template = BuildSimpleBtnTemplate();
            btn.Click += onClick;
            return btn;
        }

        // Builds a basic rounded button template used by MakeActionBtn
        private static ControlTemplate BuildSimpleBtnTemplate()
        {
            var t = new ControlTemplate(typeof(Button));
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            factory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            factory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            factory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(cp);
            t.VisualTree = factory;
            return t;
        }

        // ── Event handlers ────────────────────────────────────

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        // Filters the history list as the user types in the search box
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allEntries == null) return; // guard against firing before init

            var q = SearchBox.Text.ToLower();
            var filtered = string.IsNullOrWhiteSpace(q)
                ? _allEntries
                : _allEntries.Where(x =>
                    x.Query.ToLower().Contains(q) ||
                    x.Command.ToLower().Contains(q) ||
                    x.ResultSummary.ToLower().Contains(q)).ToList();
            RenderHistory(filtered);
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Clear all command history? This cannot be undone.",
                "Clear History", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                HistoryStore.Clear();
                _allEntries.Clear();
                RenderHistory(_allEntries); // shows "No commands executed yet"
                ExportLogBtn.IsEnabled = false; // disable export when empty
            }
        }

        private void NewSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            new HomeView().Show();
            Close();
        }
        // Exports history to a .txt or .csv file, capped at 500 entries
        private void ExportLog_Click(object sender, RoutedEventArgs e)
        {
            if (_allEntries == null || _allEntries.Count == 0)
            {
                MessageBox.Show("No data to export. Run some commands first.",
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            const int MaxExportRows = 500;
            bool truncated = _allEntries.Count > MaxExportRows;

            // Warn if over 500
            if (truncated)
            {
                var confirm = MessageBox.Show(
                    $"You have {_allEntries.Count} entries. " +
                    $"Export is limited to the first {MaxExportRows} rows.\n\n" +
                    $"Continue?",
                    "Export Limited to 500 Rows",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;
            }

            // Take max 500 entries
            var entriesToExport = _allEntries.Take(MaxExportRows).ToList();

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text File (*.txt)|*.txt|CSV File (*.csv)|*.csv",
                    Title = "Export Command History",
                    FileName = "OpticCli_History.txt"
                };

                if (saveFileDialog.ShowDialog() != true) return;

                var sb = new StringBuilder();

                if (saveFileDialog.FileName.EndsWith(".csv"))
                {
                    // CSV format with headers
                    sb.AppendLine("Date,Time,Query,Command,Risk,Status,Duration,Summary");
                    foreach (var entry in entriesToExport)
                        sb.AppendLine(
                            $"\"{entry.DateGroup}\",\"{entry.Time}\"," +
                            $"\"{entry.Query}\",\"{entry.Command}\"," +
                            $"\"{entry.Risk}\",\"{entry.Status}\"," +
                            $"\"{entry.Duration}\",\"{entry.ResultSummary}\"");
                }
                else
                {
                    sb.AppendLine("OPTIC CLI - COMMAND HISTORY EXPORT");
                    sb.AppendLine("==================================");
                    if (truncated)
                        sb.AppendLine($"NOTE: Export limited to first {MaxExportRows} of {_allEntries.Count} entries.");
                    sb.AppendLine();
                    foreach (var entry in entriesToExport)
                    {
                        sb.AppendLine($"[{entry.DateGroup} {entry.Time}] {entry.Status}");
                        sb.AppendLine($"Query:   {entry.Query}");
                        sb.AppendLine($"Command: {entry.Command}");
                        sb.AppendLine($"Risk:    {entry.Risk} | Duration: {entry.Duration}");
                        sb.AppendLine($"Summary: {entry.ResultSummary}");
                        sb.AppendLine(new string('-', 60));
                    }
                }

                File.WriteAllText(saveFileDialog.FileName, sb.ToString());
                MessageBox.Show(
                    truncated
                        ? $"Exported first {MaxExportRows} of {_allEntries.Count} entries successfully."
                        : $"Exported {entriesToExport.Count} entries successfully.",
                    "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export log:\n{ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
