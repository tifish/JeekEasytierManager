using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using JeekTools;

namespace JeekEasyTierManager;

/// <summary>
/// Machine-specific settings that never roam. Stored under %LocalAppData% so it can be read before
/// the roaming location is resolved. Currently holds only the storage-location choice, but it is
/// the home for any future machine-bound settings (e.g. window geometry).
/// </summary>
public class LocalSettings
{
    public StorageLocation StorageMode { get; set; } = StorageLocation.UserDirectory;
    public string CustomDirectory { get; set; } = "";

    public static LocalSettings Current { get; private set; } = new();

    private static LocalSettings _baseline = new();

    public static async Task Load()
    {
        await Task.Run(() =>
        {
            if (JsonSettingsFile.TryLoad(StorageManager.LocalSettingsFile, out LocalSettings loaded))
            {
                Current = loaded;
                _baseline = JsonSettingsFile.Clone(loaded);
            }
        });
    }

    public static async Task Save()
    {
        if (Design.IsDesignMode)
            return;

        await Task.Run(() =>
        {
            Directory.CreateDirectory(StorageManager.LocalConfigDirectory);
            if (
                JsonSettingsFile.TryMergeAndWrite(
                    StorageManager.LocalSettingsFile,
                    _baseline,
                    Current,
                    static _ => { },
                    forceAllLocal: false,
                    out var merged
                )
            )
            {
                Current = merged;
                _baseline = JsonSettingsFile.Clone(merged);
            }
        });
    }
}
