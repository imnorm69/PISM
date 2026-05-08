using Microsoft.AspNetCore.Mvc;
using PISM.Core.Enums;
using PISM.Core.Services;
using PISM.Data.Repositories;
using PISM.Web.ViewModels;

namespace PISM.Web.Controllers;

public class ScanController : Controller
{
    private readonly IScannerService _scanner;
    private readonly IScanJobRepository _scanJobRepo;

    public ScanController(IScannerService scanner, IScanJobRepository scanJobRepo)
    {
        _scanner = scanner;
        _scanJobRepo = scanJobRepo;
    }

    [HttpGet("/scan")]
    public async Task<IActionResult> Index()
    {
        var allRecent = await _scanJobRepo.GetRecentAsync(20);
        var vm = new ScanIndexViewModel
        {
            ActiveJobs = allRecent
                .Where(j => j.Status == ScanJobStatus.Pending || j.Status == ScanJobStatus.Running)
                .ToList(),
            RecentJobs = allRecent
                .Where(j => j.Status != ScanJobStatus.Pending && j.Status != ScanJobStatus.Running)
                .Take(10)
                .ToList()
        };
        return View(vm);
    }

    [HttpPost("/scan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start([FromForm] string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            TempData["Error"] = "Folder path is required.";
            return RedirectToAction(nameof(Index));
        }

        await _scanner.EnqueueJobAsync(folderPath.Trim());
        TempData["Success"] = $"Scan queued for: {folderPath.Trim()}";
        return RedirectToAction(nameof(Index));
    }
}
