using Microsoft.AspNetCore.SignalR;
using PISM.Core.Models;
using PISM.Core.Services;
using PISM.Web.Hubs;

namespace PISM.Web.Services;

public class ScanBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ScanHub, IScanHubClient> _hub;
    private readonly ILogger<ScanBackgroundService> _logger;

    public ScanBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<ScanHub, IScanHubClient> hub,
        ILogger<ScanBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Reset any jobs that were mid-scan when the app last stopped
        using (var scope = _scopeFactory.CreateScope())
        {
            var scanner = scope.ServiceProvider.GetRequiredService<IScannerService>();
            await scanner.ResetStuckJobsAsync();
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scanner = scope.ServiceProvider.GetRequiredService<IScannerService>();

                var progress = new Progress<ScanProgressUpdate>(update =>
                {
                    if (update.Status == Core.Enums.ScanJobStatus.Completed)
                        _ = _hub.Clients.All.ScanJobCompleted(update.JobId);
                    else if (update.Status == Core.Enums.ScanJobStatus.Failed)
                        _ = _hub.Clients.All.ScanJobFailed(update.JobId, string.Empty);
                    else
                        _ = _hub.Clients.All.ScanProgressUpdated(update);
                });

                await scanner.ProcessNextPendingJobAsync(progress, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in scan background service");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
