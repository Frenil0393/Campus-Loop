using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Models;

namespace CampusLoop.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly CampusLoopDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        CampusLoopDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<NotificationsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: /Notifications
    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var notifications = await _context.Notifications
            .Where(n => n.UserId == currentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return View(notifications);
    }

    // GET: /Notifications/Open/5
    public async Task<IActionResult> Open(int id)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null) return NotFound();

        if (notification.UserId != currentUserId)
        {
            _logger.LogWarning("Forbidden notification access attempt: User {UserId} tried to open Notification {NotificationId} owned by {OwnerId}",
                currentUserId, id, notification.UserId);
            return Forbid();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Notification {NotificationId} marked as read by User {UserId}", id, currentUserId);
        }

        if (!string.IsNullOrEmpty(notification.LinkUrl) && Url.IsLocalUrl(notification.LinkUrl))
        {
            return Redirect(notification.LinkUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Notifications/MarkAllRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var count = await _context.Notifications
            .Where(n => n.UserId == currentUserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        _logger.LogInformation("User {UserId} marked all {Count} unread notifications as read", currentUserId, count);

        return RedirectToAction(nameof(Index));
    }
}
