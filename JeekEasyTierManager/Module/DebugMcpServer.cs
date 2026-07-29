using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using JeekTools;

namespace JeekEasyTierManager;

/// <summary>
/// App-specific configuration over the generic <see cref="McpHost"/> in JeekTools: object-graph
/// roots (App/Desktop/MainWindow/MainVm), '#Name' visual-tree lookup, and the Avalonia tools
/// (visual_tree, screenshot). Compiled into all configurations, but the listener only starts in
/// Debug builds. Agents reach it through <c>bin\JeekEasyTierManagerMcp.exe --surface debug</c>,
/// which forwards stdio to this instance's named pipe — the pipe name carries the worktree's
/// instance id, so parallel Debug builds never answer for each other.
/// </summary>
public static class DebugMcpServer
{
    // Runtime gate instead of #if DEBUG around the whole file: the code compiles in every
    // configuration, only Debug builds actually listen.
    private static readonly bool ListeningEnabled =
#if DEBUG
        true;
#else
        false;
#endif

    private static readonly ObjectGraph Graph = new(
        new ObjectGraphOptions
        {
            ResolveRoot = ResolveRoot,
            RootNamesHelp = "App, Desktop, MainWindow, MainVm",
            FindNamedChild = (target, name) =>
                target is Visual visual
                    ? FindDescendantByName(visual, name)
                    : throw new InvalidOperationException(
                        $"'#{name}' requires a Visual; {target.GetType().Name} is not one."
                    ),
        }
    );

    private static readonly McpHost Host = CreateHost();

    public static void Start() => Host.Start();

    public static void Stop() => Host.Stop();

    private static McpHost CreateHost()
    {
        var host = new McpHost(
            new McpHostOptions
            {
                ServerName = "jeek-easytier-manager-debug",
                ServerTitle = "JeekEasyTierManager Debug Server",
                Graph = Graph,
                GetVersion = () => $"{AutoUpdate.GetLocalCommitCount()}",
                Enabled = ListeningEnabled,
                // Named pipe only: no port to collide over between worktree instances.
                PipeName = McpPipeNames.Debug(McpPipeNames.InstanceId(AppContext.BaseDirectory)),
                DefaultPort = 0,
                UiInvoker = func =>
                    Dispatcher
                        .UIThread.InvokeAsync(func)
                        .GetTask()
                        .WaitAsync(TimeSpan.FromSeconds(15)),
                Describe = BuildDescribeText,
                ToolListProvider = DebugMcpContract.BuildToolList,
            }
        );

        host.AddTool("visual_tree", VisualTreeAsync);
        host.AddTool("screenshot", _ => ScreenshotAsync());
        return host;
    }

    private static Task<T> OnUiAsync<T>(Func<T> func) => Host.OnUiAsync(func);

    private static IClassicDesktopStyleApplicationLifetime? Desktop =>
        Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    private static object ResolveRoot(string name) =>
        name switch
        {
            "App" => Application.Current
                ?? throw new InvalidOperationException("Application.Current is null."),
            "Desktop" => Desktop ?? throw new InvalidOperationException("No desktop lifetime."),
            "MainWindow" => App.MainWindow
                ?? throw new InvalidOperationException("MainWindow is not created yet."),
            "MainVm" => MainViewModel.Instance,
            _ => throw new InvalidOperationException(
                $"Unknown root '{name}'. Available roots: App, Desktop, MainWindow, MainVm."
            ),
        };

    private static Visual? FindDescendantByName(Visual root, string name)
    {
        var queue = new Queue<Visual>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var visual = queue.Dequeue();
            if (visual is StyledElement styled && styled.Name == name)
                return visual;
            foreach (var child in visual.GetVisualChildren())
                queue.Enqueue(child);
        }

        return null;
    }

    private static string BuildDescribeText()
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"JeekEasyTierManager debug MCP server (build {AutoUpdate.GetLocalCommitCount()})."
        );
        sb.AppendLine($"Pipe: \\\\.\\pipe\\{Host.PipeName}");
        sb.AppendLine($"ProcessId: {Environment.ProcessId}");
        sb.AppendLine($"ExecutablePath: {Environment.ProcessPath}");
        sb.AppendLine($"StorageLocation: {StorageManager.ActiveLocation} ({StorageManager.ActiveRoamingRoot})");
        sb.AppendLine($"Process uptime: {DateTime.Now - Process.GetCurrentProcess().StartTime:hh\\:mm\\:ss}.");
        sb.AppendLine($"Log file: {LogManager.CurrentRollingLogFile}");
        sb.AppendLine();
        sb.AppendLine("Roots for object paths:");
        sb.AppendLine("- App: the Avalonia Application instance");
        sb.AppendLine("- Desktop: the IClassicDesktopStyleApplicationLifetime (Windows list, Shutdown, ...)");
        sb.AppendLine("- MainWindow: the main window");
        sb.AppendLine("- MainVm: MainViewModel.Instance (configs, services, settings)");
        sb.AppendLine();
        sb.AppendLine(DebugMcpContract.PathHelp);
        sb.AppendLine();

        if (Desktop is not { } desktop)
        {
            sb.AppendLine("No desktop lifetime yet.");
        }
        else
        {
            sb.AppendLine($"Windows ({desktop.Windows.Count}):");
            foreach (var window in desktop.Windows)
            {
                sb.AppendLine(
                    $"- {window.GetType().Name} \"{window.Title}\" Visible={window.IsVisible} "
                        + $"State={window.WindowState} ClientSize={window.ClientSize} "
                        + $"DataContext={window.DataContext?.GetType().Name ?? "null"}"
                );
            }
        }

        return sb.ToString();
    }

    #region Avalonia tools

    private const int MaxVisualNodes = 2000;

    private static async Task<JsonObject> VisualTreeAsync(JsonObject args)
    {
        var path = args["path"]?.GetValue<string>() ?? "MainWindow";
        var maxDepth = Math.Max(1, args["max_depth"]?.GetValue<int>() ?? 12);

        var text = await OnUiAsync(() =>
        {
            if (Graph.Resolve(path) is not Visual root)
                throw new InvalidOperationException($"'{path}' is not a Visual.");

            var sb = new StringBuilder();
            var count = 0;
            AppendVisual(sb, root, 0, maxDepth, null, ref count);
            if (count >= MaxVisualNodes)
                sb.AppendLine($"… truncated at {MaxVisualNodes} nodes.");
            return sb.ToString();
        });

        return McpHost.ToolText(text);
    }

    private static void AppendVisual(
        StringBuilder sb,
        Visual visual,
        int depth,
        int maxDepth,
        object? parentDataContext,
        ref int count
    )
    {
        if (count >= MaxVisualNodes)
            return;
        count++;

        sb.Append(' ', depth * 2).Append(visual.GetType().Name);

        var dataContext = parentDataContext;
        if (visual is StyledElement styled)
        {
            if (!string.IsNullOrEmpty(styled.Name))
                sb.Append(" #").Append(styled.Name);
            var classes = string.Join(' ', styled.Classes);
            if (classes.Length > 0)
                sb.Append(" (").Append(classes).Append(')');
            dataContext = styled.DataContext;
            if (dataContext != null && !ReferenceEquals(dataContext, parentDataContext))
                sb.Append(" DataContext=").Append(dataContext.GetType().Name);
        }

        var bounds = visual.Bounds;
        sb.Append($" [{bounds.X:0},{bounds.Y:0} {bounds.Width:0}x{bounds.Height:0}]");
        if (!visual.IsVisible)
            sb.Append(" HIDDEN");

        switch (visual)
        {
            case TextBlock { Text.Length: > 0 } textBlock:
                sb.Append($" Text=\"{ObjectGraph.Truncate(textBlock.Text, 80)}\"");
                break;
            case TextBox { Text.Length: > 0 } textBox:
                sb.Append($" Text=\"{ObjectGraph.Truncate(textBox.Text, 80)}\"");
                break;
        }

        sb.AppendLine();

        if (depth >= maxDepth)
        {
            if (visual.GetVisualChildren().Any())
                sb.Append(' ', (depth + 1) * 2).AppendLine("…");
            return;
        }

        foreach (var child in visual.GetVisualChildren())
            AppendVisual(sb, child, depth + 1, maxDepth, dataContext, ref count);
    }

    private static async Task<JsonObject> ScreenshotAsync()
    {
        var (bytes, pixelSize) = await OnUiAsync(() =>
        {
            var window =
                App.MainWindow
                ?? throw new InvalidOperationException("MainWindow is not created yet.");
            var scaling = window.RenderScaling;
            var size = new PixelSize(
                Math.Max(1, (int)Math.Ceiling(window.ClientSize.Width * scaling)),
                Math.Max(1, (int)Math.Ceiling(window.ClientSize.Height * scaling))
            );

            using var bitmap = new RenderTargetBitmap(size, new Vector(96 * scaling, 96 * scaling));
            bitmap.Render(window);
            using var stream = new MemoryStream();
            bitmap.Save(stream, PngBitmapEncoderOptions.Default);
            return (stream.ToArray(), size);
        });

        return new JsonObject
        {
            ["content"] = new JsonArray(
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = $"Main window screenshot, {pixelSize.Width}x{pixelSize.Height}px.",
                },
                new JsonObject
                {
                    ["type"] = "image",
                    ["data"] = Convert.ToBase64String(bytes),
                    ["mimeType"] = "image/png",
                }
            ),
        };
    }

    #endregion
}
