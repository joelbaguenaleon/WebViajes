namespace WebViajes.Models;

public class PresupuestoDto
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Notas { get; set; }
    public List<string> Participantes { get; set; } = new();
    public List<PartidaDto> Partidas { get; set; } = new();
}

public class PartidaDto
{
    public string? Concepto { get; set; }
    public decimal? ImporteDirecto { get; set; }
    public string? Nota { get; set; }
    public List<DetalleDto> Detalles { get; set; } = new();
}

public class DetalleDto
{
    public string? Concepto { get; set; }
    public decimal? Importe { get; set; }
    public string? Nota { get; set; }
}