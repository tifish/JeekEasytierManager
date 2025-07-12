using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using JeekTools;

namespace JeekEasyTierManager;

public static class AutoUpdate
{
    public static string DownloadUrl { get; private set; } = "";
    public static DateTime? RemoteTime { get; private set; } = null;
    public static DateTime? LocalTime { get; private set; } = null;

    public static async Task<bool> HasUpdate()
    {
        try
        {
            DownloadUrl = "";
            RemoteTime = null;
            LocalTime = null;

            if (Settings.DisableMirrorDownload)
            {
                DownloadUrl = AppSettings.JeekEasyTierManagerZipUrl;
            }
            else
            {
                // Get the fastest mirror
                var mirror = await GitHubMirrors.GetFastestMirror(AppSettings.JeekEasyTierManagerZipUrl);
                if (mirror == "")
                    return false;

                DownloadUrl = mirror;
            }

            // Try to get the headers from the mirror
            var headers = await HttpHelper.GetHeaders(DownloadUrl);
            if (headers == null)
                return false;

            RemoteTime = headers.LastModified;

            LocalTime = File.GetLastWriteTime(AppSettings.ExePath);

            return RemoteTime - LocalTime > TimeSpan.FromMinutes(1);
        }
        catch
        {
            return false;
        }
    }

    public static bool Update(bool hideMainWindow)
    {
        try
        {
            if (DownloadUrl == "")
                return false;

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"""
                        -ExecutionPolicy Bypass -File "AutoUpdate.ps1" "{DownloadUrl}" {(hideMainWindow ? "/hide" : "")}
                        """,
                WorkingDirectory = AppSettings.AppDirectory,
                UseShellExecute = true,
            });

            App.ExitApplication();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
