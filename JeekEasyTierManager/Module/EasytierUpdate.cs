using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JeekTools;

namespace JeekEasyTierManager;

public static class EasyTierUpdate
{
    public static string RemoteVersion { get; private set; } = "";
    public static string LocalVersion { get; private set; } = "";

    public static async Task<bool> HasUpdate()
    {
        try
        {
            LocalVersion = "";
            RemoteVersion = "";

            RemoteVersion = await GetLastestVersion();
            if (RemoteVersion == "")
                return false;

            if (!File.Exists(AppSettings.EasyTierCliPath))
                return true;

            var output = await Executor.RunWithOutput(AppSettings.EasyTierCliPath, "--version");
            // easytier-cli 2.3.2-42c98203
            LocalVersion = output.Split(' ')[1].Split('-')[0];

            return RemoteVersion != LocalVersion;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string> GetLastestVersion()
    {
        var web = new HtmlWeb();

        string releasePageUrl;

        if (Settings.DisableMirrorDownload)
        {
            releasePageUrl = AppSettings.EasyTierLatestReleasePageUrl;
        }
        else
        {
            var mirror = await GitHubMirrors.GetFastestMirror(AppSettings.EasyTierLatestReleasePageUrl);
            if (mirror == "")
                return "";

            releasePageUrl = mirror;
        }

        var doc = await web.LoadFromWebAsync(releasePageUrl);
        if (doc == null)
            return "";

        var link = doc.DocumentNode.SelectSingleNode("//a[contains(@href, '/EasyTier/EasyTier/releases/tag/')]");
        if (link == null)
            return "";

        var href = link.GetAttributeValue("href", "");
        if (!href.Contains('/'))
            return "";

        return href.Split('/')[^1].TrimStart('v');
    }

    public static async Task<string> GetLastestDownloadUrl()
    {
        if (RemoteVersion == "")
            return "";

        // https://github.com/EasyTier/EasyTier/releases/download/v2.3.2/easytier-windows-x86_64-v2.3.2.zip
        var downloadUrl = $"https://github.com/EasyTier/EasyTier/releases/download/v{RemoteVersion}/easytier-windows-x86_64-v{RemoteVersion}.zip";

        if (Settings.DisableMirrorDownload)
            return downloadUrl;

        return await GitHubMirrors.GetFastestMirror(downloadUrl);
    }

    public static string DownloadUrl { get; private set; } = "";

    public static async Task<bool> Update(Action<double>? progressCallback = null)
    {
        try
        {
            DownloadUrl = await GetLastestDownloadUrl();
            if (DownloadUrl == "")
                return false;

            var downloadPath = await HttpHelper.DownloadFile(DownloadUrl, Path.GetTempPath(), progressCallback);
            if (downloadPath == null)
                return false;

            if (Directory.Exists(AppSettings.EasyTierDirectory))
                Directory.Delete(AppSettings.EasyTierDirectory, true);

            ZipFile.ExtractToDirectory(downloadPath, AppSettings.AppDirectory);

            // Rename easytier-windows-x86_64\ to EasyTier\
            var easytierDirectory = Path.Join(AppSettings.AppDirectory, "easytier-windows-x86_64");
            if (Directory.Exists(easytierDirectory))
                Directory.Move(easytierDirectory, AppSettings.EasyTierDirectory);

            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public static string LastError { get; private set; } = "";
}
