using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace JeekEasytierManager;

public static class AutoUpdate
{
    public static async Task<bool> HasUpdate()
    {
        try
        {
            var headers = await HttpHelper.GetHeaders(AppSettings.JeekEasytierManagerZipUrl);

            var updateTime = headers?.LastModified;
            if (updateTime == null)
                return false;

            var exeTime = File.GetLastWriteTime(AppSettings.ExePath);

            return updateTime - exeTime > TimeSpan.FromMinutes(1);
        }
        catch
        {
            return false;
        }
    }

    public static bool Update()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"""
                        -ExecutionPolicy Bypass -File "AutoUpdate.ps1" "{AppSettings.JeekEasytierManagerZipUrl}"
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
