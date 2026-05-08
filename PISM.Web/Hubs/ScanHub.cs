using Microsoft.AspNetCore.SignalR;
using PISM.Core.Models;

namespace PISM.Web.Hubs;

public interface IScanHubClient
{
    Task ScanProgressUpdated(ScanProgressUpdate update);
    Task ScanJobCompleted(Guid jobId);
    Task ScanJobFailed(Guid jobId, string errorMessage);
}

public class ScanHub : Hub<IScanHubClient>
{
    public const string Url = "/hubs/scan";
}
