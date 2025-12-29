using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmsPilot.Data;
using SmsPilot.Models;

namespace SmsPilot.Controllers
{
    [Authorize] // Ici aussi, je vérifie que l'utilisateur est connecté avant d'accéder aux contacts
    public class ContactsController : Controller
    {
        private readonly AppDbContext _context;

        public ContactsController(AppDbContext context)
        {
            _context = context;
        }

        // Je récupère l'ID de l'utilisateur connecté depuis le cookie d'authentification
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        // Affichage de la liste des contacts (uniquement ceux de l'utilisateur connecté)
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            // Je filtre pour n'afficher que les contacts de cet utilisateur
            var contacts = await _context.Contacts
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(contacts);
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            int userId = GetCurrentUserId();
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId); // Important : je vérifie que le contact appartient bien à l'utilisateur

            if (contact == null) return NotFound();

            return View(contact);
        }

        // GET: Contacts/Create
        public IActionResult Create()
        {
            // Plus besoin de charger la liste des utilisateurs, je gère ça automatiquement
            return View();
        }

        // POST: Contacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nom,Prenom,NumeroTelephone,Group")] Contact contact)
        {
            // Je force l'ID de l'utilisateur connecté pour éviter qu'on crée un contact pour quelqu'un d'autre
            contact.UserId = GetCurrentUserId();

            // Je retire "User" de la validation car je le définis manuellement juste au-dessus
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                _context.Add(contact);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contact);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            int userId = GetCurrentUserId();
            var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (contact == null) return NotFound();

            return View(contact);
        }

        // POST: Contacts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,Prenom,NumeroTelephone,Group")] Contact contact)
        {
            if (id != contact.Id) return NotFound();

            // Je récupère le UserId qu'on avait perdu dans le formulaire (il n'est pas dans le Bind)
            contact.UserId = GetCurrentUserId();
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contact);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(contact);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            int userId = GetCurrentUserId();
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (contact == null) return NotFound();

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int userId = GetCurrentUserId();
            var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            int userId = GetCurrentUserId();
            return _context.Contacts.Any(e => e.Id == id && e.UserId == userId);
        }
    }
}