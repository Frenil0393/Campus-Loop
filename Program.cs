using CampusLoop.Data;
using CampusLoop.Hubs;
using CampusLoop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure DbContext for SQL Server (EF Core Code First)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=CampusLoopDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

builder.Services.AddDbContext<CampusLoopDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<CampusLoopDbContext>()
.AddDefaultTokenProviders();

// 3. Configure Cookie Authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// 4. Configure SignalR
builder.Services.AddSignalR();

// 5. Add Controllers and Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map SignalR Hub
app.MapHub<ChatHub>("/chatHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Database initialization and seeding on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CampusLoopDbContext>();
        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating or seeding the database.");
    }
}

app.Run();
