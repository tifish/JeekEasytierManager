using Avalonia.Controls;

namespace JeekEasyTierManager;

public partial class MainWindowInfo : UserControl
{
    public MainWindowInfo()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;
    }
}
