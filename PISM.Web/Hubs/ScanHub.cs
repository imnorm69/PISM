using Microsoft.AspNetCore.SignalR;

namespace PISM.Web.Hubs;

public class ScanHub : Hub
{
    public const string Url = "/hubs/scan";
}
