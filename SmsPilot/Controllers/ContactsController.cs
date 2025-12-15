using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmsPilot.Data;
using SmsPilot.Models;

namespace SmsPilot.Controllers
{
    [Authorize] // Oblige à être connecté
    public class ContactsController : Controller
    {
        private readonly AppDbContext _context;

        public ContactsController(AppDbContext context)
        {
            _context = context;
        }

        // Récupère l'ID de l'utilisateur connecté depuis le cookie
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        // GET: Contacts (Liste uniquement les contacts de l'utilisateur connecté)
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            // On filtre avec .Where(c => c.UserId == userId)
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
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId); // Sécurité

            if (contact == null) return NotFound();

            return View(contact);
        }

        // GET: Contacts/Create
        public IActionResult Create()
        {
            // On n'a plus besoin de charger la liste des Users (ViewData["UserId"])
            return View();
        }

        // POST: Contacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nom,Prenom,NumeroTelephone,Group")] Contact contact)
        {
            // On force l'ID de l'utilisateur connecté
            contact.UserId = GetCurrentUserId();

            // On retire "User" de la validation car on le définit manuellement
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

            // On remet le UserId qu'on avait perdu dans le formulaire
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