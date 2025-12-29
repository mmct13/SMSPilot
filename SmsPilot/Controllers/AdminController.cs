using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmsPilot.Data;
using SmsPilot.Models;

namespace SmsPilot.Controllers
{
    // ATTENTION : Ici c'est réservé aux admins uniquement, personne d'autre ne peut entrer
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Ma page d'administration où je liste tous les utilisateurs
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // Formulaire pour créer un nouvel utilisateur
        public IActionResult Create()
        {
            return View();
        }

        // Traitement de la création d'utilisateur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            // IMPORTANT : J'ignore les listes vides lors de la création
            // Sinon ModelState va se plaindre que Contacts et Messages sont null
            ModelState.Remove("Contacts");
            ModelState.Remove("Messages");

            if (ModelState.IsValid)
            {
                user.CreatedAt = DateTime.Now;
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Astuce de debug si jamais ça ne marche toujours pas :
            // Je peux mettre un point d'arrêt ici ou inspecter les erreurs comme ça :
            // var errors = ModelState.Values.SelectMany(v => v.Errors);

            return View(user);
        }

        // Suppression d'un utilisateur
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}