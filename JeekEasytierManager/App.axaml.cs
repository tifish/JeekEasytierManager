using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace JeekEasytierManager;

public partial class App : Application
{
    private static MainWindow? _mainWindow;
    private TrayIcons? _trayIcons;
    private TrayIcon? _trayIcon;

    public static MainWindow? MainWindow => _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainWindow = new MainWindow();
            desktop.MainWindow = _mainWindow;

            // Initialize tray icon
            InitializeTrayIcon();

            // Set shutdown mode to not exit when all windows are closed
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new TrayIcon()
        {
            Icon = new WindowIcon(new Bitmap(AssetLoader.Open(new Uri("avares://JeekEasytierManager/App.ico")))),
            ToolTipText = "Jeek Easytier Manager",
            IsVisible = true
        };

        // Create right-click menu
        var menu = new NativeMenu();

        // Show/Hide main window
        var showHideMenuItem = new NativeMenuItem("Show/Hide Jeek Easytier Manager");
        showHideMenuItem.Click += (sender, e) => ToggleMainWindow();
        menu.Add(showHideMenuItem);

        // Separator
        menu.Add(new NativeMenuItemSeparator());

        // Start service
        var startServiceMenuItem = new NativeMenuItem("(_Re)start Service");
        startServiceMenuItem.Command = MainViewModel.Instance.RestartServiceCommand;
        menu.Add(startServiceMenuItem);

        // Stop service
        var stopServiceMenuItem = new NativeMenuItem("_Stop Service");
        stopServiceMenuItem.Command = MainViewModel.Instance.StopServiceCommand;
        menu.Add(stopServiceMenuItem);

        // Separator
        menu.Add(new NativeMenuItemSeparator());

        // Exit application
        var exitMenuItem = new NativeMenuItem("E_xit");
        exitMenuItem.Click += (sender, e) => ExitApplication();
        menu.Add(exitMenuItem);

        _trayIcon.Menu = menu;

        _trayIcon.Clicked += (sender, e) =>
        {
            if (_mainWindow != null)
            {
                if (_mainWindow.IsVisible)
                {
                    _mainWindow.Hide();
                }
                else
                {
                    _mainWindow.Show();
                    _mainWindow.Activate();
                    _mainWindow.BringIntoView();
                }
            }
        };

        _trayIcons = [_trayIcon];
    }

    public static void ToggleMainWindow()
    {
        if (_mainWindow != null)
        {
            if (_mainWindow.IsVisible)
            {
                _mainWindow.Hide();
            }
            else
            {
                _mainWindow.Show();
                _mainWindow.Activate();
                _mainWindow.BringIntoView();
            }
        }
    }

    public static void ExitApplication()
    {
        // Clean up resources
        MainViewModel.Instance.Dispose();

        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}