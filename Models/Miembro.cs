using System.ComponentModel.DataAnnotations;

namespace WebViajes.Models
{
    public class Miembro
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(60)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = "";

        public int RepartoId { get; set; }
        public Reparto? Reparto { get; set; }
    }
}