using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebViajes.Models
{
    public class GastoFormViewModel
    {
        public int Id { get; set; }
        public int RepartoId { get; set; }

        [Required(ErrorMessage = "El concepto es obligatorio")]
        [StringLength(120)]
        public string Concepto { get; set; } = "";

        [Range(0.01, 1000000, ErrorMessage = "El importe debe ser mayor que 0")]
        public decimal Importe { get; set; }

        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [StringLength(500)]
        public string? Notas { get; set; }

        [Display(Name = "Pagado por")]
        public int PagadoPorId { get; set; }

        // Ids de los miembros que participan en el gasto (casillas marcadas)
        public List<int> ParticipantesIds { get; set; } = new();

        // Solo para pintar el formulario, no se recibe del navegador
        [BindNever]
        public List<Miembro> Miembros { get; set; } = new();
    }
}