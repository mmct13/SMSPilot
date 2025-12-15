using System.ComponentModel.DataAnnotations;

namespace SmsPilot.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        public string Nom { get; set; } // [cite: 55]

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress]
        public string Email { get; set; } // Identifiant de connexion [cite: 49]

        [Required]
        public string PasswordHash { get; set; } // On stockera le mot de passe chiffré pour la sécurité

        public UserRole Role { get; set; } = UserRole.User; // Par défaut User [cite: 56]

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relation : Un utilisateur a plusieurs contacts [cite: 43]
        public virtual ICollection<Contact> Contacts { get; set; }

        // Relation : Un utilisateur a plusieurs messages (historique personnel) [cite: 45]
        public virtual ICollection<SmsMessage> Messages { get; set; }
    }
}