using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MsBox.Avalonia;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace JeekEasytierManager;

class Program
{
    private static Mutex? _mutex;
    private const string MutexName = "JeekEasytierManager_SingleInstance_Mutex";
    private const string PipeName = "JeekEasytierManager_IPC_Pipe";
    private const string ShowWindowMessage = "SHOW_WINDOW";

    private static NamedPipeServerStream? _pipeServer;
    private static CancellationTokenSource? _cancellationTokenSource;

    // Property to track if the application should start hidden
    public static bool StartHidden { get; private set; } = false;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Check if another instance is already running
        _mutex = new Mutex(true, MutexName, out var createdNew);

        if (!createdNew)
        {
            // Another instance is already running, send message to show window
            SendShowWindowMessage();
            _mutex.Dispose();
            return;
        }

        try
        {
            // Start the IPC server to listen for show window messages
            StartIPCServer();

            // Check if the application should start hidden
            if (args.Length > 0 && args[0] == "/hide")
            {
                StartHidden = true;
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            // Clean up
            _cancellationTokenSource?.Cancel();
            _pipeServer?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }

    private static void SendShowWindowMessage()
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            pipeClient.Connect(1000); // Wait up to 1 second
            using var writer = new StreamWriter(pipeClient);
            writer.WriteLine(ShowWindowMessage);
            writer.Flush();
        }
        catch (Exception ex)
        {
            // If pipe communication fails, fallback to window enumeration
            System.Diagnostics.Debug.WriteLine($"Failed to send IPC message: {ex.Message}");
        }
    }

    private static void StartIPCServer()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    _pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    await _pipeServer.WaitForConnectionAsync(_cancellationTokenSource.Token);

                    using var reader = new StreamReader(_pipeServer);
                    var message = await reader.ReadLineAsync();

                    if (message == ShowWindowMessage)
                    {
                        // Use Avalonia dispatcher to show window on UI thread
                        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                App.ToggleMainWindow();
                                // Ensure the window is shown and brought to front
                                if (App.MainWindow != null && !App.MainWindow.IsVisible)
                                {
                                    App.MainWindow.Show();
                                    App.MainWindow.Activate();
                                }
                            });
                        }
                    }

                    _pipeServer.Disconnect();
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"IPC Server error: {ex.Message}");
                    await Task.Delay(1000, _cancellationTokenSource.Token); // Wait before retrying
                }
                finally
                {
                    _pipeServer?.Dispose();
                    _pipeServer = null;
                }
            }
        }, _cancellationTokenSource.Token);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
