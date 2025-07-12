using Avalonia.Controls;

namespace JeekEasyTierManager;

public partial class MainWindowEditConfigs : UserControl
{
    public MainWindowEditConfigs()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;
    }
}
