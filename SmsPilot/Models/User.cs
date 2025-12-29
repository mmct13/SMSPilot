using System.ComponentModel.DataAnnotations;

namespace SmsPilot.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        public string Nom { get; set; } // Le nom de l'utilisateur

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress]
        public string Email { get; set; } // L'email sert d'identifiant de connexion

        [Required]
        public string PasswordHash { get; set; } // Je stocke le mot de passe (en clair pour l'instant, mais il faudrait le chiffrer en production)

        public UserRole Role { get; set; } = UserRole.User; // Par défaut, un nouvel utilisateur est un "User" simple

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Un utilisateur peut avoir plusieurs contacts
        public virtual ICollection<Contact> Contacts { get; set; }

        // Un utilisateur peut avoir plusieurs messages dans son historique
        public virtual ICollection<SmsMessage> Messages { get; set; }
    }
}