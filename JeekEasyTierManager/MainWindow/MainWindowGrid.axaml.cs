using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

namespace JeekEasyTierManager;

public partial class MainWindowGrid : UserControl
{
    private Window? _hostWindow;
    private double _hostWindowWidth;
    private double _hostWindowHeight;
    private bool _topContentStretchesWithWidth;
    private bool _mainGridStretchesWithHeight;

    public MainWindowGrid()
    {
        InitializeComponent();

        DataContext = MainViewModel.Instance;

        MainViewModel.Instance.MainGrid = MainGrid;
        LogViewport.FallbackWidthSource = ConfigsPanel;

        MainViewModel.Instance.PropertyChanged += MainViewModel_PropertyChanged;
        DetachedFromVisualTree += (_, _) => DetachEvents();
    }

    public void EnableTopContentStretchOnWindowResize()
    {
        _hostWindow = TopLevel.GetTopLevel(this) as Window;
        if (_hostWindow is null)
            return;

        _hostWindowWidth = _hostWindow.Bounds.Width;
        _hostWindowHeight = _hostWindow.Bounds.Height;
        _hostWindow.SizeChanged -= HostWindow_SizeChanged;
        _hostWindow.SizeChanged += HostWindow_SizeChanged;
    }

    private void HostWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (!_topContentStretchesWithWidth && Math.Abs(e.NewSize.Width - _hostWindowWidth) >= 1)
        {
            TopContentGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            _topContentStretchesWithWidth = true;
        }

        if (!_mainGridStretchesWithHeight && Math.Abs(e.NewSize.Height - _hostWindowHeight) >= 1)
        {
            MainGrid
                .RowDefinitions[0]
                .SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
            MainGrid
                .RowDefinitions[1]
                .SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Auto));
            _mainGridStretchesWithHeight = true;
        }

        if (_topContentStretchesWithWidth && _mainGridStretchesWithHeight)
        {
            if (_hostWindow != null)
                _hostWindow.SizeChanged -= HostWindow_SizeChanged;

            _hostWindow = null;
        }
    }

    private void DetachEvents()
    {
        MainViewModel.Instance.PropertyChanged -= MainViewModel_PropertyChanged;
        if (_hostWindow != null)
            _hostWindow.SizeChanged -= HostWindow_SizeChanged;
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
