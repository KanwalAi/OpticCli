// ============================================================
// File: CustomizeView.xaml.cs
// Project: OpticCli
// Namespace: OpticCli.Views
// Description: Code-behind for the Customize screen. Handles
//              parameter inputs, live command preview updates,
//              risk banner display, and final command execution.
// ============================================================
using OpticCli.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace OpticCli.Views
{
    public partial class CustomizeView : Window
    {
        private readonly CommandSuggestion? _suggestion;
        private readonly string _query;

        public CustomizeView(CommandSuggestion? suggestion, string query)
        {
            InitializeComponent();
            _suggestion = suggestion;
            _query = query;

            if (suggestion != null)
            {
                var code = suggestion.Code;

                // Check what type of command this is so we show the right options
                bool isFileSearch = code.StartsWith("Get-ChildItem") ||
                                    code.StartsWith("gci") ||
                                    code.StartsWith("dir ") ||
                                    code.StartsWith("ls ");

                bool isMkdir = code.StartsWith("mkdir") ||
                               code.StartsWith("md ") ||
                               code.StartsWith("New-Item");

                // Pre-populate path from AI command
                if (code.Contains("C:\\"))
                {
                    var startIdx = code.IndexOf("C:\\");
                    var endIdx = code.IndexOf(' ', startIdx);
                    if (endIdx < 0) endIdx = code.Length;
                    PathInput.Text = code.Substring(startIdx, endIdx - startIdx).Trim();
                }
                else if (isMkdir)
                {
                    // For New-Item, pre-fill with user's home directory
                    PathInput.Text = System.Environment.GetFolderPath(
                        System.Environment.SpecialFolder.UserProfile);
                }

                // Pre-populate filter
                if (code.Contains("-Filter "))
                {
                    var fi = code.IndexOf("-Filter ") + 8;
                    ExtInput.Text = code.Substring(fi).Split(' ')[0];
                }

                // Set checkboxes from command
                RecurseCheck.IsChecked = code.Contains("-Recurse");
                ForceCheck.IsChecked = code.Contains("-Force");

                // Hide the Options panel if this isn't a file search command
                OptionsPanel.Visibility = isFileSearch
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // Set command info
                CmdInfoText.Text = suggestion.Description;

                // Update risk banner
                UpdateRiskBanner(suggestion.Risk);

                // Update status bar
                var riskLabel = suggestion.Risk switch
                {
                    RiskLevel.Safe => "Safe",
                    RiskLevel.Medium => "Medium",
                    RiskLevel.High => "High Risk",
                    _ => "Unknown"
                };
                StatusBarCmd.Text = $"Command: {code.Split(' ')[0]} · Risk: {riskLabel}";
            }

            UpdatePreview();
        }

        // Changes the risk banner color and message based on the risk level
        private void UpdateRiskBanner(RiskLevel risk)
        {
            switch (risk)
            {
                case RiskLevel.Safe:
                    RiskBanner.Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#1A22C55E"));
                    RiskBanner.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#4D22C55E"));
                    RiskIcon.Text = "✅";
                    RiskTitle.Text = "Safe Command";
                    RiskTitle.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#22C55E"));
                    RiskDesc.Text = "This is a read-only command. It will not modify, delete, or move any files.";
                    RiskDesc.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#22C55E"));
                    break;

                case RiskLevel.Medium:
                    RiskBanner.Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#1AF59E0B"));
                    RiskBanner.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#4DF59E0B"));
                    RiskIcon.Text = "⚠";
                    RiskTitle.Text = "Medium Risk";
                    RiskTitle.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#FCD34D"));
                    RiskDesc.Text = "This command modifies files or processes. Review parameters carefully before running.";
                    RiskDesc.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#FCD34D"));
                    break;

                case RiskLevel.High:
                    RiskBanner.Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#1AEF4444"));
                    RiskBanner.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#4DEF4444"));
                    RiskIcon.Text = "🔴";
                    RiskTitle.Text = "High Risk — Destructive Command";
                    RiskTitle.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#EF4444"));
                    RiskDesc.Text = "WARNING: This command may delete files or require admin privileges. Proceed with extreme caution.";
                    RiskDesc.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#EF4444"));
                    break;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        // Rebuilds the live command preview whenever any input changes
        private void UpdatePreview()
        {
            if (RunBaseName == null) return;

            if (_suggestion != null)
            {
                var parts = _suggestion.Code.Split(' ');
                RunBaseName.Text = parts[0];
                RunRecurse.Text = (RecurseCheck?.IsChecked == true) ? " -Recurse" : "";
                RunForce.Text = (ForceCheck?.IsChecked == true) ? " -Force" : "";
                RunPath.Text = string.IsNullOrWhiteSpace(PathInput?.Text)
                    ? "" : $" {PathInput.Text}";
                RunFilter.Text = string.IsNullOrWhiteSpace(ExtInput?.Text)
                    ? "" : $" -Filter {ExtInput.Text}";
            }
            else
            {
                RunBaseName.Text = PathInput?.Text ?? "";
                RunRecurse.Text = "";
                RunPath.Text = "";
                RunForce.Text = "";
                RunFilter.Text = "";
            }
        }

        private void PathInput_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e) => UpdatePreview();

        private void ExtInput_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e) => UpdatePreview();

        private void RecurseCheck_Changed(object sender, RoutedEventArgs e)
            => UpdatePreview();

        private void ForceCheck_Changed(object sender, RoutedEventArgs e)
            => UpdatePreview();

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            // Block high risk commands until the user explicitly confirms
            if (_suggestion != null && _suggestion.Risk == RiskLevel.High)
            {
                var result = MessageBox.Show(
                    "⚠ WARNING: This is a HIGH RISK command.\n\n" +
                    "This command may delete files, modify system settings, or require admin privileges.\n\n" +
                    "Are you absolutely sure you want to proceed?",
                    "Please review and confirm the risk before proceeding.",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    StatusBarCmd.Text = "Execution blocked. High-risk command requires confirmation.";
                    return;
                }
            }

            // Build the final command and open the output window
            var finalCmd = BuildFinalCommand();
            var riskStr = _suggestion?.Risk switch
            {
                RiskLevel.Medium => "medium",
                RiskLevel.High => "high",
                _ => "safe"
            };
            new OutputView(finalCmd, _query, riskStr).Show();
            Close();
        }

        private void CopyBtn_Click(object sender, RoutedEventArgs e)
            => Clipboard.SetText(BuildFinalCommand());

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            new SuggestionsView(_query).Show();
            Close();
        }

        // Applies the user's GUI inputs on top of the original AI command
        private string BuildFinalCommand()
        {
            if (_suggestion == null)
                return PathInput.Text.Trim();

            var baseCmd = _suggestion.Code;

            bool isFileSearch = baseCmd.StartsWith("Get-ChildItem") ||
                                baseCmd.StartsWith("gci") ||
                                baseCmd.StartsWith("dir ") ||
                                baseCmd.StartsWith("ls ");

            bool isMkdir = baseCmd.StartsWith("mkdir") ||
                           baseCmd.StartsWith("md ") ||
                           baseCmd.StartsWith("New-Item -ItemType Directory") ||
                           baseCmd.StartsWith("New-Item -ItemType File");

            // Handle folder/file creation commands
            if (isMkdir)
            {
                if (!string.IsNullOrWhiteSpace(PathInput.Text))
                {
                    var parts = baseCmd.Split(' ');
                    var folderName = parts.Length > 1 ? parts[1] : "NewFolder";
                    var targetPath = PathInput.Text.TrimEnd('\\');
                    return $"mkdir \"{targetPath}\\{folderName}\"";
                }
                return baseCmd;
            }

            // Non-file-search — return as-is
            if (!isFileSearch)
                return baseCmd;

            // File search — apply GUI parameters
            var cmd = baseCmd;

            if (!string.IsNullOrWhiteSpace(PathInput.Text))
            {
                var pathPattern = @"[A-Za-z]:\\[^\s]*";
                var match = System.Text.RegularExpressions.Regex.Match(cmd, pathPattern);
                if (match.Success)
                    cmd = cmd.Replace(match.Value, PathInput.Text);
                else
                    cmd += $" {PathInput.Text}";
            }

            if (RecurseCheck.IsChecked == true && !cmd.Contains("-Recurse"))
                cmd += " -Recurse";
            else if (RecurseCheck.IsChecked == false)
                cmd = cmd.Replace(" -Recurse", "");

            if (ForceCheck.IsChecked == true && !cmd.Contains("-Force"))
                cmd += " -Force";
            else if (ForceCheck.IsChecked == false)
                cmd = cmd.Replace(" -Force", "");

            if (!string.IsNullOrWhiteSpace(ExtInput.Text))
            {
                var filterPattern = @"-Filter\s+\S+";
                if (cmd.Contains("-Filter"))
                    cmd = System.Text.RegularExpressions.Regex.Replace(
                        cmd, filterPattern, $"-Filter {ExtInput.Text}");
                else
                    cmd += $" -Filter {ExtInput.Text}";
            }

            return cmd;
        }

        private void ComboBox_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        { }
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