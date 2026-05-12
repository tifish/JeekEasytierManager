using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace JeekEasyTierManager;

public partial class MainWindowGrid : UserControl
{
    public MainWindowGrid()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;

        MainViewModel.Instance.MainGrid = MainGrid;

        MainViewModel.Instance.PropertyChanged += MainViewModel_PropertyChanged;
        DetachedFromVisualTree += (_, _) =>
            MainViewModel.Instance.PropertyChanged -= MainViewModel_PropertyChanged;
    }

    private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Messages))
            Dispatcher.UIThread.Post(ScrollLogToEnd);
    }

    private void ScrollLogToEnd()
    {
        LogScrollViewer.Offset = new Vector(
            LogScrollViewer.Offset.X,
            LogScrollViewer.Extent.Height
        );
    }
}
