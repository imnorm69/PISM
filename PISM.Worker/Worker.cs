using PISM.Core.Models;
using PISM.Core.Services;

namespace PISM.Worker;

// Standalone runner for the scanner (no SignalR). Primary deployment runs
// ScanBackgroundService inside PISM.Web which has direct hub access.
public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
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

                var progress = new Progress<ScanProgressUpdate>(u =>
                    _logger.LogInformation("[{Job}] {Processed}/{Total} — {File}",
                        u.JobId, u.ProcessedFiles, u.TotalFiles, u.CurrentFileName));

                await scanner.ProcessNextPendingJobAsync(progress, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Scan error"); }

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
