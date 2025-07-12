using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using JeekTools;

namespace JeekEasytierManager;

public static class AutoUpdate
{
    private static string _downloadUrl = "";
    public static DateTime? RemoteTime { get; private set; } = null;
    public static DateTime? LocalTime { get; private set; } = null;

    public static async Task<bool> HasUpdate()
    {
        try
        {
            _downloadUrl = "";
            RemoteTime = null;
            LocalTime = null;

            // Get the fastest mirror
            var mirror = await GitHubMirrors.GetFastestMirror(AppSettings.JeekEasytierManagerZipUrl);
            if (mirror == "")
                return false;

            _downloadUrl = mirror;

            // Try to get the headers from the mirror
            var headers = await HttpHelper.GetHeaders(_downloadUrl);
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
            if (_downloadUrl == "")
                return false;

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"""
                        -ExecutionPolicy Bypass -File "AutoUpdate.ps1" "{_downloadUrl}" {(hideMainWindow ? "/hide" : "")}
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
