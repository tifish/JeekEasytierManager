using System;
using System.Diagnostics;
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
            ClearMessages();

        var hasUpdate = await EasyTierUpdate.HasUpdate();

        AddMessage(
            $"EasyTier local version is {EasyTierUpdate.LocalVersion}, remote version is {EasyTierUpdate.RemoteVersion}"
        );

        if (hasUpdate)
        {
            if (_mainWindow!.IsVisible)
            {
                var result = await MessageBoxManager
                    .GetMessageBoxStandard(
                        "Update EasyTier",
                        $"Do you want to update easytier to {EasyTierUpdate.RemoteVersion}?",
                        ButtonEnum.YesNo,
                        Icon.Question
                    )
                    .ShowWindowDialogAsync(_mainWindow!);
                if (result == ButtonResult.No)
                    return;
            }

            AddMessage($"Updating EasyTier to {EasyTierUpdate.RemoteVersion}...");
            await StopAllServices();

            AddMessage($"Start downloading EasyTier from {EasyTierUpdate.DownloadUrl}");
            DownloadProgress = 0;
            DownloadStatus = "Downloading EasyTier... 0.00%";
            IsDownloading = true;

            bool updateOk;
            try
            {
                updateOk = await EasyTierUpdate.Update(progress =>
                {
                    DownloadProgress = progress;
                    DownloadStatus = $"Downloading EasyTier... {progress:F2}%";
                });
            }
            finally
            {
                IsDownloading = false;
            }

            if (!updateOk)
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
            ClearMessages();

        var checkResult = await AutoUpdate.CheckForUpdate();

        if (!string.IsNullOrEmpty(AutoUpdate.DownloadUrl))
            AddMessage($"Checking update from {AutoUpdate.DownloadUrl}");
        AddMessage(
            $"My local build is {FormatBuildVersion(AutoUpdate.LocalCommitCount)}, remote build is {FormatBuildVersion(AutoUpdate.RemoteCommitCount)}"
        );

        if (checkResult == AutoUpdateCheckResult.Available)
        {
            if (_mainWindow!.IsVisible)
            {
                var result = await MessageBoxManager
                    .GetMessageBoxStandard(
                        "Update Me",
                        $"Do you want to update me to build {AutoUpdate.RemoteCommitCount}?",
                        ButtonEnum.YesNo,
                        Icon.Question
                    )
                    .ShowWindowDialogAsync(_mainWindow!);
                if (result == ButtonResult.No)
                    return;
            }

            AddMessage("Updating me...");
            if (!AutoUpdate.Update(!_mainWindow!.IsVisible))
                AddMessage("Update me failed: failed to launch updater.");
        }
        else if (checkResult == AutoUpdateCheckResult.Failed)
        {
            AddMessage($"Check update of me failed: {AutoUpdate.FailureReason}");
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
        HasEasyTier =
            File.Exists(AppSettings.EasyTierCorePath) && File.Exists(AppSettings.EasyTierCliPath);
    }

    private static string FormatBuildVersion(int build)
    {
        return build > 0 ? build.ToString() : "unknown";
    }

    public string AppVersionText
    {
        get
        {
            var build = AutoUpdate.GetLocalCommitCount();
            return build > 0 ? $"Build {build}" : "Development build";
        }
    }

    [RelayCommand]
    public void OpenHomePage()
    {
        try
        {
            Process.Start(
                new ProcessStartInfo { FileName = AppSettings.HomePageUrl, UseShellExecute = true }
            );
        }
        catch (Exception ex)
        {
            ClearMessages();
            AddMessage($"Failed to open home page: {ex.Message}");
        }
    }
}
