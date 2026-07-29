using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Jeek.Avalonia.Localization;

namespace JeekEasyTierManager;

public partial class App : Application
{
    private static MainWindow? _mainWindow;
    private static TrayIcons? _trayIcons;
    private static TrayIcon? _trayIcon;
    private static NativeMenuItem? _showHideMenuItem;
    private static NativeMenuItem? _restartServiceMenuItem;
    private static NativeMenuItem? _stopServiceMenuItem;
    private static NativeMenuItem? _exitMenuItem;

    public static MainWindow? MainWindow => _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Localizer.SetLocalizer(new TabLocalizer());
            // Default to English; the saved language preference is applied after settings load.
            Localizer.Language = "en";

            _mainWindow = new MainWindow();
            desktop.MainWindow = _mainWindow;

            // Initialize tray icon
            InitializeTrayIcon();

            // Set shutdown mode to not exit when all windows are closed
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Debug MCP surface (no-op in Release builds).
            DebugMcpServer.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new TrayIcon()
        {
            Icon = new WindowIcon(
                new Bitmap(AssetLoader.Open(new Uri("avares://JeekEasyTierManager/App.ico")))
            ),
            IsVisible = true,
        };

        // Create right-click menu
        var menu = new NativeMenu();

        // Show/Hide main window
        _showHideMenuItem = new NativeMenuItem();
        _showHideMenuItem.Click += (sender, e) => ToggleMainWindow();
        menu.Add(_showHideMenuItem);

        // Separator
        menu.Add(new NativeMenuItemSeparator());

        // Restart service
        _restartServiceMenuItem = new NativeMenuItem
        {
            Command = MainViewModel.Instance.RestartSelectedServicesCommand,
        };
        menu.Add(_restartServiceMenuItem);

        // Stop service
        _stopServiceMenuItem = new NativeMenuItem
        {
            Command = MainViewModel.Instance.StopSelectedServicesCommand,
        };
        menu.Add(_stopServiceMenuItem);

        // Separator
        menu.Add(new NativeMenuItemSeparator());

        // Exit application
        _exitMenuItem = new NativeMenuItem();
        _exitMenuItem.Click += (sender, e) => ExitApplication();
        menu.Add(_exitMenuItem);

        _trayIcon.Menu = menu;

        _trayIcon.Clicked += (sender, e) =>
        {
            ToggleMainWindow();
        };

        _trayIcons = [_trayIcon];

        UpdateTrayTexts();
        Localizer.LanguageChanged += (sender, e) => UpdateTrayTexts();
    }

    private static void UpdateTrayTexts()
    {
        if (_trayIcon != null)
            _trayIcon.ToolTipText = Localizer.Get("Menu_AppTitle");
        if (_showHideMenuItem != null)
            _showHideMenuItem.Header = Localizer.Get("Menu_ShowHide");
        if (_restartServiceMenuItem != null)
            _restartServiceMenuItem.Header = Localizer.Get("Menu_RestartService");
        if (_stopServiceMenuItem != null)
            _stopServiceMenuItem.Header = Localizer.Get("Menu_StopService");
        if (_exitMenuItem != null)
            _exitMenuItem.Header = Localizer.Get("Menu_Exit");
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

        DebugMcpServer.Stop();

        // Clean up resources
        MainViewModel.Instance.Dispose();

        // Flush buffered logs before the process exits.
        JeekTools.LogManager.Shutdown();

        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
