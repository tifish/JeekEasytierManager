using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Windows.Input;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace JeekEasytierManager;

public partial class ConfigInfo : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial bool Enabled { get; set; } = false;

    [ObservableProperty]
    public partial ServiceStatus Status { get; set; } = ServiceStatus.None;

}

// Add status to color converter
public class ServiceStatusToColorConverter : IValueConverter
{
    // Cache color brushes for performance
    private static readonly Lazy<SolidColorBrush> _runningBrush = new(() => GetResourceColor("Green"));
    private static readonly Lazy<SolidColorBrush> _stoppedBrush = new(() => GetResourceColor("Red"));
    private static readonly Lazy<SolidColorBrush> _noneBrush = new(() => GetResourceColor("Gray"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Running => _runningBrush.Value,
                ServiceStatus.Stopped => _stoppedBrush.Value,
                ServiceStatus.None => _noneBrush.Value,
                _ => _noneBrush.Value
            };
        }
        return _noneBrush.Value;
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

public class ConfigListControl : UserControl
{
    private readonly Grid _grid;
    private readonly ServiceStatusToColorConverter _statusColorConverter;

    public ConfigListControl()
    {
        _grid = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
            ],
        };
        Content = _grid;
        _statusColorConverter = new ServiceStatusToColorConverter();
    }

    [Content]
    public Controls Children => _grid.Children;

    // Handle property changes
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ConfigsProperty)
        {
            OnConfigsChanged(change.OldValue as IEnumerable<ConfigInfo>, change.NewValue as IEnumerable<ConfigInfo>);
        }
        else if (change.Property == EditCommandProperty)
        {
            OnEditCommandChanged(change.OldValue as ICommand, change.NewValue as ICommand);
        }
    }

    public static readonly StyledProperty<IEnumerable<ConfigInfo>?> ConfigsProperty =
        AvaloniaProperty.Register<ConfigListControl, IEnumerable<ConfigInfo>?>(nameof(Configs));

    public IEnumerable<ConfigInfo>? Configs
    {
        get => GetValue(ConfigsProperty);
        set => SetValue(ConfigsProperty, value);
    }

    private void OnConfigsChanged(IEnumerable<ConfigInfo>? oldItems, IEnumerable<ConfigInfo>? newItems)
    {
        if (oldItems is INotifyCollectionChanged oldNotifyCollection)
        {
            oldNotifyCollection.CollectionChanged -= OnConfigsCollectionChanged;
        }

        if (newItems is INotifyCollectionChanged newNotifyCollection)
        {
            newNotifyCollection.CollectionChanged += OnConfigsCollectionChanged;
        }

        RebuildItems();
    }

    private void OnConfigsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildItems();
    }

    public static readonly StyledProperty<ICommand?> EditCommandProperty =
        AvaloniaProperty.Register<ConfigListControl, ICommand?>(nameof(EditCommand));

    public ICommand? EditCommand
    {
        get => GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    private void OnEditCommandChanged(ICommand? oldValue, ICommand? newValue)
    {
        UpdateEditCommand();
    }

    private void UpdateEditCommand()
    {
        foreach (var button in _grid.Children.OfType<Button>())
        {
            button.Command = EditCommand;
        }
    }

    private void RebuildItems()
    {
        _grid.Children.Clear();

        if (Configs == null)
            return;

        int row = 0;
        foreach (var config in Configs)
        {
            if (_grid.RowDefinitions.Count <= row)
            {
                _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            // Add row background
            var rowBackground = new Border
            {
                Background = row % 2 == 0 ?
                    new SolidColorBrush(Color.FromArgb(50, 180, 180, 180)) : // More obvious light gray background
                    new SolidColorBrush(Colors.Transparent), // Transparent background
                Margin = new Thickness(0, 1)
            };
            _grid.Children.Add(rowBackground);
            Grid.SetRow(rowBackground, row);
            Grid.SetColumnSpan(rowBackground, 3); // Span all columns

            var checkBox = new CheckBox
            {
                [!CheckBox.ContentProperty] = new Binding("Name") { Source = config },
                [!CheckBox.IsCheckedProperty] = new Binding("Enabled") { Source = config },
                Margin = new Thickness(5, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            _grid.Children.Add(checkBox);
            Grid.SetRow(checkBox, row);
            Grid.SetColumn(checkBox, 0);

            var serviceStatusText = new TextBlock
            {
                [!TextBlock.TextProperty] = new Binding("Status") { Source = config },
                [!TextBlock.ForegroundProperty] = new Binding("Status")
                {
                    Source = config,
                    Converter = _statusColorConverter
                },
                Margin = new Thickness(5, 0),
                FontWeight = FontWeight.Bold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            };
            _grid.Children.Add(serviceStatusText);
            Grid.SetRow(serviceStatusText, row);
            Grid.SetColumn(serviceStatusText, 1);

            var editButton = new Button
            {
                Content = "Edit",
                Command = EditCommand,
                CommandParameter = config.Name,
                Margin = new Thickness(5, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            _grid.Children.Add(editButton);
            Grid.SetRow(editButton, row);
            Grid.SetColumn(editButton, 2);

            row++;
        }
    }

}