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
    private static TrayIcons? _trayIcons;
    private static TrayIcon? _trayIcon;

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
            ToolTipText = "Jeek Easytier 管理器",
            IsVisible = true
        };

        // Create right-click menu
        var menu = new NativeMenu();

        // Show/Hide main window
        var showHideMenuItem = new NativeMenuItem("显示/隐藏 Jeek Easytier 管理器");
        showHideMenuItem.Click += (sender, e) => ToggleMainWindow();
        menu.Add(showHideMenuItem);

        // Separator
        menu.Add(new NativeMenuItemSeparator());

        // Start service
        menu.Add(new NativeMenuItem("重启服务")
        {
            Command = MainViewModel.Instance.RestartSelectedServicesCommand
        });

        // Stop service
        menu.Add(new NativeMenuItem("停止服务")
        {
            Command = MainViewModel.Instance.StopSelectedServicesCommand
        });

        // Separator
        menu.Add(new NativeMenuItemSeparator());

        // Exit application
        var exitMenuItem = new NativeMenuItem("退出");
        exitMenuItem.Click += (sender, e) => ExitApplication();
        menu.Add(exitMenuItem);

        _trayIcon.Menu = menu;

        _trayIcon.Clicked += (sender, e) =>
        {
            ToggleMainWindow();
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
        // Hide tray icon, or the icon will be more and more.
        _trayIcon?.IsVisible = false;

        // Clean up resources
        MainViewModel.Instance.Dispose();

        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}