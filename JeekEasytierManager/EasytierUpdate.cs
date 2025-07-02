using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace JeekEasytierManager;

public static class EasytierUpdate
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

            if (!File.Exists(AppSettings.EasytierCliPath))
                return true;

            var output = await Nssm.RunWithOutput(AppSettings.EasytierCliPath, "--version");
            // easytier-cli 2.3.2-42c98203
            LocalVersion = output.Split(' ')[1].Split('-')[0];

            return RemoteVersion != LocalVersion;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<string> GetLastestVersion()
    {
        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync(AppSettings.EasytierLatestReleasePageUrl);
        var link = doc.DocumentNode.SelectSingleNode("//a[contains(@href, '/EasyTier/EasyTier/releases/tag/')]");
        if (link == null)
            return "";

        var href = link.GetAttributeValue("href", "");
        if (!href.Contains('/'))
            return "";

        return href.Split('/')[^1].TrimStart('v');
    }

    public static async Task<string> GetLastestZipUrl()
    {
        RemoteVersion = await GetLastestVersion();
        if (RemoteVersion == "")
            return "";

        // https://github.com/EasyTier/EasyTier/releases/download/v2.3.2/easytier-windows-x86_64-v2.3.2.zip
        return $"https://github.com/EasyTier/EasyTier/releases/download/v{RemoteVersion}/easytier-windows-x86_64-v{RemoteVersion}.zip";
    }

    public static async Task<bool> Update()
    {
        try
        {
            var zipUrl = await GetLastestZipUrl();
            var zipPath = await HttpHelper.DownloadFile(zipUrl, Path.GetTempPath());

            if (Directory.Exists(AppSettings.EasytierDirectory))
                Directory.Delete(AppSettings.EasytierDirectory, true);

            ZipFile.ExtractToDirectory(zipPath, AppSettings.AppDirectory);

            // Rename easytier-windows-x86_64\ to Easytier\
            var easytierDirectory = Path.Join(AppSettings.AppDirectory, "easytier-windows-x86_64");
            if (Directory.Exists(easytierDirectory))
                Directory.Move(easytierDirectory, AppSettings.EasytierDirectory);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
