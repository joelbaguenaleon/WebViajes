namespace WebViajes.Models;

public class Partida
{
    public int Id { get; set; }
    public int PresupuestoId { get; set; }
    public string Concepto { get; set; } = "";
    public decimal? ImporteDirecto { get; set; }   // solo se usa si no hay detalles
    public string? Nota { get; set; }
    public List<Subpartida> Detalles { get; set; } = new();

    // No se guarda en la base de datos: se calcula
    public decimal Importe => Detalles.Any()
        ? Detalles.Sum(d => d.Importe)
        : ImporteDirecto ?? 0;
}

public class Subpartida
{
    public int Id { get; set; }
    public int PartidaId { get; set; }
    public string Concepto { get; set; } = "";     // vuelo A, vuelo B...
    public decimal Importe { get; set; }
    public string? Nota { get; set; }
}