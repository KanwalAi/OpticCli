// ============================================================
// File: RiskConverters.cs
// Project: OpticCli
// Namespace: OpticCli.Converters
// Description: Contains all XAML value converters for the UI.
//              Converts RiskLevel values into colors, borders, and
//              visibility states used across the application views.
// ============================================================
using OpticCli.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace OpticCli.Converters
{
    // Maps RiskLevel → foreground colour brush
    public class RiskToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) => value switch
        {
            RiskLevel.Safe => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")),
            RiskLevel.Medium => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
            RiskLevel.High => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
            _ => new SolidColorBrush(Colors.White)
        };
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
    }

    // Maps RiskLevel → background colour brush
    public class RiskToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) => value switch
        {
            RiskLevel.Safe => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A22C55E")),
            RiskLevel.Medium => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1AF59E0B")),
            RiskLevel.High => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1AEF4444")),
            _ => new SolidColorBrush(Colors.Transparent)
        };
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
    }

    // Maps RiskLevel → border colour brush
    public class RiskToBorderConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) => value switch
        {
            RiskLevel.Safe => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4D22C55E")),
            RiskLevel.Medium => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4DF59E0B")),
            RiskLevel.High => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4DEF4444")),
            _ => new SolidColorBrush(Colors.Transparent)
        };
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
    }

    // Maps RiskLevel → left-accent strip colour
    public class RiskToAccentConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c) => value switch
        {
            RiskLevel.Safe => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")),
            RiskLevel.Medium => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
            RiskLevel.High => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
            _ => new SolidColorBrush(Colors.Transparent)
        };
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
    }

    // Bool → Visibility
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => v is Visibility vis && vis == Visibility.Visible;
    }

    // Status string → colour brush
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            var s = value?.ToString() ?? "";
            return s.Contains("✓") || s.ToLower().Contains("success")
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
    }

    // Risk string → colour brush (for history view)
    public class RiskStringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            var s = value?.ToString()?.ToLower() ?? "";
            return s switch
            {
                "safe" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A22C55E")),
                "medium" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1AF59E0B")),
                "high" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1AEF4444")),
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
    }
}
