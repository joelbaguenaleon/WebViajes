namespace WebViajes.Models;

public class Presupuesto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Notas { get; set; }
    public string UsuarioId { get; set; } = "";
    public List<Partida> Partidas { get; set; } = new();
    public List<Participante> Participantes { get; set; } = new();

    public decimal Total => Partidas.Sum(p => p.Importe);
    public decimal PorPersona => Participantes.Count == 0 ? 0 : Total / Participantes.Count;
}