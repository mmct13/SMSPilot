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
        public string NumeroTelephone { get; set; } // Le numéro doit être au format international (+225...)

        public string? Group { get; set; } // Une étiquette pour organiser les contacts (ex: VIP, Prospections)

        // Clé étrangère : chaque contact appartient à un utilisateur spécifique
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}