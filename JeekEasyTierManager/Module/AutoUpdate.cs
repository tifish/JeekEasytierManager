using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using JeekTools;

namespace JeekEasyTierManager;

public enum AutoUpdateCheckResult
{
    Available,
    UpToDate,
    Failed,
}

public static class AutoUpdate
{
    private const string UpdateScriptName = "AutoUpdate.ps1";

    public static string DownloadUrl { get; private set; } = "";
    public static int LocalCommitCount { get; private set; }
    public static int RemoteCommitCount { get; private set; }
    public static string FailureReason { get; private set; } = "";

    public static async Task<AutoUpdateCheckResult> CheckForUpdate()
    {
        try
        {
            DownloadUrl = "";
            LocalCommitCount = GetLocalCommitCount();
            RemoteCommitCount = 0;
            FailureReason = "";

            string versionUrl;
            if (Settings.DisableMirrorDownload)
            {
                DownloadUrl = AppSettings.JeekEasyTierManagerZipUrl;
                versionUrl = AppSettings.JeekEasyTierManagerVersionTxtUrl;
            }
            else
            {
                var mirror = await GitHubMirrors.GetFastestMirror(
                    AppSettings.JeekEasyTierManagerZipUrl
                );
                if (mirror == "")
                    return Fail("no reachable mirror");

                DownloadUrl = mirror;
                versionUrl = await GitHubMirrors.GetFastestMirror(
                    AppSettings.JeekEasyTierManagerVersionTxtUrl
                );
                if (versionUrl == "")
                    versionUrl = AppSettings.JeekEasyTierManagerVersionTxtUrl;
            }

            var remote = await DownloadTextAsync(versionUrl);
            if (string.IsNullOrWhiteSpace(remote))
                return Fail($"empty version.txt from {versionUrl}");

            if (!int.TryParse(remote.Trim(), out var remoteCount) || remoteCount <= 0)
                return Fail($"version.txt did not contain a positive integer: '{remote.Trim()}'");
            RemoteCommitCount = remoteCount;

            if (LocalCommitCount <= 0)
                return Fail("local version unavailable (development build)");

            return RemoteCommitCount > LocalCommitCount
                ? AutoUpdateCheckResult.Available
                : AutoUpdateCheckResult.UpToDate;
        }
        catch (Exception ex)
        {
            return Fail($"exception: {ex.Message}");
        }
    }

    public static bool Update(bool hideMainWindow)
    {
        try
        {
            if (DownloadUrl == "")
                return false;

            var scriptPath = Path.Join(AppSettings.AppDirectory, UpdateScriptName);
            if (!File.Exists(scriptPath))
                return false;
            var restartArgument = hideMainWindow ? " /hide" : "";

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments =
                        $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" \"{DownloadUrl}\"{restartArgument}",
                    WorkingDirectory = AppSettings.AppDirectory,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                }
            );

            App.ExitApplication();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static AutoUpdateCheckResult Fail(string reason)
    {
        FailureReason = reason;
        return AutoUpdateCheckResult.Failed;
    }

    private static async Task<string?> DownloadTextAsync(string url)
    {
        try
        {
            using var client = HttpHelper.GetHttpClient();
            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }
    }

    public static int GetLocalCommitCount()
    {
        try
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.Major ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
