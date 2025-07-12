using Avalonia.Controls;

namespace JeekEasyTierManager;

public partial class MainWindowConfigs : UserControl
{
    public MainWindowConfigs()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;
    }
}
