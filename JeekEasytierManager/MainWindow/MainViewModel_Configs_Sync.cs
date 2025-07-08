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
        await UpdateServiceStatus();

        // Get all rpc clients
        var rpcClients = await GetAllRpcClients();
        if (rpcClients.Count == 0)
            return;

        // Get all file info list from all rpc clients
        var clientFileInfoLists = new List<List<ConfigFileInfo>>();

        foreach (var rpcClient in rpcClients)
        {
            clientFileInfoLists.Add(await rpcClient.GetConfigFileInfoList());
        }

        // Get local file info list
        var localFileInfoList = GetConfigFileInfoList();
        var localFileNames = localFileInfoList.Select(fileInfo => fileInfo.FileName).ToHashSet();

        // Delete extra configs on other nodes
        if (DeleteExtraConfigsOnOtherNodesWhenNextSync)
        {
            var fileNamesToDelete = new List<string>();

            for (var i = 0; i < clientFileInfoLists.Count; i++)
            {
                var clientFileInfoList = clientFileInfoLists[i];
                fileNamesToDelete.Clear();

                for (var j = clientFileInfoList.Count - 1; j >= 0; j--)
                {
                    if (!localFileNames.Contains(clientFileInfoList[j].FileName))
                    {
                        fileNamesToDelete.Add(clientFileInfoList[j].FileName);
                        clientFileInfoList.RemoveAt(j);
                    }
                }

                if (fileNamesToDelete.Count > 0)
                    await rpcClients[i].DeleteExtraConfigs(fileNamesToDelete);
            }

            DeleteExtraConfigsOnOtherNodesWhenNextSync = false;
        }

        // Add local file info list to the last of clientFileInfoList, to find latest file later.
        clientFileInfoLists.Add(localFileInfoList);

        // Find latest file info
        var latestFileInfoDict = new Dictionary<string, LatestFileInfo>();

        for (int i = 0; i < clientFileInfoLists.Count; i++)
        {
            foreach (var fileInfo in clientFileInfoLists[i])
            {
                if (latestFileInfoDict.TryGetValue(fileInfo.FileName, out var latestFileInfo))
                {
                    if (latestFileInfo.FileTimeUtc < fileInfo.FileTimeUtc)
                    {
                        latestFileInfo.FileTimeUtc = fileInfo.FileTimeUtc;
                        latestFileInfo.RpcClientIndex = i;
                    }

                    // At least has compared once with different time
                    if (latestFileInfo.FileTimeUtc != fileInfo.FileTimeUtc)
                        latestFileInfo.IsLatest = true;
                }
                else
                {
                    latestFileInfoDict.Add(fileInfo.FileName,
                        new LatestFileInfo
                        {
                            FileName = fileInfo.FileName,
                            FileTimeUtc = fileInfo.FileTimeUtc,
                            IsLatest = false,
                            RpcClientIndex = i
                        });
                }
            }
        }

        // Sync file content
        var latestFileInfoList = latestFileInfoDict.Values.Where(fileInfo => fileInfo.IsLatest).ToList();
        if (latestFileInfoList.Count == 0)
            return;

        var lastestFilesInLocal = latestFileInfoList
            .Where(fileInfo => fileInfo.RpcClientIndex == clientFileInfoLists.Count - 1)
            .Select(fileInfo => fileInfo.FileName)
            .ToList();
        var latestFileContentListInLocal = await GetConfigFileContent(lastestFilesInLocal);

        for (var i = 0; i < rpcClients.Count; i++)
        {
            var rpcClient = rpcClients[i];
            var latestFilesInCurrentRpcClient = latestFileInfoList
                .Where(fileInfo => fileInfo.RpcClientIndex == i)
                .Select(fileInfo => fileInfo.FileName)
                .ToList();

            var fileContentList = await rpcClient.GetConfigFileContent(latestFilesInCurrentRpcClient);
            await WriteConfigFileContent(fileContentList);

            if (latestFileContentListInLocal.Count > 0)
                await rpcClient.SendConfigFileContent(latestFileContentListInLocal);
        }

        // Refresh configs
        await LoadConfigs();
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
    }

    public void DeleteExtraConfigs(List<string> fileNames)
    {
        var configList = Configs.ToList();
        var isSelectedChanged = false;

        foreach (var fileName in fileNames)
        {
            var filePath = Path.Join(AppSettings.ConfigDirectory, fileName);
            if (File.Exists(filePath))
                File.Delete(filePath);

            var configName = Path.GetFileNameWithoutExtension(fileName);
            var configIndex = configList.FindIndex(config => config.Name == configName);
            if (configIndex != -1)
            {
                if (configList[configIndex].IsSelected)
                    isSelectedChanged = true;

                Configs.RemoveAt(configIndex);
            }
        }

        if (isSelectedChanged)
        {
            UpdateHasSelectedConfigs();
        }
    }

}