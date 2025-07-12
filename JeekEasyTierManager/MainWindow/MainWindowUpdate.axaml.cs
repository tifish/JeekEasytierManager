using Avalonia.Controls;

namespace JeekEasyTierManager;

public partial class MainWindowUpdate : UserControl
{
    public MainWindowUpdate()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;
    }
}
