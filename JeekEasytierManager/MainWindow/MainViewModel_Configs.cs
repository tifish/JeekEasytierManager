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

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private async Task LoadConfigs(bool isInitial)
    {
        if (!Directory.Exists(AppSettings.ConfigDirectory))
            return;

        // Save selected config
        var selectedConfigNames = Configs.Where(c => c.IsSelected).Select(c => c.Name).ToList();

        // Remove property change listeners from existing configs
        foreach (var config in Configs)
        {
            config.PropertyChanged -= OnConfigPropertyChanged;
        }

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
            await LoadInstalledServices();
            await UpdateServiceStatus();
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
                    config.PropertyChanged += OnConfigPropertyChanged;
                    Configs.Add(config);
                }
            }

            // Update service status
            await LoadInstalledServices(newConfigs);
            await UpdateServiceStatus(newConfigs);

            if (isInitial)
            {
                // Select installed configs
                foreach (var config in newConfigs)
                {
                    if (config.Status != ServiceStatus.None)
                        config.IsSelected = true;
                }
            }
            else
            {
                // Restore selected config
                foreach (var configName in selectedConfigNames)
                {
                    var config = Configs.FirstOrDefault(c => c.Name == configName);
                    if (config != null)
                        config.IsSelected = true;
                }
            }

            // Update Configs at once, to avoid unnecessary Status changes on UI.
            if (!isInitial)
            {
                Configs.Clear();
                foreach (var config in newConfigs)
                {
                    config.PropertyChanged += OnConfigPropertyChanged;
                    Configs.Add(config);
                }
            }

            UpdateHasSelectedConfigs();
        }
    }

    [RelayCommand]
    public void EditConfig(ConfigInfo config)
    {
        foreach (var c in Configs)
        {
            c.IsSelected = c == config;
        }

        EditSelectedConfigs();
    }

    [RelayCommand]
    public void EditConfigFile(ConfigInfo config)
    {
        var configFile = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
        if (!File.Exists(configFile))
            return;

        Executor.Open(configFile);
    }

    [ObservableProperty]
    public partial bool RenameConfigDialogIsOpen { get; set; } = false;

    [ObservableProperty]
    public partial string RenameConfigDialogText { get; set; } = "";

    private ConfigInfo? _renameConfigDialogOldConfig = null;

    [RelayCommand]
    public void RenameConfig(ConfigInfo config)
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

            // Notify that HasSelectedConfigs might have changed
            OnPropertyChanged(nameof(HasSelectedConfigs));
            EditSelectedConfigsCommand.NotifyCanExecuteChanged();
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
        config.PropertyChanged += OnConfigPropertyChanged;
        Configs.Add(config);
    }

    [RelayCommand]
    public async Task DeleteConfig(ConfigInfo config)
    {
        var result = await MessageBoxManager.GetMessageBoxStandard("Delete Config", "Are you sure you want to delete this config?", ButtonEnum.YesNo).ShowAsync();
        if (result != ButtonResult.Yes)
            return;

        var configFile = Path.Combine(AppSettings.ConfigDirectory, config.Name + ".toml");
        if (File.Exists(configFile))
            File.Delete(configFile);

        // Remove property change listener before removing from collection
        config.PropertyChanged -= OnConfigPropertyChanged;
        Configs.Remove(config);

        UpdateHasSelectedConfigs();
    }

    [RelayCommand]
    public async Task RefreshConfigs()
    {
        // Must run on UI thread
        await LoadConfigs(false);
    }

    [ObservableProperty]
    public partial bool IsEditingConfigs { get; set; } = false;
    public Grid MainGrid { get; internal set; } = null!;

    [ObservableProperty]
    public partial bool HasSelectedConfigs { get; set; }

    private void UpdateHasSelectedConfigs()
    {
        HasSelectedConfigs = Configs.Any(c => c.IsSelected);
    }

    // Handle property changes in ConfigInfo objects
    private void OnConfigPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConfigInfo.IsSelected))
        {
            UpdateHasSelectedConfigs();
        }
    }

    [RelayCommand]
    public void EditSelectedConfigs()
    {
        if (Configs.ToArray().All(c => !c.IsSelected))
            return;

        MainGrid.RowDefinitions[0].SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
        MainGrid.RowDefinitions[1].SetCurrentValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Auto));
        IsEditingConfigs = true;

        var isSingleConfig = Configs.ToArray().Count(c => c.IsSelected) == 1;
        EditIpAddress = isSingleConfig;
        EditPeers = isSingleConfig;
        EditListeners = isSingleConfig;
        EditRpcPortal = isSingleConfig;
        EditProxyNetworks = isSingleConfig;

        LoadConfig(Configs.First(c => c.IsSelected).Name);
    }

    [RelayCommand]
    public void AddConfig()
    {
        AddConfigDialogIsOpen = true;
    }

}