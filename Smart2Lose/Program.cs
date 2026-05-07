using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smart2Lose.Data;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// log4net konfigurieren
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());

XmlConfigurator.Configure(
    logRepository,
    new FileInfo("log4net.config")
);



var log = LogManager.GetLogger(typeof(Program));
log.Info("Application starting...");
// =======================
// SERVICES
// =======================

// Razor Pages
builder.Services.AddRazorPages();


// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// DbContext (MySQL + Pomelo)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

// ASP.NET Core Identity
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Cookie-Konfiguration für Identity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// =======================
// ROLLEN-INITIALISIERUNG
// =======================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await Smart2Lose.Data.RoleSeeder.SeedRolesAsync(services);
}

// =======================
// MIDDLEWARE (REIHENFOLGE IST KRITISCH)
// =======================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();          // 1️⃣ Session
app.UseAuthentication();   // 2️⃣ Identity Authentication
app.UseAuthorization();    // 3️⃣ Identity Authorization

app.MapRazorPages();

app.Run();