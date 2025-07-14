using System.ServiceProcess;
using System.Threading.Tasks;

namespace JeekEasyTierManager;

public static class ServiceControllerExtension
{
    private const int WaitInterval = 100;
    private const int TimeoutMilliseconds = 3000;

    public static async Task<bool> WaitForStatusAsync(this ServiceController service, ServiceControllerStatus status)
    {
        var startTime = System.Environment.TickCount;
        service.Refresh();

        while (service.Status != status)
        {
            await Task.Delay(WaitInterval);

            if (System.Environment.TickCount - startTime > TimeoutMilliseconds)
            {
                return false;
            }

            service.Refresh();
        }

        return true;
    }

    public static async Task<bool> StartAsync(this ServiceController service)
    {
        service.Refresh();
        if (service.Status == ServiceControllerStatus.Running)
            return true;

        service.Start();
        return await service.WaitForStatusAsync(ServiceControllerStatus.Running);
    }

    public static async Task<bool> StopAsync(this ServiceController service)
    {
        service.Refresh();
        if (service.Status == ServiceControllerStatus.Stopped)
            return true;

        service.Stop();
        return await service.WaitForStatusAsync(ServiceControllerStatus.Stopped);
    }
}