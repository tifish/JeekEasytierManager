using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using JeekTools;
using System.Linq;
using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Nett;
using System.ServiceProcess;

namespace JeekEasyTierManager;

public class EasyTierConfig
{
    [DataMember(Name = "flags")]
    public EasyTierConfigFlags? Flags { get; set; } = new();
}

public class EasyTierConfigFlags
{
    [DataMember(Name = "dev_name")]
    public string? DevName { get; set; } = "";

    [DataMember(Name = "no_tun")]
    public bool NoTun { get; set; } = false;
}

public partial class ConfigInfo : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial ServiceStatus Status { get; set; } = ServiceStatus.None;

    public string GetConfigPath()
    {
        return Path.Join(AppSettings.ConfigDirectory, Name + ".toml");
    }

    public EasyTierConfig? GetConfig()
    {
        try
        {
            return Toml.ReadFile<EasyTierConfig>(GetConfigPath());
        }
        catch
        {
            return null;
        }
    }

    public ServiceController? Service { get; set; }
}


public partial class MainViewModel : ObservableObject, IDisposable
{
    private void LoadConfigs(bool isInitial)
    {
        if (!Directory.Exists(AppSettings.ConfigDirectory))
            return;

        // Save selected config
        var selectedConfigNames = SelectedConfigs.Select(c => c.Name).ToList();

        // Get config files
        var configNames = new List<string>();
        var configFiles = Directory.GetFiles(AppSettings.ConfigDirectory, "*.toml");

        foreach (var configFile in configFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(configFile);
            configNames.Add(fileName);
        }

        // If configs are the same, only update service status
        if (configNames.Count == Configs.Count
            && configNames.All(c => Configs.Any(c2 => c2.Name == c)))
        {
            LoadInstalledServices();
            UpdateAllServicesStatus();
        }
        else
        {
            var newConfigs = new List<ConfigInfo>();

            // Add new configs
            foreach (var configName in configNames)
            {
                var config = new ConfigInfo { Name = configName };
                newConfigs.Add(config);
            }

            // Update as soon as possible when initial loading
            if (isInitial)
            {
                Configs.Clear();
                foreach (var config in newConfigs)
                {
                    Configs.Add(config);
                }
            }

            // Update service status
            LoadInstalledServices(newConfigs);
            UpdateAllServicesStatus(newConfigs);

            // Update Configs at once, to avoid unnecessary Status changes on UI.
            if (!isInitial)
            {
                Configs.Clear();
                foreach (var config in newConfigs)
                {
                    Configs.Add(config);
                }
            }

            if (isInitial)
            {
                // Select installed configs
                foreach (var config in newConfigs)
                {
                    if (config.Status != ServiceStatus.None)
                        AddSelectedConfig(config);
                }
            }
            else
            {
                // Restore selected config
                foreach (var configName in selectedConfigNames)
                {
                    var config = Configs.FirstOrDefault(c => c.Name == configName);
                    if (config != null)
                        AddSelectedConfig(config);
                }
            }
        }
    }

    [RelayCommand]
    public void EditSingleConfig(ConfigInfo config)
    {
        EditConfigs(config);
    }

    [RelayCommand]
    public async Task TestSingleConfig(ConfigInfo config)
    {
        var configFile = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
        if (!File.Exists(configFile))
            return;

        // Run cmd file in temp directory
        var cmdText = $"""
            {AppSettings.EasyTierCorePath} -c "{configFile}"
            pause
        """;
        var cmdFile = Path.GetTempFileName() + ".cmd";
        File.WriteAllText(cmdFile, cmdText);

        using var process = Executor.Run(cmdFile);
        if (process is null)
        {
            Messages = $"Failed to test config: {config.Name}";
            return;
        }

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            Messages = $"Failed to test config: {config.Name}";
        }
    }

    [RelayCommand]
    public void EditSingleConfigFile(ConfigInfo config)
    {
        var configFile = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
        if (!File.Exists(configFile))
            return;

        Executor.Open(configFile);
    }

    const string MultipleConfigInstanceName = "（选中的配置）";

    [RelayCommand]
    public void EditSelectedConfigs()
    {
        if (SelectedConfigs.Count == 0)
            return;

        EditConfigs(null);

        InstanceName = MultipleConfigInstanceName;
        FileLoggerName = MultipleConfigInstanceName;
    }

    public void EditConfigs(ConfigInfo? config)
    {
        MainGrid.RowDefinitions[0].SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
        MainGrid.RowDefinitions[1].SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Auto));
        IsEditingConfigs = true;

        var isSingleConfig = config != null;

        EditIpAddress = isSingleConfig;
        EditPeers = isSingleConfig;
        EditListeners = isSingleConfig;
        EditRpcPortal = isSingleConfig;
        EditProxyNetworks = isSingleConfig;
        EditFileLogger = isSingleConfig;

        if (config != null)
            LoadConfig(config.Name);
        else
            LoadConfig(SelectedConfigs.First().Name);
    }

    [ObservableProperty]
    public partial bool RenameConfigDialogIsOpen { get; set; } = false;

    [ObservableProperty]
    public partial string RenameConfigDialogText { get; set; } = "";

    private ConfigInfo? _renameConfigDialogOldConfig = null;

    [RelayCommand]
    public void RenameSingleConfig(ConfigInfo config)
    {
        RenameConfigDialogIsOpen = true;
        RenameConfigDialogText = config.Name;
        _renameConfigDialogOldConfig = config;
    }

    [RelayCommand]
    public void RenameConfigDialogCancel()
    {
        RenameConfigDialogIsOpen = false;
    }

    [RelayCommand]
    public void RenameConfigDialogSave()
    {
        RenameConfigDialogIsOpen = false;

        if (_renameConfigDialogOldConfig is null)
            return;

        var newName = RenameConfigDialogText;

        if (string.IsNullOrWhiteSpace(newName) || newName == _renameConfigDialogOldConfig.Name)
            return;

        var oldConfigFile = Path.Combine(AppSettings.ConfigDirectory, _renameConfigDialogOldConfig.Name + ".toml");
        var newConfigFile = Path.Combine(AppSettings.ConfigDirectory, newName + ".toml");

        if (File.Exists(newConfigFile))
        {
            Messages = $"Config file '{newName}.toml' already exists.";
            return;
        }

        try
        {
            File.Move(oldConfigFile, newConfigFile);

            _renameConfigDialogOldConfig.Name = newName;
            _renameConfigDialogOldConfig = null;
        }
        catch (Exception ex)
        {
            Messages = $"Failed to rename config: {ex.Message}";
        }
    }

    [ObservableProperty]
    public partial bool AddConfigDialogIsOpen { get; set; } = false;

    [ObservableProperty]
    public partial string AddConfigDialogText { get; set; } = "";

    [RelayCommand]
    public void AddConfigDialogCancel()
    {
        AddConfigDialogIsOpen = false;
    }

    [RelayCommand]
    public void AddConfigDialogAdd()
    {
        AddConfigDialogIsOpen = false;

        var newName = AddConfigDialogText;
        if (string.IsNullOrWhiteSpace(newName))
            return;

        var configFile = Path.Combine(AppSettings.ConfigDirectory, newName + ".toml");
        if (File.Exists(configFile))
            return;

        File.Create(configFile).Close();
        var config = new ConfigInfo { Name = newName };
        Configs.Add(config);
    }

    [RelayCommand]
    public async Task DeleteSingleConfig(ConfigInfo config)
    {
        var result = await MessageBoxManager.GetMessageBoxStandard(
            "Delete Config", "Are you sure you want to delete this config?",
            ButtonEnum.YesNo, Icon.Question)
            .ShowWindowDialogAsync(_mainWindow!);
        if (result != ButtonResult.Yes)
            return;

        DeleteConfig(config);
    }

    [RelayCommand]
    public async Task DeleteSelectedConfigs()
    {
        if (SelectedConfigs.Count == 0)
            return;

        var result = await MessageBoxManager.GetMessageBoxStandard(
            "Delete Selected Configs", "Are you sure you want to delete selected configs?",
            ButtonEnum.YesNo, Icon.Question)
            .ShowWindowDialogAsync(_mainWindow!);
        if (result != ButtonResult.Yes)
            return;

        DeleteConfigs(null);
    }

    private void DeleteConfigs(ConfigInfo? config)
    {
        if (config != null)
        {
            DeleteConfig(config);
            return;
        }

        foreach (var aConfig in SelectedConfigs)
            DeleteConfig(aConfig);
    }

    private void DeleteConfig(ConfigInfo config)
    {
        var configFile = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
        if (File.Exists(configFile))
            File.Delete(configFile);

        Configs.Remove(config);
    }

    [RelayCommand]
    public void RefreshConfigs()
    {
        // Must run on UI thread
        LoadConfigs(false);
    }

    [ObservableProperty]
    public partial bool IsEditingConfigs { get; set; } = false;
    public Grid MainGrid { get; internal set; } = null!;

    private MainWindowConfigs? _mainWindowConfigs;

    public void SetMainWindowConfigs(MainWindowConfigs mainWindowConfigs)
    {
        _mainWindowConfigs = mainWindowConfigs;
    }

    public ObservableCollection<ConfigInfo> SelectedConfigs { get; set; } = [];

    private void AddSelectedConfig(ConfigInfo config)
    {
        if (!SelectedConfigs.Contains(config))
        {
            SelectedConfigs.Add(config);
            _mainWindowConfigs?.UpdateDataGridSelection();
        }
    }

    [RelayCommand]
    public void AddConfig()
    {
        AddConfigDialogIsOpen = true;
    }

}
