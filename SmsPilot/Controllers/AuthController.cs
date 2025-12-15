using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmsPilot.Data;
using SmsPilot.Models;
using SmsPilot.ViewModels;
using System.Security.Claims;

namespace SmsPilot.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Affiche le formulaire de connexion
        [HttpGet]
        public IActionResult Login()
        {
            // Si déjà connecté, on redirige vers l'accueil (Dashboard)
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Traite la connexion
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Chercher l'utilisateur dans la BDD
                // Note : En production, on doit hacher le mot de passe. Ici, on compare en clair pour l'exercice.
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == model.Password);

                if (user != null)
                {
                    // 2. Créer les informations de l'utilisateur (Claims)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Nom),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role.ToString()), // Important pour les droits [cite: 33]
                        new Claim("UserId", user.Id.ToString()) // On garde l'ID pour plus tard
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // 3. Connecter l'utilisateur (Créer le cookie)
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    // Redirection vers l'accueil
                    return RedirectToAction("Index", "Home");
                }

                // Erreur [cite: 50]
                ModelState.AddModelError("", "Email ou mot de passe incorrect.");
            }

            return View(model);
        }

        // Déconnexion [cite: 52]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}