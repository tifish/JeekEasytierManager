using System;
using System.IO;

namespace JeekEasytierManager;

public static class Settings
{
    public static readonly string AppDirectory = AppContext.BaseDirectory;
    public static readonly string EasytierDirectory = Path.Join(AppDirectory, "Easytier");
    public static readonly string ConfigDirectory = Path.Join(AppDirectory, "Config");

    public static readonly string EasytierCorePath = Path.Join(EasytierDirectory, "easytier-core.exe");
    public static readonly string EasytierCliPath = Path.Join(EasytierDirectory, "easytier-cli.exe");
    public static readonly string NssmPath = Path.Join(AppDirectory, "Nssm", "nssm.exe");
}