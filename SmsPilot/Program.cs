using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SmsPilot.Data;
using SmsPilot.Services;

var builder = WebApplication.CreateBuilder(args);

// Je configure ma base de données SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Authentification : Début ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // Si l'utilisateur n'est pas connecté, je le redirige ici
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // La session dure 1 heure
    });
// --- Authentification : Fin ---
// Je configure le service d'envoi SMS Orange
builder.Services.AddHttpClient<OrangeSmsService>();
// J'ajoute mon service d'arrière-plan qui gère les SMS programmés
builder.Services.AddHostedService<SmsWorker>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
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
    pattern: "{controller=Auth}/{action=Login}/{id?}"); // Par défaut, je redirige vers la page de connexion
try
{
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "L'application s'est arrêtée de manière inattendue.");
    throw;
}
