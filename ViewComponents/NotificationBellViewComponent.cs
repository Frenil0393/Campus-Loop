using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;

namespace CampusLoop.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly CampusLoopDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationBellViewComponent(
        CampusLoopDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var userId = _userManager.GetUserId(HttpContext.User);
        if (string.IsNullOrEmpty(userId))
        {
            return View(0);
        }

        var unreadCount = await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return View(unreadCount);
    }
}
