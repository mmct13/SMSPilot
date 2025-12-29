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

        // J'affiche le formulaire de connexion
        [HttpGet]
        public IActionResult Login()
        {
            // Si l'utilisateur est déjà connecté, je le redirige directement vers le dashboard
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // Ici je traite la connexion quand l'utilisateur soumet le formulaire
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Je cherche l'utilisateur dans ma base de données
                // NOTE : En production, je devrais hacher le mot de passe. Ici c'est en clair juste pour l'exercice
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == model.Password);

                if (user != null)
                {
                    // Je crée les informations de l'utilisateur (Claims) pour la session
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Nom),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role.ToString()), // Important pour gérer les droits d'accès
                        new Claim("UserId", user.Id.ToString()) // Je garde l'ID pour l'utiliser plus tard
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // Je connecte l'utilisateur en créant le cookie d'authentification
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    // Tout est bon, je redirige vers l'accueil
                    return RedirectToAction("Index", "Home");
                }

                // Si les identifiants sont incorrects, j'affiche une erreur
                ModelState.AddModelError("", "Email ou mot de passe incorrect.");
            }

            return View(model);
        }

        // Déconnexion de l'utilisateur
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}