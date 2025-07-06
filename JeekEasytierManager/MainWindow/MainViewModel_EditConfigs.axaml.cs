using Avalonia.Controls;

namespace JeekEasytierManager;

public partial class MainWindowEditConfigs : UserControl
{
    public MainWindowEditConfigs()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;
    }
}
