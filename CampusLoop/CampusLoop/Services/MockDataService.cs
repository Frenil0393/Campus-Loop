using CampusLoop.Models;

namespace CampusLoop.Services;

public static class MockDataService
{
    public static List<Category> GetCategories()
    {
        return new List<Category>
        {
            new Category { Id = 1, Name = "All Items", Slug = "all", IconClass = "bi-grid-fill", Description = "Browse all listings across campus", ItemCount = 8 },
            new Category { Id = 2, Name = "Calculators & Tech", Slug = "tech", IconClass = "bi-calculator-fill", Description = "Scientific calculators, microcontrollers & accessories", ItemCount = 2 },
            new Category { Id = 3, Name = "Drafting & Lab Gear", Slug = "drafting", IconClass = "bi-rulers", Description = "Mini drafters, sheet holders, lab coats & tools", ItemCount = 2 },
            new Category { Id = 4, Name = "Textbooks & Notes", Slug = "books", IconClass = "bi-journal-bookmark-fill", Description = "Semester textbooks, reference guides & handwritten notes", ItemCount = 2 },
            new Category { Id = 5, Name = "Hostel & Furniture", Slug = "hostel", IconClass = "bi-lamp-fill", Description = "Study tables, chairs, kettle, mattress & storage", ItemCount = 1 },
            new Category { Id = 6, Name = "Bicycles & Mobility", Slug = "mobility", IconClass = "bi-bicycle", Description = "Campus cycles, locks & helmets", ItemCount = 1 }
        };
    }

    public static List<StudentUser> GetStudents()
    {
        return new List<StudentUser>
        {
            new StudentUser
            {
                Id = "std_101",
                FullName = "Aarav Patel",
                CollegeEmail = "24ceuos155@ddu.ac.in",
                PhoneNumber = "+91 98251 44520",
                Branch = "Computer Engineering",
                Semester = "5th Semester",
                AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80"
            },
            new StudentUser
            {
                Id = "std_102",
                FullName = "Priya Sharma",
                CollegeEmail = "23meuos042@ddu.ac.in",
                PhoneNumber = "+91 97230 11984",
                Branch = "Mechanical Engineering",
                Semester = "3rd Semester",
                AvatarUrl = "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=150&auto=format&fit=crop&q=80"
            },
            new StudentUser
            {
                Id = "std_103",
                FullName = "Rohan Mehta",
                CollegeEmail = "22itus015@ddu.ac.in",
                PhoneNumber = "+91 94088 77651",
                Branch = "Information Technology",
                Semester = "4th Semester",
                AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80"
            },
            new StudentUser
            {
                Id = "std_104",
                FullName = "Harsh Desai",
                CollegeEmail = "21eeuos088@ddu.ac.in",
                PhoneNumber = "+91 99044 33218",
                Branch = "Electrical Engineering",
                Semester = "7th Semester",
                AvatarUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150&auto=format&fit=crop&q=80"
            },
            new StudentUser
            {
                Id = "std_105",
                FullName = "Sneha Joshi",
                CollegeEmail = "23clus031@ddu.ac.in",
                PhoneNumber = "+91 98980 65432",
                Branch = "Civil Engineering",
                Semester = "2nd Semester",
                AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&auto=format&fit=crop&q=80"
            },
            new StudentUser
            {
                Id = "std_106",
                FullName = "Dev Shah",
                CollegeEmail = "22chus012@ddu.ac.in",
                PhoneNumber = "+91 97123 88410",
                Branch = "Chemical Engineering",
                Semester = "6th Semester",
                AvatarUrl = "https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?w=150&auto=format&fit=crop&q=80"
            }
        };
    }

    public static List<Product> GetProducts()
    {
        var students = GetStudents();
        var categories = GetCategories();

        return new List<Product>
        {
            new Product
            {
                Id = 1,
                Name = "Casio FX-991CW ClassWiz Scientific Calculator",
                Description = "Barely used for one semester exams. Supports 540+ scientific and matrix calculations. Pristine display, solar panel working flawlessly, comes with original sliding hard cover and user guide.",
                Price = 850,
                CategoryId = 2,
                Category = categories.First(c => c.Id == 2),
                SellerId = "std_101",
                Seller = students.First(s => s.Id == "std_101"),
                Condition = "Like New",
                CampusLocation = "DDU Faculty of Technology - Room 204",
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                Status = "AVAILABLE",
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1594980596870-8aa52a78d8cd?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1587145820266-a5951ee6f620?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1611348586804-61bf6c080437?w=600&auto=format&fit=crop&q=80"
                }
            },
            new Product
            {
                Id = 2,
                Name = "Omega Mini Drafter with Sheet Container & Clips",
                Description = "Essential for 1st/2nd year Engineering Graphics and Machine Drawing. High precision steel rods, scale markings completely clear, includes waterproof chart carrying canister and 4 sheet clips.",
                Price = 420,
                CategoryId = 3,
                Category = categories.First(c => c.Id == 3),
                SellerId = "std_102",
                Seller = students.First(s => s.Id == "std_102"),
                Condition = "Good",
                CampusLocation = "DDU Workshop Gate",
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                Status = "AVAILABLE",
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1503387762-592deb58ef4e?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1581092335397-9583fe92d232?w=600&auto=format&fit=crop&q=80"
                }
            },
            new Product
            {
                Id = 3,
                Name = "Data Structures & Algorithms in C++ (Balagurusamy)",
                Description = "3rd/4th semester textbook. Includes clean diagrams, step-by-step pointers and tree explanations. No missing pages, very clean binding with transparent plastic cover.",
                Price = 280,
                CategoryId = 4,
                Category = categories.First(c => c.Id == 4),
                SellerId = "std_103",
                Seller = students.First(s => s.Id == "std_103"),
                Condition = "Gently Used",
                CampusLocation = "DDU Central Library Reading Hall",
                CreatedAt = DateTime.UtcNow.AddHours(-12),
                Status = "AVAILABLE",
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=600&auto=format&fit=crop&q=80"
                }
            },
            new Product
            {
                Id = 4,
                Name = "Wooden Foldable Study Table & Bed Desk",
                Description = "Solid engineered wood hostel study table with cup holder and tablet slot. Perfect for late-night exam prep. Lightweight, easy to fold and store under bed when not in use.",
                Price = 490,
                CategoryId = 5,
                Category = categories.First(c => c.Id == 5),
                SellerId = "std_104",
                Seller = students.First(s => s.Id == "std_104"),
                Condition = "Good",
                CampusLocation = "DDU Boys Hostel Block C - Room 304",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Status = "AVAILABLE",
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1518455027359-f3f8164ba6bd?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1540518614846-7ede433c4550?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1524758631624-e2822e304c36?w=600&auto=format&fit=crop&q=80"
                }
            },
            new Product
            {
                Id = 5,
                Name = "Arduino Uno R3 Starter Kit with 30+ Sensors",
                Description = "Complete microcontrollers starter kit. Contains Arduino Uno, breadboard, jumper wires, servo motor, ultrasonic sensor, RFID reader, and LCD 16x2. Ideal for mini-projects.",
                Price = 750,
                CategoryId = 2,
                Category = categories.First(c => c.Id == 2),
                SellerId = "std_101",
                Seller = students.First(s => s.Id == "std_101"),
                Condition = "Like New",
                CampusLocation = "DDU IoT / Robotics Lab",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                Status = "AVAILABLE",
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1555680202-c86f0e12f086?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1518770660439-4636190af475?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1553406830-ef2513450d76?w=600&auto=format&fit=crop&q=80"
                }
            },
            new Product
            {
                Id = 6,
                Name = "Hero Sprint 26T Single Speed Campus Cycle",
                Description = "Used for daily commuting between hostel and academic blocks. Front & rear V-brakes in great condition, newly fitted tires, comes with high-security combination wire lock.",
                Price = 2200,
                CategoryId = 6,
                Category = categories.First(c => c.Id == 6),
                SellerId = "std_106",
                Seller = students.First(s => s.Id == "std_106"),
                Condition = "Good",
                CampusLocation = "DDU Main Gate Cycle Stand (Nadiad)",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                Status = "AVAILABLE",
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1485965120184-e220f721d03e?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1532298229144-0ec0c57515c7?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1507035895480-2b3156c31fc8?w=600&auto=format&fit=crop&q=80"
                }
            },
            new Product
            {
                Id = 7,
                Name = "Operating System Concepts 10th Ed (Silberschatz)",
                Description = "The classic Dinosaur book for OS. Covers processes, memory management, scheduling, virtualization, and file systems. Zero ink markings inside.",
                Price = 360,
                CategoryId = 4,
                Category = categories.First(c => c.Id == 4),
                SellerId = "std_103",
                Seller = students.First(s => s.Id == "std_103"),
                Condition = "Like New",
                CampusLocation = "DDU Central Lawn / Canteen",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                Status = "AVAILABLE",
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1532012164546-f432f2e37272?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1516979187457-637abb4f9353?w=600&auto=format&fit=crop&q=80"
                }
            },
            new Product
            {
                Id = 8,
                Name = "Engineering Drawing Instrument Box & Set Squares",
                Description = "Includes high precision compass, divider, lengthening bar, 45 & 60 degree set squares, protractor, and clutch pencil 0.5mm. Ready for drawing exam.",
                Price = 210,
                CategoryId = 3,
                Category = categories.First(c => c.Id == 3),
                SellerId = "std_105",
                Seller = students.First(s => s.Id == "std_105"),
                Condition = "Good",
                CampusLocation = "DDU Civil Dept 1st Floor",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                Status = "AVAILABLE",
                ImageUrls = new List<string>
                {
                    "https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1452860606245-08befc0ff44b?w=600&auto=format&fit=crop&q=80",
                    "https://images.unsplash.com/photo-1581291518857-4e27b48ff24e?w=600&auto=format&fit=crop&q=80"
                }
            }
        };
    }
}
