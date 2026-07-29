using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jeek.Avalonia.Localization;
using JeekTools;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace JeekEasyTierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    public bool IsDefaultStorage => StorageManager.ActiveLocation == StorageLocation.UserDirectory;
    public bool IsPortableStorage =>
        StorageManager.ActiveLocation == StorageLocation.ProgramDirectory;
    public bool IsCustomStorage => StorageManager.ActiveLocation == StorageLocation.CustomDirectory;
    public string StorageLocationText => StorageManager.ActiveRoamingRoot;

    private void RefreshStorageSelectionProperties()
    {
        OnPropertyChanged(nameof(IsDefaultStorage));
        OnPropertyChanged(nameof(IsPortableStorage));
        OnPropertyChanged(nameof(IsCustomStorage));
        OnPropertyChanged(nameof(StorageLocationText));
    }

    [RelayCommand]
    public async Task SwitchStorageModeFromString(string mode)
    {
        switch (mode)
        {
            case "Default":
                await SwitchStorageMode(StorageLocation.UserDirectory, null);
                break;
            case "Portable":
                await SwitchStorageMode(StorageLocation.ProgramDirectory, null);
                break;
            case "Custom":
                await BrowseCustomDirectory();
                break;
        }
    }

    [RelayCommand]
    public async Task BrowseCustomDirectory()
    {
        try
        {
            if (_mainWindow == null)
                return;

            var startLocation = await _mainWindow.StorageProvider.TryGetFolderFromPathAsync(
                StorageManager.ActiveRoamingRoot
            );

            var folders = await _mainWindow.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = Localizer.Get("Storage_PickerTitle"),
                    AllowMultiple = false,
                    SuggestedStartLocation = startLocation,
                }
            );

            if (folders.Count == 0)
            {
                RefreshStorageSelectionProperties();
                return;
            }

            var path = folders[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                ClearMessages();
                AddMessage(Localizer.Get("Storage_NotLocalFolder"));
                RefreshStorageSelectionProperties();
                return;
            }

            await SwitchStorageMode(StorageLocation.CustomDirectory, path);
        }
        catch (Exception ex)
        {
            ClearMessages();
            AddMessage(string.Format(Localizer.Get("Storage_ChooseFailed"), ex.Message));
            RefreshStorageSelectionProperties();
        }
    }

    public async Task SwitchStorageMode(StorageLocation newMode, string? customDirectory)
    {
        var oldMode = StorageManager.ActiveLocation;
        var oldRoot = StorageManager.ActiveRoamingRoot;
        var newRoot = StorageManager.GetRoamingRoot(newMode, customDirectory);

        // Nothing to do if the location does not change.
        if (string.Equals(oldRoot, newRoot, StringComparison.OrdinalIgnoreCase))
        {
            RefreshStorageSelectionProperties();
            return;
        }

        // Validate a custom target before touching anything.
        if (newMode == StorageLocation.CustomDirectory
            && (string.IsNullOrWhiteSpace(newRoot) || !StorageManager.IsUsableDirectory(newRoot)))
        {
            ClearMessages();
            AddMessage(string.Format(Localizer.Get("Storage_CannotUse"), newRoot));
            RefreshStorageSelectionProperties();
            return;
        }

        // Confirm, since this stops, reinstalls and restarts the related services.
        if (_mainWindow != null && _mainWindow.IsVisible)
        {
            var result = await MessageBoxManager
                .GetMessageBoxStandard(
                    Localizer.Get("Storage_SwitchTitle"),
                    string.Format(Localizer.Get("Storage_SwitchConfirm"), oldRoot, newRoot),
                    ButtonEnum.YesNo,
                    Icon.Question
                )
                .ShowWindowDialogAsync(_mainWindow);
            if (result == ButtonResult.No)
            {
                RefreshStorageSelectionProperties();
                return;
            }
        }

        // Ask whether to move the existing files. Leaving portable mode must move them, otherwise
        // the program-dir data would force portable mode again on next startup.
        var moveFiles = true;
        if (_mainWindow != null && _mainWindow.IsVisible)
        {
            if (oldMode == StorageLocation.ProgramDirectory)
            {
                var result = await MessageBoxManager
                    .GetMessageBoxStandard(
                        Localizer.Get("Storage_SwitchTitle"),
                        Localizer.Get("Storage_LeavePortableMustMove"),
                        ButtonEnum.YesNo,
                        Icon.Warning
                    )
                    .ShowWindowDialogAsync(_mainWindow);
                if (result == ButtonResult.No)
                {
                    RefreshStorageSelectionProperties();
                    return;
                }
            }
            else
            {
                var result = await MessageBoxManager
                    .GetMessageBoxStandard(
                        Localizer.Get("Storage_SwitchTitle"),
                        Localizer.Get("Storage_AskMoveFiles"),
                        ButtonEnum.YesNo,
                        Icon.Question
                    )
                    .ShowWindowDialogAsync(_mainWindow);
                moveFiles = result == ButtonResult.Yes;
            }
        }

        ClearMessages();
        if (moveFiles)
            AddMessage(string.Format(Localizer.Get("Storage_Migrating"), oldRoot, newRoot));

        // Remember the installed services (running ones are tracked by StopAllServices).
        var installedConfigs = Configs.Where(c => c.Service != null).ToList();
        await StopAllServices();
        StopWatchingStorage();

        // Migrate roaming data (both Config and Settings) to the new location.
        if (moveFiles)
        {
            try
            {
                MoveRoamingDirectory(
                    Path.Combine(oldRoot, "Config"),
                    Path.Combine(newRoot, "Config")
                );
                MoveRoamingDirectory(
                    Path.Combine(oldRoot, "Settings"),
                    Path.Combine(newRoot, "Settings")
                );
            }
            catch (Exception ex)
            {
                AddMessage(string.Format(Localizer.Get("Storage_MigrationFailed"), ex.Message));
                StartWatchingStorage();
                await RestoreAllServices();
                RefreshStorageSelectionProperties();
                return;
            }
        }

        // Activate the new location, then persist the mode so it matches where the files now live.
        StorageManager.SetActive(newMode, newRoot);
        LocalSettings.Current.StorageMode = newMode;
        LocalSettings.Current.CustomDirectory =
            newMode == StorageLocation.CustomDirectory ? newRoot : "";
        await LocalSettings.Save();

        // When the files were not moved, the new location may hold different data; load it.
        if (!moveFiles)
            await ReloadRoamingSettings();

        // Reinstall each installed service so the baked NSSM "-c" path points at the new location.
        foreach (var config in installedConfigs)
        {
            await UninstallService(config);
            await InstallService(config);
        }

        // Rebuild the config list (rebinds services, restores selection) and restart what was running.
        LoadConfigs(false);
        await RestoreAllServices();

        // Leaving portable: make sure the program-dir folders are gone so startup won't force portable.
        if (
            oldMode == StorageLocation.ProgramDirectory
            && newMode != StorageLocation.ProgramDirectory
        )
        {
            StorageManager.Storage.TryDeleteProgramConfig(out _);
            TryDeleteDirectory(StorageManager.PortableSettingsDirectory);
        }

        StartWatchingStorage();
        RefreshStorageSelectionProperties();
        await ShowInfo();

        AddMessage(string.Format(Localizer.Get("Storage_LocationNow"), newRoot));
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Best effort; a leftover folder only means portable may be re-detected next start.
        }
    }

    /// <summary>
    /// Moves one roaming data directory via <see cref="SettingsStorage.MoveConfigRoot"/>, retrying
    /// transient errors: EasyTier service processes can briefly keep a file handle open right
    /// after being stopped.
    /// </summary>
    private static void MoveRoamingDirectory(string source, string target)
    {
        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                SettingsStorage.MoveConfigRoot(source, target);
                return;
            }
            catch (Exception ex)
                when ((ex is IOException or UnauthorizedAccessException) && attempt < maxAttempts)
            {
                Thread.Sleep(200);
            }
        }
    }
}
