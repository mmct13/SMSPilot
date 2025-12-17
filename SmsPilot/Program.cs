using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SmsPilot.Data;
using SmsPilot.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration de la base de données SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- AUTHENTIFICATION (Début) ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // Redirection si non connecté
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Durée de la session
    });
// --- AUTHENTIFICATION (Fin) ---
builder.Services.AddHttpClient<OrangeSmsService>();
// Service d'arrière-plan (Worker)
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
    pattern: "{controller=Auth}/{action=Login}/{id?}"); // On change la page par défaut vers Login
app.Run();
