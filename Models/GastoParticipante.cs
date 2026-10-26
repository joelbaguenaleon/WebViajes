namespace WebViajes.Models
{
    public class GastoParticipante
    {
        public int Id { get; set; }

        public int GastoId { get; set; }
        public Gasto? Gasto { get; set; }

        public int MiembroId { get; set; }
        public Miembro? Miembro { get; set; }
    }
}