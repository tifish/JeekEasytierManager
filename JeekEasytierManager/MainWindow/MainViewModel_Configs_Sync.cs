using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JeekTools;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Avalonia.Threading;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    class LatestFileInfo
    {
        public string FileName = "";
        public DateTime FileTimeUtc = DateTime.MinValue;
        public bool IsLatest = false;
        public int RpcClientIndex = -1;
    }

    [RelayCommand]
    public async Task SyncConfigs()
    {
        await UpdateAllServicesStatus();

        // Get all rpc clients
        var rpcClients = await GetAllRpcClients();
        if (rpcClients.Count == 0)
            return;

        // Get local file info list
        var localFileInfoList = GetConfigFileInfoList();
        var localFileNameIndexDict = new Dictionary<string, int>();
        for (var i = 0; i < localFileInfoList.Count; i++)
        {
            localFileNameIndexDict.Add(localFileInfoList[i].FileName, i);
        }

        var localNeedRefresh = false;

        // Get all file info list from all rpc clients
        foreach (var rpcClient in rpcClients)
        {
            var remoteFileInfoList = await rpcClient.GetConfigFileInfoList();
            var remoteFileNameIndexDict = new Dictionary<string, int>();
            for (var i = 0; i < remoteFileInfoList.Count; i++)
            {
                remoteFileNameIndexDict.Add(remoteFileInfoList[i].FileName, i);
            }

            // Find local only files
            var localOnlyFileInfos = localFileInfoList.Where(fileInfo => !remoteFileNameIndexDict.ContainsKey(fileInfo.FileName)).ToList();

            // Find remote only files
            var remoteOnlyFileInfos = remoteFileInfoList.Where(fileInfo => !localFileNameIndexDict.ContainsKey(fileInfo.FileName)).ToList();

            // Find local newer files (files that exist on both local and remote, but local version is newer)
            var localNewerFileInfos = new List<ConfigFileInfo>();
            foreach (var localFileInfo in localFileInfoList)
            {
                if (remoteFileNameIndexDict.TryGetValue(localFileInfo.FileName, out var remoteIndex))
                {
                    var remoteFileInfo = remoteFileInfoList[remoteIndex];
                    if (localFileInfo.FileTimeUtc > remoteFileInfo.FileTimeUtc)
                        localNewerFileInfos.Add(localFileInfo);
                }
            }

            // Find remote newer files (files that exist on both local and remote, but remote version is newer)
            var remoteNewerFileInfos = new List<ConfigFileInfo>();
            foreach (var remoteFileInfo in remoteFileInfoList)
            {
                if (localFileNameIndexDict.TryGetValue(remoteFileInfo.FileName, out var localIndex))
                {
                    var localFileInfo = localFileInfoList[localIndex];
                    if (remoteFileInfo.FileTimeUtc > localFileInfo.FileTimeUtc)
                        remoteNewerFileInfos.Add(remoteFileInfo);
                }
            }

            var remoteNeedRefresh = false;

            // Send local only files and local newer files to remote
            if (localOnlyFileInfos.Count > 0 || localNewerFileInfos.Count > 0)
            {
                var fileNames = localOnlyFileInfos.Concat(localNewerFileInfos).Select(f => f.FileName).ToList();
                var fileContentList = await GetConfigFileContent(fileNames);
                await rpcClient.SendConfigFileContent(fileContentList);
                remoteNeedRefresh = localOnlyFileInfos.Count > 0;
            }

            if (DeleteExtraConfigsOnOtherNodesWhenNextSync)
            {
                // Get remote newer files from remote
                if (remoteNewerFileInfos.Count > 0)
                {
                    var fileNames = remoteNewerFileInfos.Select(fileInfo => fileInfo.FileName).ToList();
                    var remoteFileContentList = await rpcClient.GetConfigFileContent(fileNames);
                    await WriteConfigFileContent(remoteFileContentList);
                }

                // Delete remote only files on other nodes
                if (remoteOnlyFileInfos.Count > 0)
                {
                    var fileNames = remoteOnlyFileInfos.Select(fileInfo => fileInfo.FileName).ToList();
                    await rpcClient.DeleteExtraConfigs(fileNames);
                    remoteNeedRefresh = true;
                }

                DeleteExtraConfigsOnOtherNodesWhenNextSync = false;
            }
            else
            {
                // Get remote only files and remote newer files from remote
                if (remoteOnlyFileInfos.Count > 0 || remoteNewerFileInfos.Count > 0)
                {
                    var fileNames = remoteOnlyFileInfos.Concat(remoteNewerFileInfos)
                        .Select(fileInfo => fileInfo.FileName).ToList();
                    var remoteFileContentList = await rpcClient.GetConfigFileContent(fileNames);
                    await WriteConfigFileContent(remoteFileContentList);
                    localNeedRefresh = remoteOnlyFileInfos.Count > 0;
                }
            }

            // Refresh configs on remote
            if (remoteNeedRefresh)
                await rpcClient.RefreshConfigs();
        }

        // Refresh configs
        if (localNeedRefresh)
            await RefreshConfigs();
    }

    private async Task<List<ISyncService>> GetAllRpcClients()
    {
        var rpcClients = new List<ISyncService>();

        foreach (var config in Configs)
        {
            if (config.Status != ServiceStatus.Running)
                continue;

            var peers = await GetPeers(config);
            if (peers.Count == 0)
                continue;

            foreach (var peer in peers)
            {
                if (peer.Cost == "Local")
                    continue;

                var rpcClient = await RemoteCall.GetClient($"http://{peer.Ipv4}:16666");
                if (rpcClient == null)
                    continue;

                rpcClients.Add(rpcClient);
            }
        }

        return rpcClients;
    }

    private class PeerInfo
    {
        [JsonPropertyName("ipv4")]
        public string Ipv4 { get; set; } = "";

        [JsonPropertyName("cost")]
        public string Cost { get; set; } = "";
    }

    private async Task<List<PeerInfo>> GetPeers(ConfigInfo config)
    {
        var rpcSocket = GetRpcSocket(config.Name);
        var peersJson = await Executor.RunWithOutput(AppSettings.EasytierCliPath, $"-p {rpcSocket} -o json peer", Encoding.UTF8);
        var peers = JsonFile<List<PeerInfo>>.FromJson(peersJson);
        return peers ?? [];
    }

    public List<ConfigFileInfo> GetConfigFileInfoList()
    {
        // Can run on any thread
        var result = new List<ConfigFileInfo>();

        foreach (var configFile in Directory.GetFiles(AppSettings.ConfigDirectory))
        {
            var fileInfo = new ConfigFileInfo
            {
                FileName = Path.GetFileName(configFile),
                FileTimeUtc = File.GetLastWriteTimeUtc(configFile)
            };
            result.Add(fileInfo);
        }

        return result;
    }

    public async Task<List<ConfigFileInfo>> GetConfigFileContent(List<string> fileNames)
    {
        // Can run on any thread
        var result = new List<ConfigFileInfo>();

        foreach (var fileName in fileNames)
        {
            var filePath = Path.Join(AppSettings.ConfigDirectory, fileName);
            result.Add(new ConfigFileInfo
            {
                FileName = fileName,
                FileTimeUtc = File.GetLastWriteTimeUtc(filePath),
                Content = await File.ReadAllTextAsync(filePath),
            });
        }

        return result;
    }

    public async Task WriteConfigFileContent(List<ConfigFileInfo> fileContentList)
    {
        foreach (var fileContent in fileContentList)
        {
            var filePath = Path.Join(AppSettings.ConfigDirectory, fileContent.FileName);
            await File.WriteAllTextAsync(filePath, fileContent.Content);
            File.SetLastWriteTimeUtc(filePath, fileContent.FileTimeUtc);
        }

        foreach (var fileContent in fileContentList)
        {
            var configName = Path.GetFileNameWithoutExtension(fileContent.FileName);
            var config = Configs.FirstOrDefault(config => config.Name == configName);
            if (config != null)
            {
                if (config.Status == ServiceStatus.Running)
                {
                    await RestartService(config);
                    await UpdateServiceStatus(config);
                }
            }
        }
    }

    public async Task DeleteExtraConfigs(List<string> fileNames)
    {
        var isSelectedChanged = false;

        foreach (var fileName in fileNames)
        {
            var filePath = Path.Join(AppSettings.ConfigDirectory, fileName);
            if (File.Exists(filePath))
                File.Delete(filePath);

            var configName = Path.GetFileNameWithoutExtension(fileName);
            var configIndex = Configs.ToList().FindIndex(config => config.Name == configName);
            if (configIndex != -1)
            {
                var config = Configs[configIndex];

                if (config.IsSelected)
                    isSelectedChanged = true;

                await UninstallService(config);

                config.PropertyChanged -= OnConfigPropertyChanged;

                Configs.RemoveAt(configIndex);
            }
        }

        if (isSelectedChanged)
        {
            UpdateHasSelectedConfigs();
        }
    }

}