using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using JeekTools;

namespace JeekEasyTierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    public static MainViewModel Instance { get; private set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<ConfigInfo> Configs { get; set; } = [];

    public async Task Init()
    {
        GitHubMirrors.TestUrl = AppSettings.JeekEasyTierManagerZipUrl;

        await AppSettings.Load();
        await LoadConfigs(true);
        CheckHasEasyTier();
        await ShowInfo();
        await ApplySettings();
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
