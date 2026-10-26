namespace WebViajes.Models
{
    public class ResumenReparto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public List<MiembroResumen> Miembros { get; set; } = new();
        public List<GastoResumen> Gastos { get; set; } = new();
        public List<BalanceMiembro> Balances { get; set; } = new();
        public List<Liquidacion> Liquidaciones { get; set; } = new();

        public decimal Total => Gastos.Sum(g => g.Importe);
    }

    public record MiembroResumen(int Id, string Nombre);

    public record GastoResumen(int Id, DateTime Fecha, string Concepto, string? Notas,
                               string PagadoPor, string Participantes, decimal Importe);

    // Saldo = Pagado - Debe. Positivo: le deben dinero. Negativo: debe dinero.
    public record BalanceMiembro(string Nombre, decimal Pagado, decimal Debe, decimal Saldo);

    public record Liquidacion(string De, string A, decimal Importe);
}