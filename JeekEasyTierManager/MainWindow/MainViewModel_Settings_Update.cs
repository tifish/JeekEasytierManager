using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jeek.Avalonia.Localization;
using JeekTools;
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
            string.Format(
                Localizer.Get("Update_EasyTierVersionStatus"),
                EasyTierUpdate.LocalVersion,
                EasyTierUpdate.RemoteVersion
            )
        );

        if (hasUpdate)
        {
            if (_mainWindow!.IsVisible)
            {
                var result = await MessageBoxManager
                    .GetMessageBoxStandard(
                        Localizer.Get("Update_EasyTierTitle"),
                        string.Format(
                            Localizer.Get("Update_ConfirmEasyTier"),
                            EasyTierUpdate.RemoteVersion
                        ),
                        ButtonEnum.YesNo,
                        Icon.Question
                    )
                    .ShowWindowDialogAsync(_mainWindow!);
                if (result == ButtonResult.No)
                    return;
            }

            AddMessage(
                string.Format(Localizer.Get("Update_UpdatingEasyTier"), EasyTierUpdate.RemoteVersion)
            );
            await StopAllServices();

            AddMessage(
                string.Format(Localizer.Get("Update_StartDownloading"), EasyTierUpdate.DownloadUrl)
            );
            DownloadProgress = 0;
            DownloadStatus = string.Format(Localizer.Get("Update_DownloadingProgress"), 0.00);
            IsDownloading = true;

            bool updateOk;
            try
            {
                updateOk = await EasyTierUpdate.Update(progress =>
                {
                    DownloadProgress = progress;
                    DownloadStatus = string.Format(
                        Localizer.Get("Update_DownloadingProgress"),
                        progress
                    );
                });
            }
            finally
            {
                IsDownloading = false;
            }

            if (!updateOk)
            {
                AddMessage(
                    string.Format(Localizer.Get("Update_EasyTierFailed"), EasyTierUpdate.LastError)
                );
                return;
            }

            CheckHasEasyTier();
            await RestoreAllServices();
            AddMessage(
                string.Format(Localizer.Get("Update_EasyTierCompleted"), EasyTierUpdate.RemoteVersion)
            );
        }
        else
        {
            AddMessage(Localizer.Get("Update_NoEasyTierUpdate"));
        }
    }

    [RelayCommand]
    public async Task UpdateMe(bool clearMessages)
    {
        if (clearMessages)
            ClearMessages();

        var checkResult = await AutoUpdate.CheckForUpdate();

        if (!string.IsNullOrEmpty(AutoUpdate.DownloadUrl))
            AddMessage(string.Format(Localizer.Get("Update_CheckingUpdate"), AutoUpdate.DownloadUrl));
        AddMessage(
            string.Format(
                Localizer.Get("Update_MeBuildStatus"),
                FormatBuildVersion(AutoUpdate.LocalCommitCount),
                FormatBuildVersion(AutoUpdate.RemoteCommitCount)
            )
        );

        if (checkResult == UpdateCheckOutcome.Available)
        {
            if (_mainWindow!.IsVisible)
            {
                var result = await MessageBoxManager
                    .GetMessageBoxStandard(
                        Localizer.Get("Update_MeTitle"),
                        string.Format(
                            Localizer.Get("Update_ConfirmMe"),
                            AutoUpdate.RemoteCommitCount
                        ),
                        ButtonEnum.YesNo,
                        Icon.Question
                    )
                    .ShowWindowDialogAsync(_mainWindow!);
                if (result == ButtonResult.No)
                    return;
            }

            AddMessage(Localizer.Get("Update_UpdatingMe"));

            // Download and stage in-app, so a failed download never leaves the user without a
            // running app; only the final swap happens in the updater script.
            DownloadProgress = 0;
            DownloadStatus = string.Format(Localizer.Get("Update_DownloadingProgress"), 0.00);
            IsDownloading = true;
            string? stageDir;
            try
            {
                var progress = new Progress<UpdateDownloadProgress>(p =>
                {
                    var percent =
                        p.TotalBytes is > 0 ? p.ReceivedBytes * 100.0 / p.TotalBytes.Value : 0;
                    DownloadProgress = percent;
                    DownloadStatus = string.Format(
                        Localizer.Get("Update_DownloadingProgress"),
                        percent
                    );
                });
                stageDir = await AutoUpdate.DownloadAndStage(progress);
            }
            finally
            {
                IsDownloading = false;
            }

            if (stageDir == null)
            {
                AddMessage(
                    string.Format(Localizer.Get("Update_MeCheckFailed"), AutoUpdate.FailureReason)
                );
                return;
            }

            if (!AutoUpdate.Install(stageDir))
                AddMessage(Localizer.Get("Update_MeFailedLaunchUpdater"));
        }
        else if (checkResult == UpdateCheckOutcome.Failed)
        {
            AddMessage(string.Format(Localizer.Get("Update_MeCheckFailed"), AutoUpdate.FailureReason));
        }
        else
        {
            AddMessage(Localizer.Get("Update_NoMeUpdate"));
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
        return build > 0 ? build.ToString() : Localizer.Get("Update_UnknownBuild");
    }

    public string AppVersionText
    {
        get
        {
            var build = AutoUpdate.GetLocalCommitCount();
            return build > 0
                ? string.Format(Localizer.Get("Update_BuildLabel"), build)
                : Localizer.Get("Update_DevBuild");
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
            AddMessage(string.Format(Localizer.Get("Update_OpenHomePageFailed"), ex.Message));
        }
    }
}
