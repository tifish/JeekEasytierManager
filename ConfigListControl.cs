using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Windows.Input;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Data;

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

public class ConfigListControl : UserControl
{
    private readonly Grid _grid;

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
    }

    [Content]
    public Controls Children => _grid.Children;

    // 处理属性变化
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
                Margin = new Thickness(5, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
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