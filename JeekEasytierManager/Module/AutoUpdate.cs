using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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

            // Try to get the headers from the mirrors
            var mirrors = GitHubMirrors.GetMirrors(AppSettings.JeekEasytierManagerZipUrl);
            HttpHelper.HttpHeaders? headers = null;

            foreach (var mirror in mirrors)
            {
                headers = await HttpHelper.GetHeaders(mirror);
                if (headers != null)
                {
                    _downloadUrl = mirror;
                    break;
                }
            }

            if (headers == null)
                return false;

            // Get the update time
            RemoteTime = headers?.LastModified;
            if (RemoteTime == null)
                return false;

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
