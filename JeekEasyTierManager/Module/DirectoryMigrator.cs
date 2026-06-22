using System;
using System.IO;
using System.Threading;

namespace JeekEasyTierManager;

/// <summary>
/// Moves the contents of one directory into another. Used to relocate roaming data when the
/// storage mode changes.
/// </summary>
public static class DirectoryMigrator
{
    /// <summary>
    /// Copies <paramref name="source"/> into <paramref name="target"/> and then deletes the source.
    /// Copy-then-delete (never delete-first) means an interruption leaves the source intact, so no
    /// data is lost. Works across volumes and merges into a non-empty target (source wins on conflict).
    /// </summary>
    public static void MoveMerge(string source, string target)
    {
        source = Path.GetFullPath(source);
        target = Path.GetFullPath(target);

        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
            return;
        if (!Directory.Exists(source))
            return;

        CopyMerge(source, target);
        DeleteWithRetry(source);
    }

    public static void CopyMerge(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, dir)));

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destFile = Path.Combine(target, Path.GetRelativePath(source, file));
            Retry(() => File.Copy(file, destFile, overwrite: true));
        }
    }

    private static void DeleteWithRetry(string directory)
    {
        Retry(() =>
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        });
    }

    // EasyTier service processes can briefly keep a file handle open right after being stopped,
    // so retry transient IO/permission errors before giving up.
    private static void Retry(Action action)
    {
        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (Exception ex)
                when ((ex is IOException or UnauthorizedAccessException) && attempt < maxAttempts)
            {
                Thread.Sleep(200);
            }
        }
    }
}
