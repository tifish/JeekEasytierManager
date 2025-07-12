using Avalonia.Controls;

namespace JeekEasyTierManager;

public partial class MainWindowSettings : UserControl
{
    public MainWindowSettings()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;
    }
}
