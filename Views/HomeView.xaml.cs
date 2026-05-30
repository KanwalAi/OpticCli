// ============================================================
// File: HomeView.xaml.cs
// Project: OpticCli
// Namespace: OpticCli.Views
// Description: Code-behind for the Home screen. Handles search
//              input validation, quick prompt chips, voice input,
//              and navigation to the Suggestions screen.
// ============================================================
using System;
using System.Speech.Recognition;
using System.Windows;
using System.Windows.Input;

namespace OpticCli.Views
{
    public partial class HomeView : Window
    {
        private SpeechRecognitionEngine _recognizer;
        private bool _isListening = false;
        // Starts or stops microphone listening for voice input
        private void MicBtn_Click(object sender, RoutedEventArgs e)
        {
            // If already listening, stop and reset
            if (_isListening)
            {
                _recognizer?.RecognizeAsyncStop();
                _isListening = false;
                NLInput.Text = NLInput.Text == "Listening..." ? "" : NLInput.Text;
                return;
            }

            try
            {
                _recognizer = new SpeechRecognitionEngine(
                 new System.Globalization.CultureInfo("en-US"));
                _recognizer.LoadGrammar(new DictationGrammar());
                _recognizer.BabbleTimeout = TimeSpan.FromSeconds(3);
                _recognizer.EndSilenceTimeout = TimeSpan.FromSeconds(1);
                _recognizer.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(1.5);
                _recognizer.SetInputToDefaultAudioDevice();

                _recognizer.SpeechRecognized += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Only accept result if confidence is high enough
                        if (args.Result.Confidence > 0.6f)
                        {
                            NLInput.Text = args.Result.Text;
                            NLInput.CaretIndex = NLInput.Text.Length;
                        }
                        else
                        {
                            NLInput.Text = "";
                            MessageBox.Show(
                                $"Speech not recognized clearly (confidence: {args.Result.Confidence:P0}).\nPlease try again and speak slowly.",
                                "Low Confidence", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        NLInput.Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                            .ConvertFromString("#C8D0E0"));
                        _isListening = false;

                        NLInput.Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                            .ConvertFromString("#C8D0E0"));
                        _isListening = false;
                    });
                };

                _recognizer.SpeechRecognized += (s, args) =>
                    _recognizer.RecognizeAsyncStop();

                NLInput.Text = "🎙 Listening... speak now";
                NLInput.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                    .ConvertFromString("#00E5FF"));
                _isListening = true;
                _recognizer.RecognizeAsync(RecognizeMode.Single);
            }
            catch
            {
                MessageBox.Show(
                    "Microphone not available or Windows Speech Recognition not enabled.",
                    "Mic Error");
            }
        }

        private const string PlaceholderText = "Find all large PDF files on this drive...";

        public HomeView() => InitializeComponent();

        // Opens with a pre-filled query (used when coming back from another screen)
        public HomeView(string prefillQuery) : this()
        {
            NLInput.Text = prefillQuery;
            NLInput.CaretIndex = NLInput.Text.Length;
        }

        /* ── Drag window ── */
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        /* ── Clear placeholder on focus ── */
        private void NLInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (NLInput.Text == PlaceholderText)
                NLInput.Text = string.Empty;
        }

        /* ── Enter key = Discover ── */
        private void NLInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) OpenSuggestions();
        }

        private void DiscoverBtn_Click(object sender, RoutedEventArgs e)
            => OpenSuggestions();

        // Fills the search box with the chip's text when clicked
        private void Chip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn)
            {
                var raw = btn.Content?.ToString() ?? "";
                NLInput.Text = raw.StartsWith("⟩ ") ? raw[2..] : raw;
                NLInput.Focus();
                NLInput.CaretIndex = NLInput.Text.Length;
            }
        }

        // Validates the input and navigates to the Suggestions screen
        private void OpenSuggestions()
        {
            var query = NLInput.Text.Trim();

            // Empty or placeholder
            if (string.IsNullOrEmpty(query) || query == PlaceholderText)
            {
                ShowValidation("⚠  Please enter a command description to search.", "#EF4444");
                NLInput.Focus();
                return;
            }

            // Too vague — 1 character
            if (query.Length == 1)
            {
                ShowValidation("⚠  Query too vague, please be more descriptive.", "#F59E0B");
                NLInput.Focus();
                return;
            }

            // Exceeds 300 character limit
            if (query.Length > 300)
            {
                ShowValidation("⚠  Query exceeds 300-character limit. Please shorten your input.", "#EF4444");
                NLInput.Focus();
                return;
            }

            // Valid — proceed
            ValidationMsg.Visibility = Visibility.Collapsed;
            var suggestions = new SuggestionsView(query);
            suggestions.Show();
            this.Close();
        }

        // Shows a colored validation message below the search box
        private void ShowValidation(string message, string colorHex)
        {
            ValidationMsg.Text = message;
            ValidationMsg.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter
                .ConvertFromString(colorHex));
            ValidationMsg.Visibility = Visibility.Visible;
        }
        private void HistoryBtn_Click(object sender, RoutedEventArgs e)
        {
            new HistoryView().Show();
            this.Close();
        }

        // Hide the validation message as soon as the user starts typing
        private void NLInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (ValidationMsg != null)
                ValidationMsg.Visibility = Visibility.Collapsed;
        }
    }
}
