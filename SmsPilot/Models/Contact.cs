using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmsPilot.Models
{
    public class Contact
    {
        public int Id { get; set; }

        [Required]
        public string Nom { get; set; }

        public string Prenom { get; set; }

        [Required]
        [Phone]
        public string NumeroTelephone { get; set; } // Doit respecter le format international (+225...)

        public string? Group { get; set; } // Étiquette (ex: VIP, Prospections)

        // Clé étrangère vers l'Utilisateur (Chaque utilisateur gère ses propres contacts)
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}