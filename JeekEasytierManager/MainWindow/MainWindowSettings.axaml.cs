using Avalonia.Controls;

namespace JeekEasytierManager;

public partial class MainWindowSettings : UserControl
{
    public MainWindowSettings()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;
    }
}
