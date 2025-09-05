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

        AddMessage($"EasyTier local version is {EasyTierUpdate.LocalVersion}, remote version is {EasyTierUpdate.RemoteVersion}");

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

            AddMessage($"Updating EasyTier to {EasyTierUpdate.RemoteVersion}...");
            await StopAllServices();
            if (!await EasyTierUpdate.Update((progress) =>
            {
                AddMessage($"\nStart downloading EasyTier from {EasyTierUpdate.DownloadUrl}");
                AddMessage($"Downloading EasyTier: {progress:F2}%");
            }))
            {
                AddMessage($"Update EasyTier failed: {EasyTierUpdate.LastError}");
                return;
            }
            CheckHasEasyTier();
            await RestoreAllServices();
            AddMessage($"Update EasyTier to {EasyTierUpdate.RemoteVersion} completed.");
        }
        else
        {
            AddMessage("No update of EasyTier found.");
        }
    }

    [RelayCommand]
    public async Task UpdateMe(bool clearMessages)
    {
        if (clearMessages)
            Messages = "";

        var hasUpdate = await AutoUpdate.HasUpdate();

        AddMessage($"Checking update from {AutoUpdate.DownloadUrl}");
        AddMessage($"My local version is {AutoUpdate.LocalTime}, remote version is {AutoUpdate.RemoteTime}");

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

            AddMessage("Updating me...");
            AutoUpdate.Update(!_mainWindow!.IsVisible);
        }
        else
        {
            AddMessage("No update of me found.");
        }
    }

    [ObservableProperty]
    public partial bool HasEasyTier { get; set; } = true;

    private void CheckHasEasyTier()
    {
        HasEasyTier = File.Exists(AppSettings.EasyTierCorePath) && File.Exists(AppSettings.EasyTierCliPath);
    }

}