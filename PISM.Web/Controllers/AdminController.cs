using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PISM.Core.Options;
using PISM.Data.Services;

namespace PISM.Web.Controllers;

public class AdminController : Controller
{
    private readonly TestingOptions _testing;
    private readonly ResetService _reset;

    public AdminController(IOptions<TestingOptions> testing, ResetService reset)
    {
        _testing = testing.Value;
        _reset = reset;
    }

    [HttpGet("/admin/reset")]
    public IActionResult Reset()
    {
        if (!_testing.PreserveOriginals) return NotFound();
        return View();
    }

    [HttpPost("/admin/reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetConfirmed()
    {
        if (!_testing.PreserveOriginals) return NotFound();
        var result = await _reset.ResetAsync();
        return View(result);
    }
}
