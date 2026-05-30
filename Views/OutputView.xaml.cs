// ============================================================
// File: OutputView.xaml.cs
// Project: OpticCli
// Namespace: OpticCli.Views
// Description: Code-behind for the Output screen. Runs the command,
//              parses results into table or raw view, handles export
//              to CSV/JSON, and shows AI error explanations on failure.
// ============================================================

using OpticCli.Models;
using OpticCli.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace OpticCli.Views
{
    // Represents one row in the results DataGrid with color-coded type badges
    public class FileRow
    {
        public string Icon { get; set; } = "📄";
        public string Name { get; set; } = "";
        public string Size { get; set; } = "";
        public string Type { get; set; } = "";
        public string Modified { get; set; } = "";
        public string Path { get; set; } = "";

        public SolidColorBrush TypeBadgeBg => TypeColors().bg;
        public SolidColorBrush TypeBadgeFg => TypeColors().fg;
        public SolidColorBrush TypeBadgeBd => TypeColors().bd;

        // Returns background, foreground, and border colors based on file type
        private (SolidColorBrush bg, SolidColorBrush fg, SolidColorBrush bd) TypeColors()
        {
            switch (Type)
            {
                case "PDF": return (Brush("#1AEF4444"), Brush("#FCA5A5"), Brush("#4DEF4444"));
                case "DOCX": return (Brush("#1A3B82F6"), Brush("#93C5FD"), Brush("#4D3B82F6"));
                case "XLSX": return (Brush("#1A22C55E"), Brush("#86EFAC"), Brush("#4D22C55E"));
                case "IMG": return (Brush("#1AF59E0B"), Brush("#FCD34D"), Brush("#4DF59E0B"));
                case "PPTX": return (Brush("#1AF97316"), Brush("#FDBA74"), Brush("#4DF97316"));
                default: return (Brush("#1AFFFFFF"), Brush("#C8D0E0"), Brush("#2AFFFFFF"));
            }
        }

        private static SolidColorBrush Brush(string hex)
            => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }

    public partial class OutputView : Window
    {
        private readonly string _query;
        private readonly string _command;
        private readonly string _risk;
        private List<FileRow> _allRows = new List<FileRow>();
        private ObservableCollection<FileRow> _displayed;
        private readonly ShellWrapper _shell = new ShellWrapper();
        private readonly AIService _aiService = new AIService();
        private string _rawOutput = "";

        public OutputView(string command, string query, string risk = "safe")
        {
            InitializeComponent();
            _query = command;
            _command = EnhanceCommand(command);
            _risk = risk;

            ExecutedCmdText.Text = command.StartsWith("PS›")
                ? command : $"PS› {command}";

            _displayed = new ObservableCollection<FileRow>();
            ResultsGrid.ItemsSource = _displayed;
            ResultsGrid.SelectionChanged += (_, _) =>
                SelCount.Text = $"  ·  {ResultsGrid.SelectedItems.Count} selected";

            Loaded += async (_, _) => await ExecuteCommandAsync();
        }
        // Appends FullName expansion to file search commands so we get full paths
        private static string EnhanceCommand(string command)
        {

            bool isFileSearch = command.TrimStart().StartsWith("Get-ChildItem") ||
                                command.TrimStart().StartsWith("gci ");

            bool alreadyHasFullName = command.Contains("FullName") ||
                                      command.Contains("Select-Object");

            if (isFileSearch && !alreadyHasFullName)
                return command + " | Select-Object -ExpandProperty FullName";

            return command;
        }
        // Runs the command, updates the UI with results or error info, and saves to history
        private async System.Threading.Tasks.Task ExecuteCommandAsync()
        {
            var startTime = DateTime.Now;

            // Show executing state
            BannerIcon.Text = "⏳";
            BannerStatusText.Text = "Executing command...";
            StatusBarText.Text = "Running...";

            var result = await _shell.ExecuteCommandAsync(_command);
            var elapsed = (DateTime.Now - startTime).TotalSeconds;

            _rawOutput = result;

            // Update elapsed time
            ElapsedText.Text = $"{elapsed:F2}s";
            TimePillText.Text = $"{elapsed:F2}";

            var lines = result.Split(new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            OutputLinesNum.Text = lines.Length.ToString();

            // Check for error
            if (result.StartsWith("[ERROR]") || result.StartsWith("[SYSTEM EXCEPTION]"))
            {
                BannerIcon.Text = "❌";
                BannerStatusText.Text = "Command failed";
                StatusPillText.Text = "Failed";
                StatusPillText.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#EF4444"));
                StatusBarText.Text = $"{_command} · Failed · {elapsed:F2}s";

                ItemsFoundNum.Visibility = Visibility.Collapsed;
                ItemsFoundLabel.Visibility = Visibility.Collapsed;

                ShowRaw(result);

                // Ask AI to explain the error
                ErrorExplainPanel.Visibility = Visibility.Visible;
                ErrorWhyText.Text = "⏳ Asking AI to explain this error...";
                ErrorFixText.Text = "";
                ErrorTipText.Text = "";

                try
                {
                    var explanation = await _aiService.ExplainErrorAsync(_command, result);
                    var explainLines = explanation.Split('\n');
                    ErrorWhyText.Text = explainLines.Length > 0 ? explainLines[0] : "";
                    ErrorFixText.Text = explainLines.Length > 1 ? explainLines[1] : "";
                    ErrorTipText.Text = explainLines.Length > 2 ? explainLines[2] : "";
                }
                catch
                {
                    ErrorWhyText.Text = "WHY: Could not retrieve AI explanation.";
                }

                // Add failed command to history
                HistoryStore.Add(new HistoryEntry
                {
                    DateGroup = "Today · " + DateTime.Now.ToString("MMM dd, yyyy"),
                    Time = DateTime.Now.ToString("hh:mm tt"),
                    Query = _query,
                    Command = _command,
                    Risk = _risk,
                    Status = "✗ Failed",
                    Duration = $"{elapsed:F2}s",
                    ResultSummary = "Access Denied or Error"
                });
                return;
            }

            // Try structured parse
            _allRows = ParseOutputToRows(result);

            if (_allRows.Count > 0)
            {
                // Enable export buttons only when we have data
                ExportCsvBtn.IsEnabled = true;
                ExportJsonBtn.IsEnabled = true;
                ExportCsvBtn.ToolTip = null;  // clear tooltip when enabled
                ExportJsonBtn.ToolTip = null;
                ExportCsvToolTip.Visibility = Visibility.Collapsed;
                ExportJsonToolTip.Visibility = Visibility.Collapsed;

                foreach (var row in _allRows)
                    _displayed.Add(row);

                GridBorder.Visibility = Visibility.Visible;
                TableBtn.IsEnabled = true;
                TableBtn.Opacity = 1.0;

                RawBorder.Visibility = Visibility.Collapsed;
                RawOutputBox.Text = _rawOutput;
                ItemsFoundNum.Visibility = Visibility.Visible;
                ItemsFoundLabel.Visibility = Visibility.Visible;

                BannerIcon.Text = "✅";
                BannerStatusText.Text = $"Command Executed Successfully · {_allRows.Count} items found";
                ItemsFoundNum.Text = _allRows.Count.ToString();
                ItemsFoundLabel.Text = "items found";
                StatusPillText.Text = "Success";
                PageInfo.Text = $"Showing 1–{_displayed.Count} of {_displayed.Count} items";
                StatusBarText.Text = $"{_command} · {_allRows.Count} results · {elapsed:F2}s";
            }
            else
            {
                // Raw output
                ShowRaw(result);
                TableBtn.IsEnabled = false;
                TableBtn.Opacity = 0.4;
                TableBtn.ToolTip = "Table view not available for this command's output";

                ExportCsvBtn.IsEnabled = false;
                ExportJsonBtn.IsEnabled = false;
                ExportCsvBtn.ToolTip = "No data to export";
                ExportJsonBtn.ToolTip = "No data to export";
                ExportCsvToolTip.Visibility = Visibility.Visible;
                ExportJsonToolTip.Visibility = Visibility.Visible;

                BannerIcon.Text = "✅";
                BannerStatusText.Text = $"Command Executed Successfully · {lines.Length} lines of output";

                ItemsFoundNum.Visibility = Visibility.Collapsed;
                ItemsFoundLabel.Visibility = Visibility.Collapsed;

                StatusPillText.Text = "Success";
                StatusBarText.Text = $"{_command} · {lines.Length} lines · {elapsed:F2}s";
                PageInfo.Text = $"Raw output · {lines.Length} lines";
            }

            // Add to history
            HistoryStore.Add(new HistoryEntry
            {
                DateGroup = "Today · " + DateTime.Now.ToString("MMM dd, yyyy"),
                Time = DateTime.Now.ToString("hh:mm tt"),
                Query = _query,
                Command = _command,
                Risk = _risk,
                Status = "✓ Success",
                Duration = $"{elapsed:F2}s",
                ResultSummary = _allRows.Count > 0
                    ? $"{_allRows.Count} items found"
                    : $"{lines.Length} lines"
            });
        }

        // Shows the raw text output and highlights the Raw button
        private void ShowRaw(string text)
        {
            RawOutputBox.Text = text;
            RawBorder.Visibility = Visibility.Visible;
            GridBorder.Visibility = Visibility.Collapsed;

            TableBtn.Style = (Style)FindResource("GhostBtn");
            RawBtn.Style = (Style)FindResource("PrimaryBtn");
        }

        // Tries 3 parsing strategies in order: file paths, format-table, key-value
        private static List<FileRow> ParseOutputToRows(string raw)
        {
            var rows = new List<FileRow>();
            var lines = raw.Split(new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

            // ── Method 1: File path detection ────────────────────────────────
            var fileRows = TryParseFilePaths(lines);
            if (fileRows.Count > 0) return fileRows;

            // ── Method 2: PowerShell Format-Table detection ──────────────────
            var tableRows = TryParseFormatTable(lines);
            if (tableRows.Count > 0) return tableRows;

            // ── Method 3: Key-Value pairs (e.g. Get-PSDrive output) ──────────
            var kvRows = TryParseKeyValue(lines);
            if (kvRows.Count > 0) return kvRows;

            return rows;
        }

        // Checks each line to see if it's a valid file path and reads its metadata
        private static List<FileRow> TryParseFilePaths(string[] lines)
        {
            var rows = new List<FileRow>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                try
                {
                    if (trimmed.Contains(":\\") || trimmed.Contains(":/"))
                    {
                        var info = new System.IO.FileInfo(trimmed);
                        if (info.Exists)
                        {
                            rows.Add(new FileRow
                            {
                                Icon = GetIcon(info.Extension),
                                Name = info.Name,
                                Size = FormatSize(info.Length),
                                Type = info.Extension.TrimStart('.').ToUpper(),
                                Modified = info.LastWriteTime.ToString("yyyy-MM-dd"),
                                Path = info.DirectoryName ?? ""
                            });
                        }
                    }
                }
                catch { }
            }
            return rows;
        }

        // Parses PowerShell Format-Table output using column header positions
        private static List<FileRow> TryParseFormatTable(string[] lines)
        {
            var rows = new List<FileRow>();

            int headerIdx = -1;
            int separatorIdx = -1;

            // Find the separator line (all dashes) to locate column positions
            for (int i = 0; i < lines.Length - 1; i++)
            {
                var next = lines[i + 1].Trim();
                if (next.Length > 0 && next.Replace("-", "").Replace(" ", "").Length == 0)
                {
                    headerIdx = i;
                    separatorIdx = i + 1;
                    break;
                }
            }

            if (headerIdx < 0 || separatorIdx < 0) return rows;

            // Parse column positions from separator line
            var separator = lines[separatorIdx];
            var columns = new List<(string Name, int Start, int End)>();
            var header = lines[headerIdx];

            int pos = 0;
            while (pos < separator.Length)
            {
                while (pos < separator.Length && separator[pos] == ' ') pos++;
                if (pos >= separator.Length) break;

                int start = pos;
                while (pos < separator.Length && separator[pos] == '-') pos++;
                int end = pos;

                var colName = start < header.Length
                    ? header.Substring(start, Math.Min(end - start, header.Length - start)).Trim()
                    : $"Col{columns.Count + 1}";

                columns.Add((colName, start, end));
            }

            if (columns.Count == 0) return rows;

            for (int i = separatorIdx + 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = new List<string>();
                for (int c = 0; c < columns.Count; c++)
                {
                    var (name, start, end) = columns[c];
                    if (start >= line.Length)
                    {
                        values.Add("");
                        continue;
                    }

                    if (c == columns.Count - 1)
                    {
                        values.Add(line.Substring(start).Trim());
                    }
                    else
                    {
                        int nextStart = columns[c + 1].Start;
                        int len = Math.Min(nextStart - start, line.Length - start);
                        values.Add(len > 0 ? line.Substring(start, len).Trim() : "");
                    }
                }

                var row = new FileRow
                {
                    Icon = "📋",
                    Name = values.Count > 0 ? values[0] : "",
                    Size = values.Count > 1 ? values[1] : "",
                    Type = values.Count > 2 ? values[2] : "",
                    Modified = values.Count > 3 ? values[3] : "",
                    Path = values.Count > 4 ? string.Join(" | ", values.Skip(4)) : ""
                };

                if (!string.IsNullOrWhiteSpace(row.Name))
                    rows.Add(row);
            }
            return rows;
        }

        // Parses key : value or key = value output (e.g. Get-PSDrive)
        private static List<FileRow> TryParseKeyValue(string[] lines)
        {
            var rows = new List<FileRow>();
            int count = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                int colonIdx = trimmed.IndexOf(" : ");
                int equalIdx = trimmed.IndexOf(" = ");
                int sepIdx = colonIdx >= 0 ? colonIdx :
                               equalIdx >= 0 ? equalIdx : -1;

                if (sepIdx > 0)
                {
                    count++;
                    var key = trimmed.Substring(0, sepIdx).Trim();
                    var value = trimmed.Substring(sepIdx + 3).Trim();

                    rows.Add(new FileRow
                    {
                        Icon = "🔑",
                        Name = key,
                        Size = value,
                        Type = "Property",
                        Modified = "",
                        Path = ""
                    });
                }
            }

            // Only use key-value parse if we found multiple pairs
            return count >= 2 ? rows : new List<FileRow>();
        }

        // Returns an emoji icon based on file extension
        private static string GetIcon(string ext)
        {
            switch (ext.ToLower())
            {
                case ".pdf": return "📄";
                case ".docx": case ".doc": return "📝";
                case ".xlsx": case ".xls": return "📊";
                case ".pptx": case ".ppt": return "📑";
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif": return "🖼";
                case ".txt": return "📃";
                case ".zip": case ".rar": return "🗜";
                default: return "📄";
            }
        }

        // Converts bytes to a readable size string
        private static string FormatSize(long bytes)
        {
            if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1024) return $"{bytes / 1024.0:F0} KB";
            return $"{bytes} B";
        }
        private void TableBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_allRows.Count == 0)
            {
                GridBorder.Visibility = Visibility.Collapsed;
                RawBorder.Visibility = Visibility.Visible;
                return;
            }

            GridBorder.Visibility = Visibility.Visible;
            RawBorder.Visibility = Visibility.Collapsed;
            RawOutputBox.Text = _rawOutput;

            // Highlight Table button
            TableBtn.Style = (Style)FindResource("PrimaryBtn");
            RawBtn.Style = (Style)FindResource("GhostBtn");
        }

        private void RawBtn_Click(object sender, RoutedEventArgs e)
        {
            GridBorder.Visibility = Visibility.Collapsed;
            RawBorder.Visibility = Visibility.Visible;

            // Highlight Raw button
            RawBtn.Style = (Style)FindResource("PrimaryBtn");
            TableBtn.Style = (Style)FindResource("GhostBtn");
        }
        // Filters the displayed rows as the user types in the filter box
        private void FilterBox_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            if (FindName("FilterBox") is System.Windows.Controls.TextBox filterBox)
            {
                var q = filterBox.Text.ToLower();
                _displayed.Clear();
                foreach (var r in _allRows.Where(r =>
                    r.Name.ToLower().Contains(q) ||
                    r.Type.ToLower().Contains(q) ||
                    r.Path.ToLower().Contains(q)))
                    _displayed.Add(r);
                PageInfo.Text = $"Showing 1–{_displayed.Count} of {_displayed.Count} items";
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        private void ExportCSV_Click(object sender, RoutedEventArgs e)
        {
            if (_allRows.Count == 0)
            {
                MessageBox.Show("No table data to export. Run a command that returns structured results.",
                    "Nothing to Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            const int MaxExportRows = 500;
            bool truncated = _displayed.Count > MaxExportRows;

            if (truncated)
            {
                var confirm = MessageBox.Show(
                    $"You have {_displayed.Count} items. " +
                    $"Export is limited to the first {MaxExportRows} rows.\n\nContinue?",
                    "Export Limited to 500 Rows",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;
            }

            var rowsToExport = _displayed.Take(MaxExportRows).ToList();

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "opticcli_export.csv"
            };
            if (dlg.ShowDialog() != true) return;

            var sb = new StringBuilder();
            sb.AppendLine("Name,Size,Type,Modified,Path");
            foreach (var r in rowsToExport)
            {
                var safeName = r.Name.Replace("\"", "\"\"");
                var safePath = r.Path.Replace("\"", "\"\"");
                sb.AppendLine($"\"{safeName}\",\"{r.Size}\",\"{r.Type}\",\"{r.Modified}\",\"{safePath}\"");
            }

            File.WriteAllText(dlg.FileName, sb.ToString());
            MessageBox.Show(
                truncated
                    ? $"Exported first {MaxExportRows} of {_displayed.Count} items successfully."
                    : $"Exported {rowsToExport.Count} items successfully.",
                "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ExportJSON_Click(object sender, RoutedEventArgs e)
        {
            if (_allRows.Count == 0)
            {
                MessageBox.Show("No table data to export. Run a command that returns structured results.",
                    "Nothing to Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            const int MaxExportRows = 500;
            bool truncated = _displayed.Count > MaxExportRows;

            if (truncated)
            {
                var confirm = MessageBox.Show(
                    $"You have {_displayed.Count} items. " +
                    $"Export is limited to the first {MaxExportRows} rows.\n\nContinue?",
                    "Export Limited to 500 Rows",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;
            }

            var rowsToExport = _displayed.Take(MaxExportRows).ToList();

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = "opticcli_export.json"
            };
            if (dlg.ShowDialog() != true) return;

            var sb = new StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < rowsToExport.Count; i++)
            {
                var r = rowsToExport[i];
                var safeName = r.Name.Replace("\"", "\\\"");
                var safePath = r.Path.Replace("\\", "\\\\").Replace("\"", "\\\"");
                var comma = i < rowsToExport.Count - 1 ? "," : "";
                sb.AppendLine(
                    $"  {{\"name\":\"{safeName}\",\"size\":\"{r.Size}\"," +
                    $"\"type\":\"{r.Type}\",\"modified\":\"{r.Modified}\"," +
                    $"\"path\":\"{safePath}\"}}{comma}");
            }
            sb.Append("]");

            File.WriteAllText(dlg.FileName, sb.ToString());
            MessageBox.Show(
                truncated
                    ? $"Exported first {MaxExportRows} of {_displayed.Count} items successfully."
                    : $"Exported {rowsToExport.Count} items successfully.",
                "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            new CustomizeView(null, _query).Show();
            Close();
        }

        private void HistoryBtn_Click(object sender, RoutedEventArgs e)
        {
            new HistoryView().Show();
            Close();
        }
        private void NewSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            new HomeView().Show();
            this.Close();
        }
    }
}