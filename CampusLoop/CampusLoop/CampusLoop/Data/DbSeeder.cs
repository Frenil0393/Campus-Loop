using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CampusLoop.Models;

namespace CampusLoop.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<CampusLoopDbContext>();

        // 1. Seed Roles
        string[] roles = { Roles.Admin, Roles.Student };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Admin User
        var adminEmail = "admin@ddu.ac.in";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                PhoneNumber = "9998887770",
                Branch = "Administration",
                Semester = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
            }
        }

        // 3. Seed Demo Students (@ddu.ac.in)
        var student1Email = "rahul.sharma@ddu.ac.in";
        var student1 = await userManager.FindByEmailAsync(student1Email);
        if (student1 == null)
        {
            student1 = new ApplicationUser
            {
                UserName = student1Email,
                Email = student1Email,
                FullName = "Rahul Sharma",
                PhoneNumber = "9876543210",
                Branch = "Computer Engineering",
                Semester = 6,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(student1, "Student@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(student1, Roles.Student);
            }
        }

        var student2Email = "priya.patel@ddu.ac.in";
        var student2 = await userManager.FindByEmailAsync(student2Email);
        if (student2 == null)
        {
            student2 = new ApplicationUser
            {
                UserName = student2Email,
                Email = student2Email,
                FullName = "Priya Patel",
                PhoneNumber = "9876543211",
                Branch = "Information Technology",
                Semester = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(student2, "Student@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(student2, Roles.Student);
            }
        }

        // 4. Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new() { Name = "Calculators", Description = "Scientific calculators, graphing calculators, covers, and accessories" },
                new() { Name = "Laptops & Electronics", Description = "Laptops, chargers, keyboards, mouse, headphones, and USB drives" },
                new() { Name = "Textbooks & Notes", Description = "Engineering course books, reference materials, and handwritten notes" },
                new() { Name = "Drafting & Lab Equipment", Description = "Mini drafters, drawing sheets, roller scales, and lab coats" },
                new() { Name = "Study Tables & Furniture", Description = "Hostel study desks, folding chairs, bookshelves, and table lamps" },
                new() { Name = "Other Essentials", Description = "Backpacks, sports gear, bicycles, and general campus necessities" }
            };
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 5. Seed Sample Products (if none exist or update existing images)
        if (student1 != null && student2 != null)
        {
            var calcCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Calculators");
            var elecCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Laptops & Electronics");
            var bookCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Textbooks & Notes");
            var draftCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Drafting & Lab Equipment");
            var furnCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Study Tables & Furniture");

            if (!await context.Products.AnyAsync())
            {
                var sampleProducts = new List<Product>
                {
                    new()
                    {
                        Title = "Casio FX-991EX ClassWiz Scientific Calculator",
                        Description = "Original Casio FX-991EX ClassWiz in perfect working condition. Used for 2 semesters only. High-resolution LCD, spreadsheet and matrix calculation support. Original hard slip-on case included.",
                        Price = 850.00m,
                        CategoryId = calcCat?.Id ?? 1,
                        SellerId = student1.Id,
                        Status = ProductStatus.Available,
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    new()
                    {
                        Title = "HP Wireless Ergonomic Optical Mouse + Pad",
                        Description = "HP 2.4GHz wireless optical mouse with USB nano receiver. Battery life lasts 6+ months on a single AA battery. Comes with free anti-slip cloth mousepad.",
                        Price = 400.00m,
                        CategoryId = elecCat?.Id ?? 2,
                        SellerId = student1.Id,
                        Status = ProductStatus.Available,
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new()
                    {
                        Title = "Engineering Mechanics by Bhavikatti (Latest Edition)",
                        Description = "Standard textbook for 1st & 2nd year engineering students. Very clean pages with minimal pencil highlights. No torn or missing pages.",
                        Price = 320.00m,
                        CategoryId = bookCat?.Id ?? 3,
                        SellerId = student2.Id,
                        Status = ProductStatus.Available,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new()
                    {
                        Title = "Omega Mini Drafter with Heavy Clamp and Cover Bag",
                        Description = "Complete engineering drawing mini drafter with unbroken scales, smooth 360 degree protractor head, and protective canvas carry bag. Perfect for mechanical and civil engineering drawing labs.",
                        Price = 480.00m,
                        CategoryId = draftCat?.Id ?? 4,
                        SellerId = student2.Id,
                        Status = ProductStatus.Available,
                        CreatedAt = DateTime.UtcNow.AddHours(-12)
                    },
                    new()
                    {
                        Title = "Foldable Wooden Hostel Study Desk with Cup Holder",
                        Description = "Compact folding bed and floor study desk. Sturdy MDF top with aluminum legs. Includes slot for iPad/tablet and cup holder. Great for hostel rooms.",
                        Price = 650.00m,
                        CategoryId = furnCat?.Id ?? 5,
                        SellerId = student1.Id,
                        Status = ProductStatus.Sold,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    }
                };

                await context.Products.AddRangeAsync(sampleProducts);
                await context.SaveChangesAsync();

                var imageMap = new Dictionary<int, string>
                {
                    { sampleProducts[0].Id, "/images/casio_calculator.jpg" },
                    { sampleProducts[1].Id, "/images/wireless_mouse.jpg" },
                    { sampleProducts[2].Id, "/images/engineering_book.jpg" },
                    { sampleProducts[3].Id, "/images/drafting_kit.jpg" },
                    { sampleProducts[4].Id, "/images/sample_product_4.svg" }
                };

                var productImages = new List<ProductImage>();
                foreach (var p in sampleProducts)
                {
                    var img = imageMap.ContainsKey(p.Id) ? imageMap[p.Id] : "/images/placeholder.svg";
                    for (int i = 1; i <= 3; i++)
                    {
                        productImages.Add(new ProductImage
                        {
                            ProductId = p.Id,
                            ImageUrl = img,
                            IsPrimary = (i == 1),
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await context.ProductImages.AddRangeAsync(productImages);
                await context.SaveChangesAsync();
            }
            else
            {
                // Update existing sample images to use our crisp generated photos
                var p1 = await context.Products.FirstOrDefaultAsync(p => p.Title.Contains("Casio"));
                if (p1 != null)
                {
                    var imgs = await context.ProductImages.Where(i => i.ProductId == p1.Id).ToListAsync();
                    foreach (var img in imgs) img.ImageUrl = "/images/casio_calculator.jpg";
                }
                var p2 = await context.Products.FirstOrDefaultAsync(p => p.Title.Contains("Mouse"));
                if (p2 != null)
                {
                    var imgs = await context.ProductImages.Where(i => i.ProductId == p2.Id).ToListAsync();
                    foreach (var img in imgs) img.ImageUrl = "/images/wireless_mouse.jpg";
                }
                var p3 = await context.Products.FirstOrDefaultAsync(p => p.Title.Contains("Mechanics"));
                if (p3 != null)
                {
                    var imgs = await context.ProductImages.Where(i => i.ProductId == p3.Id).ToListAsync();
                    foreach (var img in imgs) img.ImageUrl = "/images/engineering_book.jpg";
                }
                var p4 = await context.Products.FirstOrDefaultAsync(p => p.Title.Contains("Drafter"));
                if (p4 != null)
                {
                    var imgs = await context.ProductImages.Where(i => i.ProductId == p4.Id).ToListAsync();
                    foreach (var img in imgs) img.ImageUrl = "/images/drafting_kit.jpg";
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
