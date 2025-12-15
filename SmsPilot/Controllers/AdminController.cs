using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmsPilot.Data;
using SmsPilot.Models;

namespace SmsPilot.Controllers
{
    // Sécurité CRITIQUE : Seul un utilisateur avec le rôle "Admin" peut entrer ici
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // 1. LISTE DES UTILISATEURS (Tableau de bord Admin)
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // 2. CRÉER UN NOUVEL UTILISATEUR (Page)
        public IActionResult Create()
        {
            return View();
        }

        // 3. ENREGISTRER L'UTILISATEUR (Action)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            // --- CORRECTION MAJEURE ICI ---
            // On ignore les listes vides lors de la création
            ModelState.Remove("Contacts");
            ModelState.Remove("Messages");
            // -----------------------------

            if (ModelState.IsValid)
            {
                user.CreatedAt = DateTime.Now;
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Astuce de debug : Si ça ne marche toujours pas, regarde pourquoi
            // Mets un point d'arrêt ici ou inspecte les erreurs :
            // var errors = ModelState.Values.SelectMany(v => v.Errors);

            return View(user);
        }

        // 4. SUPPRIMER UN UTILISATEUR
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