using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;
using CampusLoop.Hubs;

// Automatically determine the correct project root and wwwroot paths
string projectRoot = Directory.GetCurrentDirectory();
if (!Directory.Exists(Path.Combine(projectRoot, "wwwroot")))
{
    // If started from bin/Debug/net10.0, navigate to true project folder
    string candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    if (Directory.Exists(Path.Combine(candidate, "wwwroot")))
    {
        projectRoot = candidate;
    }
}
string webRoot = Path.Combine(projectRoot, "wwwroot");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = projectRoot,
    WebRootPath = webRoot
});

// Listen on both port 5004 and 5000 so it ALWAYS responds on either URL
builder.WebHost.UseUrls("http://localhost:5004", "http://localhost:5000");

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Entity Framework Core with SQLite database located in project root
string dbPath = Path.Combine(projectRoot, "CampusLoop.db");
builder.Services.AddDbContext<CampusLoopDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? $"Data Source={dbPath}"));

// 2. Cookie Authentication for Students and Admin (R.1, R.8, R.13)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "CampusLoop.Auth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
});

// 3. SignalR for Real-time chat (R.6)
builder.Services.AddSignalR();

var app = builder.Build();

// Auto-initialize & seed SQLite database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CampusLoopDbContext>();
    db.InitializeDatabase();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// IMPORTANT FOR VISUAL STUDIO: UseStaticFiles MUST be called BEFORE UseRouting()
// This ensures that /css/site.css, images, and js load 100% reliably in Visual Studio and browsers!
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Route configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SignalR Real-Time Chat Hub mapping (R.6)
app.MapHub<ChatHub>("/chatHub");

// Auto-open browser on startup
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "http://localhost:5004",
            UseShellExecute = true
        });
    }
    catch { }
});

app.Run();
