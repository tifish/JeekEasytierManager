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
    public async Task UpdateEasytier(bool clearMessages)
    {
        if (clearMessages)
            Messages = "";

        var hasUpdate = await EasytierUpdate.HasUpdate();

        Messages += $"Local version is {EasytierUpdate.LocalVersion}, remote version is {EasytierUpdate.RemoteVersion}";

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

            Messages += $"\nUpdating Easytier to {EasytierUpdate.RemoteVersion}...";
            await StopService();
            await EasytierUpdate.Update();
            CheckHasEasytier();
            await RestartService();
            Messages += "\nUpdate completed.";
        }
        else
        {
            Messages += "\nNo update of Easytier found.";
        }
    }

    [RelayCommand]
    public async Task UpdateMe(bool clearMessages)
    {
        if (clearMessages)
            Messages = "";

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

            Messages += "\nUpdating me...";
            AutoUpdate.Update(!_mainWindow!.IsVisible);
        }
        else
        {
            Messages += "\nNo update of me found.";
        }
    }

    [ObservableProperty]
    public partial bool HasEasytier { get; set; } = true;

    private void CheckHasEasytier()
    {
        HasEasytier = File.Exists(AppSettings.EasytierCorePath) && File.Exists(AppSettings.EasytierCliPath);
    }

}