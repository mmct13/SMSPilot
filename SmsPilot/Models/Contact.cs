using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmsPilot.Models
{
    public class Contact
    {
        public int Id { get; set; }

        [Required]
        public string Nom { get; set; } // [cite: 61]

        public string Prenom { get; set; } // [cite: 61]

        [Required]
        [Phone]
        public string NumeroTelephone { get; set; } // Doit respecter le format international (+225...) [cite: 63]

        public string? Group { get; set; } // Étiquette (ex: VIP, Prospections) [cite: 65]

        // Clé étrangère vers l'Utilisateur (Chaque utilisateur gère ses propres contacts) [cite: 43]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}