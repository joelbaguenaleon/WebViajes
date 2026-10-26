using System.ComponentModel.DataAnnotations;

namespace WebViajes.Models
{
    public class Gasto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El concepto es obligatorio")]
        [StringLength(120)]
        [Display(Name = "Concepto")]
        public string Concepto { get; set; } = "";

        [Range(0.01, 1000000, ErrorMessage = "El importe debe ser mayor que 0")]
        [Display(Name = "Importe")]
        public decimal Importe { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [StringLength(500)]
        [Display(Name = "Notas")]
        public string? Notas { get; set; }

        public int RepartoId { get; set; }
        public Reparto? Reparto { get; set; }

        [Display(Name = "Pagado por")]
        public int PagadoPorId { get; set; }
        public Miembro? PagadoPor { get; set; }

        public List<GastoParticipante> Participantes { get; set; } = new();
    }
}