using Avalonia.Controls;

namespace JeekEasyTierManager;

public partial class MainWindowGrid : UserControl
{
    public MainWindowGrid()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;

        MainViewModel.Instance.MainGrid = MainGrid;
    }
}