using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JeekTools;

namespace JeekEasyTierManager;

/// <summary>
/// App-specific configuration over the generic <see cref="AutoUpdater"/> in JeekTools. See that
/// class for how checking, staging, and installing work.
/// </summary>
public static class AutoUpdate
{
    private static readonly AutoUpdater Updater = new(
        new AutoUpdaterOptions
        {
            AppExeName = "JeekEasyTierManager.exe",
            ReleaseZipUrl = AppSettings.JeekEasyTierManagerZipUrl,
            VersionTxtUrl = AppSettings.JeekEasyTierManagerVersionTxtUrl,
            UserAgent = "JeekEasyTierManager-Updater/1.0",
#if DEBUG
            // Debug builds never self-update.
            Disabled = true,
#endif
        }
    );

    public static string DownloadUrl => Updater.DownloadUrl;
    public static int LocalCommitCount => Updater.LocalVersion;
    public static int RemoteCommitCount => Updater.RemoteVersion;
    public static string FailureReason => Updater.FailureReason;

    public static int GetLocalCommitCount() => Updater.GetLocalVersion();

    public static Task<UpdateCheckOutcome> CheckForUpdate() => Updater.HasUpdateAsync();

    public static Task<string?> DownloadAndStage(
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        // Honor the "disable mirror download" option by downloading directly from GitHub.
        IReadOnlyList<string>? urls = Settings.DisableMirrorDownload
            ? [AppSettings.JeekEasyTierManagerZipUrl]
            : null;
        return Updater.DownloadAndStageAsync(urls, progress, cancellationToken);
    }

    /// <summary>Hands the staged package to the updater script and exits the app.</summary>
    public static bool Install(string stagedPackageDir)
    {
        if (!Updater.LaunchInstall(stagedPackageDir))
            return false;

        App.ExitApplication();
        return true;
    }
}
