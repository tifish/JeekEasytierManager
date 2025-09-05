using System.Linq;
using Avalonia.Controls;

namespace JeekEasyTierManager;

public partial class MainWindowConfigs : UserControl
{
    private bool _isUpdatingSelection = false;

    public MainWindowConfigs()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;
        MainViewModel.Instance.SetMainWindowConfigs(this);
    }

    public void DataGrid_SelectionChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isUpdatingSelection)
            return;

        var dataGrid = (DataGrid)sender!;
        var selectedItems = dataGrid.SelectedItems;
        MainViewModel.Instance.HasSelectedConfigs = selectedItems.Count > 0;
        MainViewModel.Instance.SelectedConfigs = [.. selectedItems.Cast<ConfigInfo>()];
    }

    public void UpdateDataGridSelection()
    {
        if (ConfigsDataGrid != null)
        {
            _isUpdatingSelection = true;
            try
            {
                ConfigsDataGrid.SelectedItems.Clear();
                foreach (var config in MainViewModel.Instance.SelectedConfigs)
                {
                    ConfigsDataGrid.SelectedItems.Add(config);
                }
            }
            finally
            {
                _isUpdatingSelection = false;
            }
        }
    }
}
