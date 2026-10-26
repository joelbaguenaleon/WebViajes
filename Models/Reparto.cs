using System.ComponentModel.DataAnnotations;

namespace WebViajes.Models
{
    public class Reparto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = "";

        // Id del usuario de Identity que es dueño del reparto
        public string UsuarioId { get; set; } = "";

        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public List<Miembro> Miembros { get; set; } = new();
        public List<Gasto> Gastos { get; set; } = new();
    }
}