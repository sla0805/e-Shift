// Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using eShift.Data; 
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Register EF Core with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .EnableSensitiveDataLogging() // Add this
           .LogTo(Console.WriteLine));
        


builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

//  Tell ASP.NET to use your custom login path
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

//First Admin account setup
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    try
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Create Admin Role if it doesn't exist
        string adminRoleName = "Admin";
        if (await roleManager.FindByNameAsync(adminRoleName) == null)
        {
            await roleManager.CreateAsync(new IdentityRole(adminRoleName));
        }

        // Intial Admin Account Info
        string adminEmail = "superadmin@gmail.com";
        string adminPassword = "Asd123!@#"; 

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);

            if (createAdminResult.Succeeded)
            {
                // Add Admin user to Admin Role
                await userManager.AddToRoleAsync(adminUser, adminRoleName);
                Console.WriteLine($"Admin user '{adminEmail}' created and assigned to '{adminRoleName}' role.");
            }
            else
            {
                Console.WriteLine($"Error creating admin user: {string.Join(", ", createAdminResult.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            Console.WriteLine($"Admin user '{adminEmail}' already exists.");
            // Optional: Ensure existing admin user has the admin role
            var existingAdminUser = await userManager.FindByEmailAsync(adminEmail);
            if (!await userManager.IsInRoleAsync(existingAdminUser, adminRoleName))
            {
                await userManager.AddToRoleAsync(existingAdminUser, adminRoleName);
                Console.WriteLine($"Existing user '{adminEmail}' assigned to '{adminRoleName}' role.");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database with admin user/roles.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
