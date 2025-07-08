using System;
using System.Collections.Generic;
using System.IO;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Server;
using MessagePack;

namespace JeekEasytierManager;

public interface ISyncService : IService<ISyncService>
{
    UnaryResult<bool> Ping();
    UnaryResult<List<ConfigFileInfo>> GetConfigFileInfoList();
    UnaryResult<List<ConfigFileInfo>> GetConfigFileContent(List<string> fileNames);
    UnaryResult SendConfigFileContent(List<ConfigFileInfo> fileContentList);
    UnaryResult DeleteExtraConfigs(List<string> fileNames);
}

[MessagePackObject]
public class ConfigFileInfo
{
    [Key(0)]
    public string FileName { get; set; } = "";

    [Key(1)]
    public DateTime FileTimeUtc { get; set; }

    [Key(2)]
    public string Content { get; set; } = "";
}

public class SyncService : ServiceBase<ISyncService>, ISyncService
{
    protected void EnsureAuthorized()
    {
        var token = Context.CallContext.RequestHeaders.GetValue("authorization");
        if (string.IsNullOrEmpty(token) || token != Settings.SyncPassword)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthorized"));
        }
    }

    public UnaryResult<bool> Ping()
    {
        EnsureAuthorized();
        return UnaryResult.FromResult(true);
    }

    public UnaryResult<List<ConfigFileInfo>> GetConfigFileInfoList()
    {
        EnsureAuthorized();
        return UnaryResult.FromResult(MainViewModel.Instance.GetConfigFileInfoList());
    }

    public async UnaryResult<List<ConfigFileInfo>> GetConfigFileContent(List<string> fileNames)
    {
        EnsureAuthorized();
        return await MainViewModel.Instance.GetConfigFileContent(fileNames);
    }

    public async UnaryResult SendConfigFileContent(List<ConfigFileInfo> fileContentList)
    {
        EnsureAuthorized();
        await MainViewModel.Instance.WriteConfigFileContent(fileContentList);
    }

    public UnaryResult DeleteExtraConfigs(List<string> fileNames)
    {
        EnsureAuthorized();
        MainViewModel.Instance.DeleteExtraConfigs(fileNames);
        return UnaryResult.CompletedResult;
    }
}
