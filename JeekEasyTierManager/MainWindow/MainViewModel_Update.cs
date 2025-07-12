using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace JeekEasyTierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    [RelayCommand]
    public async Task UpdateEasyTier(bool clearMessages)
    {
        if (clearMessages)
            Messages = "";

        var hasUpdate = await EasyTierUpdate.HasUpdate();

        Messages += $"\nEasyTier local version is {EasyTierUpdate.LocalVersion}, remote version is {EasyTierUpdate.RemoteVersion}";

        if (hasUpdate)
        {
            if (_mainWindow!.IsVisible)
            {
                var result = await MessageBoxManager.GetMessageBoxStandard(
                    "Update EasyTier", $"Do you want to update easytier to {EasyTierUpdate.RemoteVersion}?",
                    ButtonEnum.YesNo, Icon.Question)
                    .ShowWindowDialogAsync(_mainWindow!);
                if (result == ButtonResult.No)
                    return;
            }

            Messages += $"\nUpdating EasyTier to {EasyTierUpdate.RemoteVersion}...";
            await StopAllServices();
            if (!await EasyTierUpdate.Update((progress) =>
            {
                Messages = $"\nStart downloading EasyTier from {EasyTierUpdate.DownloadUrl}";
                Messages += $"\nDownloading EasyTier: {progress:F2}%";
            }))
            {
                Messages += $"\nUpdate EasyTier failed: {EasyTierUpdate.LastError}";
                return;
            }
            CheckHasEasyTier();
            await RestoreAllServices();
            Messages += $"\nUpdate EasyTier to {EasyTierUpdate.RemoteVersion} completed.";
        }
        else
        {
            Messages += "\nNo update of EasyTier found.";
        }
    }

    [RelayCommand]
    public async Task UpdateMe(bool clearMessages)
    {
        if (clearMessages)
            Messages = "";

        var hasUpdate = await AutoUpdate.HasUpdate();

        Messages += $"\nChecking update from {AutoUpdate.DownloadUrl}";
        Messages += $"\nMy local version is {AutoUpdate.LocalTime}, remote version is {AutoUpdate.RemoteTime}";

        if (hasUpdate)
        {
            if (_mainWindow!.IsVisible)
            {
                var result = await MessageBoxManager.GetMessageBoxStandard(
                    "Update Me", $"Do you want to update me to {AutoUpdate.RemoteTime}?",
                    ButtonEnum.YesNo, Icon.Question)
                    .ShowWindowDialogAsync(_mainWindow!);
                if (result == ButtonResult.No)
                    return;
            }

            Messages += "\nUpdating me...";
            AutoUpdate.Update(!_mainWindow!.IsVisible);
        }
        else
        {
            Messages += "\nNo update of me found.";
        }
    }

    [ObservableProperty]
    public partial bool HasEasyTier { get; set; } = true;

    private void CheckHasEasyTier()
    {
        HasEasyTier = File.Exists(AppSettings.EasyTierCorePath) && File.Exists(AppSettings.EasyTierCliPath);
    }

}