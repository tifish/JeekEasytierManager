using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using JeekTools;
using System;
using System.Text;

namespace JeekEasyTierManager;

class Program
{


    // Property to track if the application should start hidden
    public static bool StartHidden { get; private set; } = false;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        using var singleInstance = new SingleInstance("JeekEasyTierManager");
        if (singleInstance.IsRunning)
            return;

        singleInstance.StartIPCServer(async () =>
        {
            // Use Avalonia dispatcher to show window on UI thread
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Ensure the window is shown and brought to front
                    if (App.MainWindow != null)
                    {
                        App.MainWindow.Show();
                        App.MainWindow.Activate();
                        App.MainWindow.BringIntoView();
                    }
                });
            }
        });

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Start sync server
        RemoteCall.StartServer("http://0.0.0.0:16666");

        // Check if the application should start hidden
        if (args.Length > 0 && args[0] == "/hide")
        {
            StartHidden = true;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
