using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using JeekTools;

namespace JeekEasyTierManager;

// Add status to color converter
public class ServiceStatusToColorConverter : IValueConverter
{
    public static ServiceStatusToColorConverter Instance { get; } = new();

    // Cache color brushes for performance
    private static readonly Lazy<SolidColorBrush> _greenBrush = new(() => GetResourceColor("Green"));
    private static readonly Lazy<SolidColorBrush> _redBrush = new(() => GetResourceColor("Red"));
    private static readonly Lazy<SolidColorBrush> _yellowBrush = new(() => GetResourceColor("Yellow"));
    private static readonly Lazy<SolidColorBrush> _grayBrush = new(() => GetResourceColor("Gray"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Running => _greenBrush.Value,
                ServiceStatus.Stopped => _yellowBrush.Value,
                ServiceStatus.Paused => _redBrush.Value,
                ServiceStatus.None => _grayBrush.Value,
                _ => _grayBrush.Value
            };
        }
        return _grayBrush.Value;
    }

    private static SolidColorBrush GetResourceColor(string resourceKey)
    {
        if (Application.Current?.Resources.TryGetResource(resourceKey, null, out var resource) == true
            && resource is Color color)
        {
            return new SolidColorBrush(color);
        }
        // Return default color if resource cannot be obtained
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

