using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using JeekTools;

namespace JeekEasytierManager;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private void LoadConfigs()
    {
        if (!Directory.Exists(AppSettings.ConfigDirectory))
            return;

        // 获取配置文件列表
        var configFiles = Directory.GetFiles(AppSettings.ConfigDirectory, "*.toml");
        foreach (var configFile in configFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(configFile);
            Configs.Add(new ConfigInfo { Name = fileName });
        }
    }

    [RelayCommand]
    public void EditConfig(ConfigInfo config)
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
        Configs.Add(new ConfigInfo { Name = newName });
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

        Configs.Remove(config);
    }

    [RelayCommand]
    public void EditSelectedConfig()
    {
    }

    [RelayCommand]
    public void AddConfig()
    {
        AddConfigDialogIsOpen = true;
    }

}