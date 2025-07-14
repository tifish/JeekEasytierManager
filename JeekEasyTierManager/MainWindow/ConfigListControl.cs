using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia;
using System.Collections.Specialized;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Data.Converters;
using System;
using System.Globalization;
using Avalonia.Layout;
using JeekTools;
using System.IO;
using System.Runtime.Serialization;
using Nett;
using System.ServiceProcess;

namespace JeekEasyTierManager;

public class EasyTierConfig
{
    [DataMember(Name = "flags")]
    public EasyTierConfigFlags? Flags { get; set; } = new();
}

public class EasyTierConfigFlags
{
    [DataMember(Name = "dev_name")]
    public string? DevName { get; set; } = "";

    [DataMember(Name = "no_tun")]
    public bool NoTun { get; set; } = false;
}

public partial class ConfigInfo : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = false;

    [ObservableProperty]
    public partial ServiceStatus Status { get; set; } = ServiceStatus.None;

    public bool IsInstalled { get; set; } = false;

    public string GetConfigPath()
    {
        return Path.Join(AppSettings.ConfigDirectory, Name + ".toml");
    }

    public EasyTierConfig? GetConfig()
    {
        try
        {
            return Toml.ReadFile<EasyTierConfig>(GetConfigPath());
        }
        catch
        {
            return null;
        }
    }

    public ServiceController? Service { get; set; }
}

// Add status to color converter
public class ServiceStatusToColorConverter : IValueConverter
{
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
            };
            _grid.Children.Add(rowBackground);
            Grid.SetRow(rowBackground, row);
            Grid.SetColumnSpan(rowBackground, 3); // Span all columns

            var checkBox = new CheckBox
            {
                [!CheckBox.ContentProperty] = new Binding(nameof(ConfigInfo.Name)) { Source = config },
                [!CheckBox.IsCheckedProperty] = new Binding(nameof(ConfigInfo.IsSelected)) { Source = config },
                Margin = new Thickness(5, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            _grid.Children.Add(checkBox);
            Grid.SetRow(checkBox, row);
            Grid.SetColumn(checkBox, 0);

            var serviceStatusText = new TextBlock
            {
                [!TextBlock.TextProperty] = new Binding(nameof(ConfigInfo.Status)) { Source = config },
                [!TextBlock.ForegroundProperty] = new Binding(nameof(ConfigInfo.Status))
                {
                    Source = config,
                    Converter = _statusColorConverter
                },
                Margin = new Thickness(10, 0, 0, 0),
                FontWeight = FontWeight.Bold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            };
            _grid.Children.Add(serviceStatusText);
            Grid.SetRow(serviceStatusText, row);
            Grid.SetColumn(serviceStatusText, 1);

            // Create StackPanel to hold buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };

            var button = new Button
            {
                Content = "➕",
                Command = MainViewModel.Instance.InstallSingleServiceCommand,
                CommandParameter = config,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                [!Button.IsVisibleProperty] = new Binding(nameof(MainViewModel.Instance.ShowMoreConfigActions)) { Source = MainViewModel.Instance },
            };
            buttonPanel.Children.Add(button);
            ToolTip.SetTip(button, "安装服务");

            button = new Button
            {
                Content = "➖",
                Command = MainViewModel.Instance.UninstallSingleServiceCommand,
                CommandParameter = config,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                [!Button.IsVisibleProperty] = new Binding(nameof(MainViewModel.Instance.ShowMoreConfigActions)) { Source = MainViewModel.Instance },
            };
            buttonPanel.Children.Add(button);
            ToolTip.SetTip(button, "卸载服务");

            button = new Button
            {
                Content = "▶️",
                Command = MainViewModel.Instance.RestartSingleServiceCommand,
                CommandParameter = config,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            buttonPanel.Children.Add(button);
            ToolTip.SetTip(button, "重启服务");

            button = new Button
            {
                Content = "⏹️",
                Command = MainViewModel.Instance.StopSingleServiceCommand,
                CommandParameter = config,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            buttonPanel.Children.Add(button);
            ToolTip.SetTip(button, "停止服务");

            button = new Button
            {
                Content = "🧪",
                Command = MainViewModel.Instance.TestSingleConfigCommand,
                CommandParameter = config,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            buttonPanel.Children.Add(button);
            ToolTip.SetTip(button, "测试配置");

            button = new Button
            {
                Content = "✏️",
                Command = MainViewModel.Instance.EditSingleConfigCommand,
                CommandParameter = config,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            buttonPanel.Children.Add(button);
            ToolTip.SetTip(button, "编辑配置");

            button = new Button
            {
                Content = "📝",
                Command = MainViewModel.Instance.EditConfigFileCommand,
                CommandParameter = config,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                [!Button.IsVisibleProperty] = new Binding(nameof(MainViewModel.Instance.ShowMoreConfigActions)) { Source = MainViewModel.Instance },
            };
            buttonPanel.Children.Add(button);
            ToolTip.SetTip(button, "编辑配置文件");

            button = new Button
            {
                Content = "⛏️",
                Command = MainViewModel.Instance.RenameConfigCommand,
                CommandParameter = config,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                [!Button.IsVisibleProperty] = new Binding(nameof(MainViewModel.Instance.ShowMoreConfigActions)) { Source = MainViewModel.Instance },
            };
            buttonPanel.Children.Add(button);
            ToolTip.SetTip(button, "重命名");

            button = new Button
            {
                Content = "🗑️",
                Command = MainViewModel.Instance.DeleteConfigCommand,
                CommandParameter = config,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                [!Button.IsVisibleProperty] = new Binding(nameof(MainViewModel.Instance.ShowMoreConfigActions)) { Source = MainViewModel.Instance },
            };
            buttonPanel.Children.Add(button);
            ToolTip.SetTip(button, "删除");

            _grid.Children.Add(buttonPanel);
            Grid.SetRow(buttonPanel, row);
            Grid.SetColumn(buttonPanel, 2);

            row++;
        }
    }

}