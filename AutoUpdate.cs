using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace JeekEasytierManager;

public static class AutoUpdate
{
    public static async Task<bool> HasUpdate()
    {
        var headers = await HttpHelper.GetHeaders(Settings.JeekEasytierManagerZipUrl);

        var updateTime = headers?.LastModified;
        if (updateTime == null)
            return false;

        var exeTime = File.GetLastWriteTime(Settings.ExePath);

        return updateTime - exeTime > TimeSpan.FromMinutes(1);
    }

    public static bool Update()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"""
                        -ExecutionPolicy Bypass -File "AutoUpdate.ps1" "{Settings.JeekEasytierManagerZipUrl}"
                        """,
            WorkingDirectory = Settings.AppDirectory,
            UseShellExecute = true,
        });

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.MainWindow?.Close();

        return true;
    }
}
