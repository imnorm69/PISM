using Microsoft.AspNetCore.Mvc;
using PISM.Core.Enums;
using PISM.Data.Repositories;
using PISM.Web.ViewModels;

namespace PISM.Web.Controllers;

public class StatsController : Controller
{
    private readonly IImageRepository _imageRepo;
    private readonly IScanJobRepository _scanJobRepo;

    public StatsController(IImageRepository imageRepo, IScanJobRepository scanJobRepo)
    {
        _imageRepo = imageRepo;
        _scanJobRepo = scanJobRepo;
    }

    [HttpGet("/stats")]
    public async Task<IActionResult> Index()
    {
        var vm = new StatsViewModel
        {
            NeedsReviewCount = await _imageRepo.CountByStatusAsync(ImageStatus.NeedsReview),
            KeptCount = await _imageRepo.CountByStatusAsync(ImageStatus.Kept),
            DeletedCount = await _imageRepo.CountByStatusAsync(ImageStatus.Deleted),
            NeedsReviewBytes = await _imageRepo.TotalSizeByStatusAsync(ImageStatus.NeedsReview),
            KeptBytes = await _imageRepo.TotalSizeByStatusAsync(ImageStatus.Kept),
            DeletedBytes = await _imageRepo.TotalSizeByStatusAsync(ImageStatus.Deleted),
            DuplicatesCount = await _imageRepo.CountDuplicatesAsync(),
            RecentJobs = await _scanJobRepo.GetRecentAsync(5)
        };
        return View(vm);
    }
}
