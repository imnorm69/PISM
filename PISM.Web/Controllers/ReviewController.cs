using Microsoft.AspNetCore.Mvc;
using PISM.Core.Enums;
using PISM.Core.Models;
using PISM.Data.Repositories;
using PISM.Data.Services;
using PISM.Web.Models;
using PISM.Web.ViewModels;

namespace PISM.Web.Controllers;

public class ReviewController : Controller
{
    private const int PageSize = 24;

    private readonly IImageRepository _imageRepo;
    private readonly ReviewService _reviewService;

    public ReviewController(IImageRepository imageRepo, ReviewService reviewService)
    {
        _imageRepo = imageRepo;
        _reviewService = reviewService;
    }

    [HttpGet("/review")]
    public IActionResult Index() => RedirectToAction(nameof(Gallery));

    [HttpGet("/review/gallery")]
    public async Task<IActionResult> Gallery(int page = 1, string status = "NeedsReview")
    {
        if (!Enum.TryParse<ImageStatus>(status, true, out var statusEnum))
            statusEnum = ImageStatus.NeedsReview;

        page = Math.Max(1, page);
        var images = await _imageRepo.GetByStatusAsync(statusEnum, page, PageSize);
        var totalCount = await _imageRepo.CountByStatusAsync(statusEnum);

        var vm = new GalleryPageViewModel
        {
            Images = images,
            CurrentPage = page,
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize)),
            TotalCount = totalCount,
            CurrentStatus = statusEnum
        };

        return View(vm);
    }

    [HttpGet("/review/detail/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, string status = "NeedsReview")
    {
        if (!Enum.TryParse<ImageStatus>(status, true, out var statusEnum))
            statusEnum = ImageStatus.NeedsReview;

        var image = await _imageRepo.GetByIdAsync(id);
        if (image == null) return NotFound();

        var (prevId, nextId) = await _imageRepo.GetAdjacentIdsAsync(id, statusEnum);

        ImageFile? duplicateOf = null;
        if (image.DuplicateOfId.HasValue)
            duplicateOf = await _imageRepo.GetByIdAsync(image.DuplicateOfId.Value);

        var related = await _imageRepo.GetRelatedAsync(image.Hash);

        var vm = new DetailViewModel
        {
            Image = image,
            PreviousId = prevId,
            NextId = nextId,
            DuplicateOf = duplicateOf,
            CurrentStatus = statusEnum,
            RelatedImages = related
        };

        return View(vm);
    }

    [HttpPost("/review/keep/{id:guid}")]
    public async Task<IActionResult> Keep(Guid id)
    {
        await _reviewService.KeepAsync(id);
        return Ok(new { success = true });
    }

    [HttpPost("/review/delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _reviewService.DeleteAsync(id);
        return Ok(new { success = true });
    }

    [HttpPost("/review/bulk")]
    public async Task<IActionResult> Bulk([FromBody] BulkActionRequest request)
    {
        if (request.Ids.Count == 0) return BadRequest();

        int count = request.Action switch
        {
            "keep" => await _reviewService.BulkKeepAsync(request.Ids),
            "delete" => await _reviewService.BulkDeleteAsync(request.Ids),
            "tag" when !string.IsNullOrWhiteSpace(request.Tag)
                => await _reviewService.BulkTagAsync(request.Ids, request.Tag),
            _ => 0
        };

        return Ok(new { success = true, count });
    }

    [HttpPost("/review/save-crop/{id:guid}")]
    public async Task<IActionResult> SaveCrop(Guid id, [FromBody] SaveCropRequest request)
    {
        await _reviewService.SaveCropAsync(id, request.X, request.Y, request.Width, request.Height, request.Rotation);
        return Ok(new { success = true });
    }

    [HttpPost("/review/add-tag/{id:guid}")]
    public async Task<IActionResult> AddTag(Guid id, [FromBody] AddTagRequest request)
    {
        await _reviewService.AddTagAsync(id, request.Tag);
        var image = await _imageRepo.GetByIdAsync(id);
        var tags = image!.Tags.Select(t => new { id = t.Id, tag = t.Tag });
        return Ok(new { success = true, tags });
    }

    [HttpPost("/review/remove-tag/{tagId:guid}")]
    public async Task<IActionResult> RemoveTag(Guid tagId)
    {
        await _reviewService.RemoveTagAsync(tagId);
        return Ok(new { success = true });
    }
}
