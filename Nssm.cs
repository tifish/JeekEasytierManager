using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace JeekEasytierManager;

public static class Nssm
{
    public static async Task<string> RunWithOutput(string fileName, string arguments, Encoding? encoding = null)
    {
        var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = encoding ?? Encoding.Default,
        });
        if (process is null)
            return "";
        return await process.StandardOutput.ReadToEndAsync();
    }

    private static Process? CallNssm(string command)
    {
        return Process.Start(new ProcessStartInfo(Settings.NssmPath, command)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
    }

    private static async Task<bool> CallNssmAndWait(string command)
    {
        var process = CallNssm(command);
        if (process is null)
            return false;
        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }

    public static async Task<bool> InstallService(string serviceName, string fileName, string arguments)
    {
        return await CallNssmAndWait($"""install "{serviceName}" "{fileName}" {arguments}""");
    }

    public static async Task<bool> UninstallService(string serviceName)
    {
        return await CallNssmAndWait($"""remove "{serviceName}" confirm""");
    }

    public static async Task<bool> StartService(string serviceName)
    {
        return await CallNssmAndWait($"""start "{serviceName}""");
    }

    public static async Task<bool> StopService(string serviceName)
    {
        return await CallNssmAndWait($"""stop "{serviceName}""");
    }

    public static async Task<bool> RestartService(string serviceName)
    {
        return await CallNssmAndWait($"""restart "{serviceName}""");
    }

    public static async Task<ServiceStatus> GetServiceStatus(string serviceName)
    {
        var statusString = (await RunWithOutput(Settings.NssmPath, $"""status "{serviceName}" """)).Trim();
        return statusString switch
        {
            "" => ServiceStatus.None,
            "SERVICE_STOPPED" or "SERVICE_PAUSED" or "SERVICE_STOP_PENDING" => ServiceStatus.Stopped,
            _ => ServiceStatus.Running,
        };
    }

}

public enum ServiceStatus
{
    None,
    Stopped,
    Running,
}

