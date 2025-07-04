using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    public static MainViewModel Instance { get; private set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<ConfigInfo> Configs { get; set; } = [];

    public async Task Init()
    {
        await AppSettings.Load();
        LoadConfigs();
        await LoadEnabledServices();
        await UpdateServiceStatus();
        CheckHasEasytier();
        await ShowPeers();

        _autoUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromHours(1)
        };
        _autoUpdateTimer.Tick += OnAutoUpdateMeTimerElapsed;

        AutoUpdateMe = Settings.AutoUpdateMe;
        AutoUpdateEasytier = Settings.AutoUpdateEasytier;
    }

    public void Dispose()
    {
        _autoUpdateTimer?.Stop();

        GC.SuppressFinalize(this);
    }

    private MainWindow? _mainWindow;

    public void SetMainWindow(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }
}
