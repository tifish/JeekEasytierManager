using Avalonia.Controls;

namespace JeekEasytierManager;

public partial class MainWindowConfigs : UserControl
{
    public MainWindowConfigs()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;
    }
}
