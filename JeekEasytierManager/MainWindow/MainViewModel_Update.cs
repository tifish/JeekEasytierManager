using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    [RelayCommand]
    public async Task UpdateEasytier()
    {
        var hasUpdate = await EasytierUpdate.HasUpdate();

        Messages = $"Local version is {EasytierUpdate.LocalVersion}, remote version is {EasytierUpdate.RemoteVersion}";

        if (hasUpdate)
        {
            if (_mainWindow!.IsVisible)
            {
                var result = await MessageBoxManager.GetMessageBoxStandard(
                    "Update Easytier", $"Do you want to update easytier to {EasytierUpdate.RemoteVersion}?",
                    ButtonEnum.YesNo, Icon.Question)
                    .ShowAsync();
                if (result == ButtonResult.No)
                    return;
            }

            await ForceUpdateEasytier();
        }
        else
        {
            Messages += "\nNo update found.";
        }
    }

    private async Task ForceUpdateEasytier()
    {
        Messages += $"\nUpdating easytier to {EasytierUpdate.RemoteVersion}...";
        await StopService();
        await EasytierUpdate.Update();
        CheckHasEasytier();
        await RestartService();
        Messages += "\nUpdate completed.";
    }

    [RelayCommand]
    public async Task UpdateMe()
    {
        if (await AutoUpdate.HasUpdate())
        {
            if (_mainWindow!.IsVisible)
            {
                var result = await MessageBoxManager.GetMessageBoxStandard(
                    "Update Me", $"Do you want to update me?",
                    ButtonEnum.YesNo, Icon.Question)
                    .ShowAsync();
                if (result == ButtonResult.No)
                    return;
            }

            ForceUpdateMe();
        }
        else
        {
            Messages = "\nNo update found.";
        }
    }

    private void ForceUpdateMe()
    {
        Messages = "\nUpdating Me...";
        AutoUpdate.Update(!_mainWindow!.IsVisible);
    }

    [ObservableProperty]
    public partial bool HasEasytier { get; set; } = true;

    private void CheckHasEasytier()
    {
        HasEasytier = File.Exists(AppSettings.EasytierCorePath) && File.Exists(AppSettings.EasytierCliPath);
    }

}