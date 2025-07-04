using Avalonia.Controls;

namespace JeekEasytierManager;

public partial class MainWindowInfo : UserControl
{
    public MainWindowInfo()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;
    }
}
