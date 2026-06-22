using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Json.Easy;

namespace JeekEasyTierManager;

/// <summary>
/// Machine-specific settings that never roam. Stored under %LocalAppData% so it can be read before
/// the roaming location is resolved. Currently holds only the storage-mode choice, but it is the
/// home for any future machine-bound settings (e.g. window geometry).
/// </summary>
public class LocalSettings
{
    public StorageMode StorageMode { get; set; } = StorageMode.Default;
    public string CustomDirectory { get; set; } = "";

    public static LocalSettings Current { get; private set; } = new();

    public static async Task Load()
    {
        var loaded = await new JsonFile(StorageManager.LocalSettingsFile).Load<LocalSettings>();
        if (loaded != null)
            Current = loaded;
    }

    public static async Task Save()
    {
        if (Design.IsDesignMode)
            return;

        Directory.CreateDirectory(StorageManager.LocalConfigDirectory);
        await new JsonFile(StorageManager.LocalSettingsFile).Save(Current);
    }
}
