using KahootTransnetBW.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// ✅ EINMALIG Builder verwenden
builder.Services.AddRazorPages();

// ✅ ConnectionString auslesen (nach CreateBuilder, aber ohne es erneut aufzurufen!)
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

app.UseRouting();

app.UseAuthorization(); // ✅ funktioniert jetzt

app.MapRazorPages();

app.Run();
