using GisyWeb.Components;
using Microsoft.EntityFrameworkCore; // Nécessaire pour UseSqlite
using GisyWeb.Services;      // Ajuste si ton namespace est différent
using GisyWeb.Infrastructure.Services; // Pour AdminService et UiOrchestrator
using GisyWeb.Infrastructure.AdminDb; // Pour ton AdminDbContext

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. CONFIGURATION DES SERVICES (D.I.)
// ---------------------------------------------------------

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- CONFIGURATION SQLITE (Base Globale GLO) ---
var connectionString = "Data Source=Admin/Carto5Data/Carto5_GLO_Admin.db";
builder.Services.AddDbContextFactory<Carto5AdminContext>(options =>
    options.UseSqlite(connectionString));

// --- ENREGISTREMENT DE TES SERVICES ---
// Ces services seront maintenant disponibles dans tes pages @inject
builder.Services.AddSingleton<SessionContext>(); 
builder.Services.AddScoped<SystemContextService>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<UiOrchestratorService>();


var app = builder.Build();

// ---------------------------------------------------------
// 2. CONFIGURATION DU PIPELINE HTTP (Middleware)
// ---------------------------------------------------------

// Autoriser l'affichage dans une iframe (CSP et X-Frame)
app.Use(async (context, next) =>
{
    context.Response.Headers.Remove("X-Frame-Options");
    context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'self' https://localhost:7149 https://www.gisywebML.com http://www.gisywebML.com;");
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Ignorer les erreurs de certificat en développement
if (app.Environment.IsDevelopment())
{
    System.Net.ServicePointManager.ServerCertificateValidationCallback +=
        (sender, cert, chain, sslPolicyErrors) => true;
}

app.Run();