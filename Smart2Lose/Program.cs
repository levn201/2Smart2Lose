using Smart2Lose.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// ✅ EINMALIG Builder verwenden
builder.Services.AddRazorPages();

// ✅ Session konfigurieren
builder.Services.AddDistributedMemoryCache(); // Für Session-Speicherung im Arbeitsspeicher
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);  // Timeout für Session nach 20 Minuten
    options.Cookie.HttpOnly = true;                  // Sicherheitsoptionen
    options.Cookie.IsEssential = true;               // Notwendig für Funktionalität
});

// ✅ ConnectionString auslesen
string connectionString = builder.Configuration.GetConnectionString("MySqlConnection");

var app = builder.Build();

// ✅ Middleware-Konfiguration
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession(); // Füge dies vor Routing hinzu

app.UseRouting();

app.UseAuthorization(); // ✅ funktioniert jetzt

app.MapRazorPages();

app.Run();
