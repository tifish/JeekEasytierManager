using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using JeekTools;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    public static MainViewModel Instance { get; private set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<ConfigInfo> Configs { get; set; } = [];

    public async Task Init()
    {
        await AppSettings.Load();
        await LoadConfigs(true);
        CheckHasEasytier();
        await ShowPeers();
        await ApplySettings();

        GitHubMirrors.TestUrl = AppSettings.JeekEasytierManagerZipUrl;
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
