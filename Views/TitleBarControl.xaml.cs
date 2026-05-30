// ============================================================
// File: TitleBarControl.xaml.cs
// Project: OpticCli
// Namespace: OpticCli.Views
// Description: Code-behind for the custom title bar. Handles
//              window close, minimize, and maximize/restore.
// ============================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpticCli.Views
{
    public partial class TitleBarControl : UserControl
    {
        public TitleBarControl() => InitializeComponent();

        private void CloseBtn_Click(object sender, MouseButtonEventArgs e)
        {
            // Walk up the visual tree to find and close the parent Window
            Window.GetWindow(this)?.Close();
        }
        private void MinimizeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            Window.GetWindow(this).WindowState = WindowState.Minimized;
        }

        // Toggle between maximized and normal window state
        private void MaximizeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            var win = Window.GetWindow(this);
            win.WindowState = win.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }
}