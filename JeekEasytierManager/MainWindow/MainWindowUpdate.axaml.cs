using Avalonia.Controls;

namespace JeekEasytierManager;

public partial class MainWindowUpdate : UserControl
{
    public MainWindowUpdate()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;
    }
}
