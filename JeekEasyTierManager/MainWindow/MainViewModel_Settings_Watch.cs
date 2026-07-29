using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JeekEasyTierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    // Roaming data (Config\*.toml and Settings\Settings.json) may be edited outside the app,
    // especially in portable installs. Watch the active folders and reload only the changed files
    // after 10 seconds of quiet.
    private static readonly TimeSpan StorageReloadDelay = TimeSpan.FromSeconds(10);

    private readonly List<FileSystemWatcher> _storageWatchers = [];
    private readonly HashSet<string> _pendingStorageChanges = new(
        StringComparer.OrdinalIgnoreCase
    );
    private DispatcherTimer? _storageReloadTimer;

    public void StartWatchingStorage()
    {
        StopWatchingStorage();

        string[] directories =
        [
            StorageManager.ActiveConfigDirectory,
            StorageManager.ActiveSettingsDirectory,
        ];
        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
                continue;

            try
            {
                var watcher = new FileSystemWatcher(directory)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter =
                        NotifyFilters.FileName
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.LastWrite
                        | NotifyFilters.Size,
                };
                watcher.Created += OnStorageChanged;
                watcher.Deleted += OnStorageChanged;
                watcher.Renamed += OnStorageChanged;
                watcher.Changed += OnStorageChanged;
                watcher.EnableRaisingEvents = true;
                _storageWatchers.Add(watcher);
            }
            catch
            {
                // Watching is best effort; the app still works without picking up external edits.
            }
        }
    }

    public void StopWatchingStorage()
    {
        foreach (var watcher in _storageWatchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= OnStorageChanged;
            watcher.Deleted -= OnStorageChanged;
            watcher.Renamed -= OnStorageChanged;
            watcher.Changed -= OnStorageChanged;
            watcher.Dispose();
        }

        _storageWatchers.Clear();
        _pendingStorageChanges.Clear();
        _storageReloadTimer?.Stop();
    }

    // Events arrive on a background thread; hop to the UI thread and debounce.
    private void OnStorageChanged(object? sender, FileSystemEventArgs e)
    {
        if (StorageManager.IsSelfWriteRecent())
            return;

        var changedPath = e.FullPath;
        var oldPath = e is RenamedEventArgs renamed ? renamed.OldFullPath : null;

        Dispatcher.UIThread.Post(() => ScheduleStorageReload(changedPath, oldPath));
    }

    private void ScheduleStorageReload(string changedPath, string? oldPath)
    {
        AddPendingStorageChange(changedPath);
        if (!string.IsNullOrWhiteSpace(oldPath))
            AddPendingStorageChange(oldPath);

        if (_storageReloadTimer is null)
        {
            _storageReloadTimer = new DispatcherTimer { Interval = StorageReloadDelay };
            _storageReloadTimer.Tick += async (_, _) =>
            {
                _storageReloadTimer!.Stop();
                var changedPaths = _pendingStorageChanges.ToList();
                _pendingStorageChanges.Clear();

                try
                {
                    await ReloadChangedStorage(changedPaths);
                }
                catch (Exception ex)
                {
                    AddMessage($"Reload after external change failed: {ex.Message}");
                }
            };
        }

        // Restart the countdown on every event so loading only happens after the quiet period.
        _storageReloadTimer.Stop();
        _storageReloadTimer.Start();
    }

    private void AddPendingStorageChange(string path)
    {
        try
        {
            _pendingStorageChanges.Add(Path.GetFullPath(path));
        }
        catch
        {
            _pendingStorageChanges.Add(path);
        }
    }

    private async Task ReloadChangedStorage(IReadOnlyCollection<string> changedPaths)
    {
        var settingsChanged = changedPaths.Any(p =>
            string.Equals(
                p,
                Path.GetFullPath(StorageManager.ActiveSettingsFile),
                StringComparison.OrdinalIgnoreCase
            )
        );
        var configsChanged = changedPaths.Any(p =>
            p.StartsWith(
                Path.GetFullPath(StorageManager.ActiveConfigDirectory)
                    + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (settingsChanged)
            await ReloadRoamingSettings();

        if (configsChanged)
            LoadConfigs(false);
    }

    /// <summary>Reloads Settings.json from disk and reapplies it to the UI.</summary>
    private async Task ReloadRoamingSettings()
    {
        await AppSettings.Load();

        ApplyLanguage(Settings.Language);
        await ApplyTheme(Settings.Theme, false);

        DisableMirrorDownload = Settings.DisableMirrorDownload;
        AutoUpdateMe = Settings.AutoUpdateMe;
        AutoUpdateEasyTier = Settings.AutoUpdateEasyTier;
        UpdateInterval = Settings.UpdateCheckInterval;
        SyncPassword = Settings.SyncPassword;
        AutoRefreshInfo = Settings.AutoRefreshInfo;
    }
}
