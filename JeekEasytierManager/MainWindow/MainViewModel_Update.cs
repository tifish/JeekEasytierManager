using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
            await ForceUpdateEasytier();
        }
        else
        {
            Messages += "\nNo update found.";
        }
    }

    private async Task ForceUpdateEasytier()
    {
        Messages += "\nUpdating easytier...";
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