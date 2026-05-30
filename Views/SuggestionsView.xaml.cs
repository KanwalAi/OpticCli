// ============================================================
// File: SuggestionsView.xaml.cs
// Project: OpticCli
// Namespace: OpticCli.Views
// Description: Code-behind for the Suggestions screen. Fetches AI
//              command suggestions, builds the command cards, handles
//              selection, and navigates to Customize or Output.
// ============================================================

using OpticCli.Models;
using OpticCli.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OpticCli.Views
{
    public partial class SuggestionsView : Window
    {
        private readonly string _query;
        private List<CommandSuggestion> _suggestions;
        private int _selectedIndex = 0;
        private readonly AIService _aiService = new AIService();

        public SuggestionsView(string query)
        {
            InitializeComponent();
            _query = query;
            QueryEchoText.Text = $"\"{query}\"";
            _suggestions = new List<CommandSuggestion>();

            // Show a loading message while waiting for the AI
            ShowLoading();

            Loaded += async (_, _) => await LoadSuggestionsAsync();
        }

        // Shows a placeholder message while AI suggestions are loading
        private void ShowLoading()
        {
            CommandsPanel.Children.Clear();
            var loading = new TextBlock
            {
                Text = "⚡ Asking AI for command suggestions...",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#00E5FF")),
                Margin = new Thickness(0, 30, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            CommandsPanel.Children.Add(loading);
        }

        // Calls the AI service and builds the cards, falls back to defaults on error
        private async System.Threading.Tasks.Task LoadSuggestionsAsync()
        {
            try
            {
                _suggestions = await _aiService.GetSuggestionsAsync(_query);

                if (_suggestions.Count == 0)
                    _suggestions = FallbackSuggestions();

                // Select the first card by default
                _suggestions[0].IsSelected = true;
                _selectedIndex = 0;

                QueryEchoText.Text = $"\"{_query}\"";

                BuildCards();
            }
            catch (System.Exception ex)
            {
                // Show error + fallback suggestions
                _suggestions = FallbackSuggestions();
                _suggestions[0].IsSelected = true;
                BuildCards();

                MessageBox.Show(
                    $"AI service error: {ex.Message}\n\nShowing fallback suggestions.",
                    "AI Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        // Default suggestions shown when the AI call fails
        private static List<CommandSuggestion> FallbackSuggestions() =>
            new List<CommandSuggestion>
            {
                new CommandSuggestion
                {
                    Code        = "Get-Process | Sort-Object CPU -Descending | Select-Object -First 20",
                    Description = "Lists the top 20 CPU-consuming processes. Read-only diagnostic command.",
                    Risk        = RiskLevel.Safe
                },
                new CommandSuggestion
                {
                    Code        = "Get-PSDrive -PSProvider FileSystem | Select-Object Name, Used, Free",
                    Description = "Shows disk usage for all drives including used and free space.",
                    Risk        = RiskLevel.Safe
                }
            };

        // Builds and renders all command cards based on current suggestions
        private void BuildCards()
        {
            CommandsPanel.Children.Clear();

            for (int i = 0; i < _suggestions.Count; i++)
            {
                var s = _suggestions[i];
                int idx = i;

                var accentColor = s.Risk switch
                {
                    RiskLevel.Safe => Color.FromRgb(0x22, 0xC5, 0x5E),
                    RiskLevel.Medium => Color.FromRgb(0xF5, 0x9E, 0x0B),
                    RiskLevel.High => Color.FromRgb(0xEF, 0x44, 0x44),
                    _ => Colors.Transparent
                };
                var riskFg = s.Risk switch
                {
                    RiskLevel.Safe => "#22C55E",
                    RiskLevel.Medium => "#F59E0B",
                    RiskLevel.High => "#EF4444",
                    _ => "#FFFFFF"
                };
                var riskBg = s.Risk switch
                {
                    RiskLevel.Safe => "#1A22C55E",
                    RiskLevel.Medium => "#1AF59E0B",
                    RiskLevel.High => "#1AEF4444",
                    _ => "#00000000"
                };
                var riskBd = s.Risk switch
                {
                    RiskLevel.Safe => "#4D22C55E",
                    RiskLevel.Medium => "#4DF59E0B",
                    RiskLevel.High => "#4DEF4444",
                    _ => "#00000000"
                };

                // Card border — highlighted if selected
                var card = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    BorderThickness = new Thickness(1),
                    BorderBrush = s.IsSelected
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4400E5FF"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E2430")),
                    Background = s.IsSelected
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A00E5FF"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#05FFFFFF")),
                    Margin = new Thickness(0, 0, 0, 10),
                    Cursor = Cursors.Hand,
                    Tag = idx
                };
                card.MouseLeftButtonDown += (_, _) => SelectCard(idx);

                var innerGrid = new Grid();
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Left color strip indicates risk level
                var strip = new Border
                {
                    Background = new SolidColorBrush(accentColor),
                    CornerRadius = new CornerRadius(12, 0, 0, 12),
                    Width = 4
                };
                Grid.SetColumn(strip, 0);
                innerGrid.Children.Add(strip);

                var content = new StackPanel { Margin = new Thickness(16, 14, 14, 14) };
                Grid.SetColumn(content, 1);

                var topRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 6)
                };

                // Radio circle — filled when selected
                var radio = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Stroke = s.IsSelected
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E5FF"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A3040")),
                    StrokeThickness = 2,
                    Fill = s.IsSelected
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A00E5FF"))
                        : new SolidColorBrush(Colors.Transparent),
                    Margin = new Thickness(0, 2, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (s.IsSelected)
                {
                    var innerDot = new Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#00E5FF")),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    var radioGrid = new Grid
                    {
                        Width = 18,
                        Height = 18,
                        Margin = new Thickness(0, 2, 12, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    radioGrid.Children.Add(radio);
                    radioGrid.Children.Add(innerDot);
                    topRow.Children.Add(radioGrid);
                }
                else
                    topRow.Children.Add(radio);

                var psPrompt = new TextBlock
                {
                    Text = "PS› ",
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#6600E5FF")),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var cmdText = new TextBlock
                {
                    Text = s.Code,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var codeRow = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
                codeRow.Children.Add(psPrompt);
                codeRow.Children.Add(cmdText);
                topRow.Children.Add(codeRow);
                content.Children.Add(topRow);

                var desc = new TextBlock
                {
                    Text = s.Description,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#5A6478")),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(30, 0, 0, 0),
                    LineHeight = 18
                };
                content.Children.Add(desc);
                innerGrid.Children.Add(content);

                // Risk badge on the right side of the card
                var badge = new Border
                {
                    CornerRadius = new CornerRadius(20),
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(riskBg)),
                    BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(riskBd)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 5, 12, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 16, 0)
                };
                Grid.SetColumn(badge, 2);
                var badgeText = new TextBlock
                {
                    Text = s.RiskLabel,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(riskFg))
                };
                badge.Child = badgeText;
                innerGrid.Children.Add(badge);

                card.Child = innerGrid;
                CommandsPanel.Children.Add(card);
            }

            UpdateSelInfo();
        }

        // Updates which card is selected and redraws the list
        private void SelectCard(int idx)
        {
            for (int i = 0; i < _suggestions.Count; i++)
                _suggestions[i].IsSelected = (i == idx);
            _selectedIndex = idx;
            BuildCards();
        }

        // Updates the "X command selected" label at the bottom
        private void UpdateSelInfo()
        {
            SelInfo.Text = $"{_suggestions.FindAll(s => s.IsSelected).Count} command selected";
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void CustomizeBtn_Click(object sender, RoutedEventArgs e)
        {
            var sel = _selectedIndex < _suggestions.Count
                ? _suggestions[_selectedIndex] : null;
            new CustomizeView(sel, _query).Show();
            Close();
        }

        private void RunDirectBtn_Click(object sender, RoutedEventArgs e)
        {
            var sel = _selectedIndex < _suggestions.Count
                ? _suggestions[_selectedIndex] : null;

            // Block high risk commands from running without confirmation
            if (sel != null && sel.Risk == RiskLevel.High)
            {
                var result = MessageBox.Show(
                    "⚠ WARNING: This is a HIGH RISK command.\n\n" +
                    $"Command: {sel.Code}\n\n" +
                    "This command may delete files or modify critical system settings.\n" +
                    "Are you absolutely sure you want to run this directly?",
                    "Please review and confirm the risk before proceeding.",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    SelInfo.Text = "⚠ Execution blocked. High-risk command requires confirmation.";
                    return;
                }
            }

            // Proceed to output screen
            var riskStr = sel?.Risk switch
            {
                RiskLevel.Medium => "medium",
                RiskLevel.High => "high",
                _ => "safe"
            };
            new OutputView(sel?.Code ?? "", _query, riskStr).Show();
            Close();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            new HomeView().Show();
            Close();
        }
        private void HistoryBtn_Click(object sender, RoutedEventArgs e)
        {
            new HistoryView().Show();
            this.Close();
        }
        private void NewSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            new HomeView().Show();
            this.Close();
        }
    }
}