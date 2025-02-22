using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Beltelecom.Data;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Настройка Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender, EmailSenderStub>();

// Добавление MVC и Razor Pages
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

var app = builder.Build();

//// **Применение миграций перед созданием пользователей и ролей**
//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    context.Database.Migrate(); // Применяем миграции
//}

//// **Вызываем асинхронную инициализацию ролей и администратора**
//await InitializeRolesAndAdminAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
app.MapRazorPages();

try
{
    // Создаем DbContextOptions
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(connectionString) // Используем строку подключения из конфигурации
        .Options;

    // Передаем options в конструктор ApplicationDbContext
    using (var context = new ApplicationDbContext(options))
    {
        var products = context.Products.ToList();
        Console.WriteLine($"Найдено товаров: {products.Count}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка при подключении к базе данных: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}


app.Run();


/// <summary>
/// Асинхронная инициализация ролей и пользователя-администратора.
/// </summary>
async Task InitializeRolesAndAdminAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = { "Admin", "User" };

    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    string adminEmail = "admin@beltelecom.by";
    string adminPassword = "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}