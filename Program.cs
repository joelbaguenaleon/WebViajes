using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebViajes.Data;
using QuestPDF.Infrastructure;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using WebViajes.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Licencia de QuestPDF (versión Community)
QuestPDF.Settings.License = LicenseType.Community;

// Carpeta de datos: en Railway es el volumen, en local App_Data
var dataDir = Environment.GetEnvironmentVariable("RAILWAY_VOLUME_MOUNT_PATH") ?? "App_Data";
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "app.db");

// Base de datos SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Claves de Data Protection persistentes (para no perder sesiones en cada deploy)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));

// Cabeceras del proxy de Railway (HTTPS, IP real)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Identity (registro / login)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // no hay envío de correo todavía
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // necesario para las páginas de Identity (Login, Register...)
builder.Services.AddScoped<RepartoService>(); //servicio de repartos

// Tus servicios propios, si los tienes (descomenta y ajusta), por ejemplo:
// builder.Services.AddScoped<RepartoService>();

// ---------------------------------------------------------------
// 2) CONSTRUIR LA APP
// ---------------------------------------------------------------
var app = builder.Build();

// ---------------------------------------------------------------
// 3) PIPELINE: a partir de aquí solo app.Use... / app.Map...
// ---------------------------------------------------------------

// Debe ir lo primero
app.UseForwardedHeaders();

// Aplicar migraciones automáticamente al arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// En Railway el HTTPS lo gestiona el proxy, así que solo redirigimos en local
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();