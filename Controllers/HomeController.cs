using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;
using CampusLoop.Services;
using CampusLoop.ViewModels;

namespace CampusLoop.Controllers;

public class HomeController : Controller
{
    private readonly CampusLoopDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMarketplaceItemService _marketplaceItemService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        CampusLoopDbContext context,
        UserManager<ApplicationUser> userManager,
        IMarketplaceItemService marketplaceItemService,
        ILogger<HomeController> logger)
    {
        _context = context;
        _userManager = userManager;
        _marketplaceItemService = marketplaceItemService;
        _logger = logger;
    }

    // GET: / (Marketplace - R.2.1, R.4.1, R.4.2)
    public async Task<IActionResult> Index(string? search, int? categoryId, string? sort)
    {
        var currentUserId = _userManager.GetUserId(User);
        _logger.LogInformation("Loading marketplace home page for User {UserId}. Search: '{Search}', Category: {CategoryId}, Sort: '{Sort}'",
            currentUserId ?? "Anonymous", search, categoryId, sort);

        var items = await _marketplaceItemService.GetMarketplaceItemsAsync(search, categoryId, sort, currentUserId);

        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.CurrentSearch = search;
        ViewBag.CurrentCategory = categoryId;
        ViewBag.CurrentSort = sort;

        return View(items);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        _logger.LogError("Home error occurred. RequestId: {RequestId}", requestId);
        return View(new ErrorViewModel { RequestId = requestId });
    }
}
