using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceProcess;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jeek.Avalonia.Localization;
using JeekTools;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Nett;

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
    public partial bool IsSelected { get; set; } = false;

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
        if (
            configNames.Count == Configs.Count
            && configNames.All(c => Configs.Any(c2 => c2.Name == c))
        )
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
            ClearMessages();
            AddMessage(string.Format(Localizer.Get("Configs_TestConfigFailed"), config.Name));
            return;
        }

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            ClearMessages();
            AddMessage(string.Format(Localizer.Get("Configs_TestConfigFailed"), config.Name));
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

    private static string MultipleConfigInstanceName =>
        Localizer.Get("Configs_MultipleConfigInstanceName");

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
        MainGrid
            .RowDefinitions[0]
            .SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
        MainGrid
            .RowDefinitions[1]
            .SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Auto));
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

        var oldConfigFile = Path.Combine(
            AppSettings.ConfigDirectory,
            _renameConfigDialogOldConfig.Name + ".toml"
        );
        var newConfigFile = Path.Combine(AppSettings.ConfigDirectory, newName + ".toml");

        if (File.Exists(newConfigFile))
        {
            ClearMessages();
            AddMessage(string.Format(Localizer.Get("Configs_ConfigFileAlreadyExists"), newName));
            return;
        }

        try
        {
            StorageManager.TouchSelfWrite();
            File.Move(oldConfigFile, newConfigFile);

            _renameConfigDialogOldConfig.Name = newName;
            _renameConfigDialogOldConfig = null;
        }
        catch (Exception ex)
        {
            ClearMessages();
            AddMessage(string.Format(Localizer.Get("Configs_RenameConfigFailed"), ex.Message));
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

        StorageManager.TouchSelfWrite();
        File.Create(configFile).Close();
        var config = new ConfigInfo { Name = newName };
        Configs.Add(config);
    }

    [RelayCommand]
    public async Task DeleteSingleConfig(ConfigInfo config)
    {
        var result = await MessageBoxManager
            .GetMessageBoxStandard(
                Localizer.Get("Configs_DeleteConfigTitle"),
                Localizer.Get("Configs_DeleteConfigConfirm"),
                ButtonEnum.YesNo,
                Icon.Question
            )
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

        var result = await MessageBoxManager
            .GetMessageBoxStandard(
                Localizer.Get("Configs_DeleteSelectedConfigsTitle"),
                Localizer.Get("Configs_DeleteSelectedConfigsConfirm"),
                ButtonEnum.YesNo,
                Icon.Question
            )
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

        foreach (var aConfig in SelectedConfigs.ToArray())
            DeleteConfig(aConfig);
    }

    private void DeleteConfig(ConfigInfo config)
    {
        var configFile = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
        if (File.Exists(configFile))
        {
            StorageManager.TouchSelfWrite();
            File.Delete(configFile);
        }

        Configs.Remove(config);
        SelectedConfigs.Remove(config);
        config.IsSelected = false;
        UpdateSelectedConfigsState();
        _mainWindowConfigs?.UpdateDataGridSelection();
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

    [ObservableProperty]
    public partial bool HasMultipleSelectedConfigs { get; set; } = false;

    [ObservableProperty]
    public partial string SelectedConfigsSummary { get; set; } = "";

    public void SetSelectedConfigs(IEnumerable<ConfigInfo> configs, bool updateDataGridSelection = true)
    {
        var selectedConfigs = configs.Distinct().ToList();

        SelectedConfigs.Clear();
        foreach (var config in selectedConfigs)
        {
            SelectedConfigs.Add(config);
        }

        foreach (var config in Configs)
        {
            var isSelected = selectedConfigs.Contains(config);
            if (config.IsSelected != isSelected)
                config.IsSelected = isSelected;
        }

        UpdateSelectedConfigsState();

        if (updateDataGridSelection)
            _mainWindowConfigs?.UpdateDataGridSelection();
    }

    public void SetConfigSelected(ConfigInfo config, bool isSelected)
    {
        if (isSelected)
        {
            AddSelectedConfig(config);
            return;
        }

        SelectedConfigs.Remove(config);
        if (config.IsSelected)
            config.IsSelected = false;

        UpdateSelectedConfigsState();
        _mainWindowConfigs?.UpdateDataGridSelection();
    }

    private void AddSelectedConfig(ConfigInfo config)
    {
        if (!SelectedConfigs.Contains(config))
        {
            SelectedConfigs.Add(config);
        }

        if (!config.IsSelected)
            config.IsSelected = true;

        UpdateSelectedConfigsState();
        _mainWindowConfigs?.UpdateDataGridSelection();
    }

    private void UpdateSelectedConfigsState()
    {
        HasMultipleSelectedConfigs = SelectedConfigs.Count >= 2;
        SelectedConfigsSummary = string.Format(
            Localizer.Get("Configs_SelectedConfigsSummary"),
            SelectedConfigs.Count
        );
    }

    [RelayCommand]
    public void AddConfig()
    {
        AddConfigDialogIsOpen = true;
    }
}
