using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jeek.Avalonia.Localization;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace JeekEasyTierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    public bool IsDefaultStorage => StorageManager.ActiveMode == StorageMode.Default;
    public bool IsPortableStorage => StorageManager.ActiveMode == StorageMode.Portable;
    public bool IsCustomStorage => StorageManager.ActiveMode == StorageMode.Custom;
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
                await SwitchStorageMode(StorageMode.Default, null);
                break;
            case "Portable":
                await SwitchStorageMode(StorageMode.Portable, null);
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

            await SwitchStorageMode(StorageMode.Custom, path);
        }
        catch (Exception ex)
        {
            ClearMessages();
            AddMessage(string.Format(Localizer.Get("Storage_ChooseFailed"), ex.Message));
            RefreshStorageSelectionProperties();
        }
    }

    public async Task SwitchStorageMode(StorageMode newMode, string? customDirectory)
    {
        var oldMode = StorageManager.ActiveMode;
        var oldRoot = StorageManager.ActiveRoamingRoot;
        var newRoot = StorageManager.GetRoamingRoot(newMode, customDirectory);

        // Nothing to do if the location does not change.
        if (string.Equals(oldRoot, newRoot, StringComparison.OrdinalIgnoreCase))
        {
            RefreshStorageSelectionProperties();
            return;
        }

        // Validate a custom target before touching anything.
        if (newMode == StorageMode.Custom
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

        ClearMessages();
        AddMessage(string.Format(Localizer.Get("Storage_Migrating"), oldRoot, newRoot));

        // Remember the installed services (running ones are tracked by StopAllServices).
        var installedConfigs = Configs.Where(c => c.Service != null).ToList();
        await StopAllServices();

        // Migrate roaming data (both Config and Settings) to the new location.
        try
        {
            DirectoryMigrator.MoveMerge(
                Path.Combine(oldRoot, "Config"),
                Path.Combine(newRoot, "Config")
            );
            DirectoryMigrator.MoveMerge(
                Path.Combine(oldRoot, "Settings"),
                Path.Combine(newRoot, "Settings")
            );
        }
        catch (Exception ex)
        {
            AddMessage(string.Format(Localizer.Get("Storage_MigrationFailed"), ex.Message));
            await RestoreAllServices();
            RefreshStorageSelectionProperties();
            return;
        }

        // Activate the new location, then persist the mode so it matches where the files now live.
        StorageManager.SetActive(newMode, newRoot);
        LocalSettings.Current.StorageMode = newMode;
        LocalSettings.Current.CustomDirectory = newMode == StorageMode.Custom ? newRoot : "";
        await LocalSettings.Save();

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
        if (oldMode == StorageMode.Portable && newMode != StorageMode.Portable)
        {
            TryDeleteDirectory(StorageManager.PortableConfigDirectory);
            TryDeleteDirectory(StorageManager.PortableSettingsDirectory);
        }

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
}
