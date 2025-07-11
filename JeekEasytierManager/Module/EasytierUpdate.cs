using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JeekTools;

namespace JeekEasytierManager;

public static class EasytierUpdate
{
    private static int _mirrorIndex = 0;
    public static string RemoteVersion { get; private set; } = "";
    public static string LocalVersion { get; private set; } = "";

    public static async Task<bool> HasUpdate()
    {
        try
        {
            LocalVersion = "";
            RemoteVersion = "";

            RemoteVersion = await GetLastestVersion();

            if (!File.Exists(AppSettings.EasytierCliPath))
                return true;

            var output = await Executor.RunWithOutput(AppSettings.EasytierCliPath, "--version");
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
        var mirrors = GitHubMirrors.GetMirrors(AppSettings.EasytierLatestReleasePageUrl);
        HtmlDocument? doc = null;
        _mirrorIndex = 0;

        for (var i = 0; i < mirrors.Length; i++)
        {
            doc = await web.LoadFromWebAsync(mirrors[i]);
            if (doc != null)
            {
                _mirrorIndex = i;
                break;
            }
        }

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

    public static string GetLastestDownloadUrl()
    {
        if (RemoteVersion == "")
            return "";

        // https://github.com/EasyTier/EasyTier/releases/download/v2.3.2/easytier-windows-x86_64-v2.3.2.zip
        var downloadUrl = $"https://github.com/EasyTier/EasyTier/releases/download/v{RemoteVersion}/easytier-windows-x86_64-v{RemoteVersion}.zip";
        var mirrors = GitHubMirrors.GetMirrors(downloadUrl);
        return mirrors[_mirrorIndex];
    }

    public static async Task<bool> Update()
    {
        try
        {
            var downloadUrl = GetLastestDownloadUrl();
            if (downloadUrl == "")
                return false;

            var downloadPath = await HttpHelper.DownloadFile(downloadUrl, Path.GetTempPath());

            if (Directory.Exists(AppSettings.EasytierDirectory))
                Directory.Delete(AppSettings.EasytierDirectory, true);

            ZipFile.ExtractToDirectory(downloadPath, AppSettings.AppDirectory);

            // Rename easytier-windows-x86_64\ to Easytier\
            var easytierDirectory = Path.Join(AppSettings.AppDirectory, "easytier-windows-x86_64");
            if (Directory.Exists(easytierDirectory))
                Directory.Move(easytierDirectory, AppSettings.EasytierDirectory);

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
