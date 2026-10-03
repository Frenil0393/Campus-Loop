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

    public HomeController(
        CampusLoopDbContext context,
        UserManager<ApplicationUser> userManager,
        IMarketplaceItemService marketplaceItemService)
    {
        _context = context;
        _userManager = userManager;
        _marketplaceItemService = marketplaceItemService;
    }

    // GET: / (Marketplace - R.2.1, R.4.1, R.4.2)
    public async Task<IActionResult> Index(string? search, int? categoryId, string? sort)
    {
        var currentUserId = _userManager.GetUserId(User);

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
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
