using Microsoft.EntityFrameworkCore;
using WebViajes.Data;
using WebViajes.Models;

namespace WebViajes.Services
{
    public class RepartoService
    {
        private readonly ApplicationDbContext _context;

        public RepartoService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Devuelve null si el reparto no existe o no es del usuario
        public async Task<ResumenReparto?> ObtenerResumenAsync(int repartoId, string usuarioId)
        {
            var reparto = await _context.Repartos
                .AsNoTracking()
                .Include(r => r.Miembros)
                .Include(r => r.Gastos).ThenInclude(g => g.PagadoPor)
                .Include(r => r.Gastos).ThenInclude(g => g.Participantes).ThenInclude(p => p.Miembro)
                .FirstOrDefaultAsync(r => r.Id == repartoId && r.UsuarioId == usuarioId);

            if (reparto == null) return null;

            var pagado = reparto.Miembros.ToDictionary(m => m.Id, _ => 0m);
            var debe = reparto.Miembros.ToDictionary(m => m.Id, _ => 0m);

            foreach (var g in reparto.Gastos)
            {
                pagado[g.PagadoPorId] += g.Importe;

                var ids = g.Participantes.Select(p => p.MiembroId).OrderBy(i => i).ToList();
                if (ids.Count == 0) continue;

                // Reparto en céntimos para no perder ni inventar céntimos:
                // 10,00 € entre 3 = 3,34 + 3,33 + 3,33
                long centimos = (long)Math.Round(g.Importe * 100, MidpointRounding.AwayFromZero);
                long cuota = centimos / ids.Count;
                int resto = (int)(centimos % ids.Count);
                int desplazamiento = g.Id % ids.Count; // rota quién se lleva el céntimo extra

                for (int i = 0; i < ids.Count; i++)
                {
                    bool extra = ((i - desplazamiento + ids.Count) % ids.Count) < resto;
                    debe[ids[i]] += (cuota + (extra ? 1 : 0)) / 100m;
                }
            }

            var resumen = new ResumenReparto
            {
                Id = reparto.Id,
                Nombre = reparto.Nombre,
                Miembros = reparto.Miembros
                    .OrderBy(m => m.Nombre)
                    .Select(m => new MiembroResumen(m.Id, m.Nombre))
                    .ToList(),
                Gastos = reparto.Gastos
                    .OrderByDescending(g => g.Fecha).ThenByDescending(g => g.Id)
                    .Select(g => new GastoResumen(
                        g.Id, g.Fecha, g.Concepto, g.Notas,
                        g.PagadoPor?.Nombre ?? "",
                        string.Join(", ", g.Participantes
                            .Select(p => p.Miembro?.Nombre ?? "")
                            .OrderBy(n => n)),
                        g.Importe))
                    .ToList(),
                Balances = reparto.Miembros
                    .OrderBy(m => m.Nombre)
                    .Select(m => new BalanceMiembro(
                        m.Nombre, pagado[m.Id], debe[m.Id], pagado[m.Id] - debe[m.Id]))
                    .ToList()
            };

            resumen.Liquidaciones = CalcularLiquidaciones(resumen.Balances);
            return resumen;
        }

        // Empareja al que más debe con al que más le deben: pocas transferencias
        private static List<Liquidacion> CalcularLiquidaciones(List<BalanceMiembro> balances)
        {
            var deudores = balances.Where(b => b.Saldo < 0)
                .Select(b => (b.Nombre, Resto: -b.Saldo))
                .OrderByDescending(x => x.Resto).ToList();

            var acreedores = balances.Where(b => b.Saldo > 0)
                .Select(b => (b.Nombre, Resto: b.Saldo))
                .OrderByDescending(x => x.Resto).ToList();

            var resultado = new List<Liquidacion>();
            int i = 0, j = 0;

            while (i < deudores.Count && j < acreedores.Count)
            {
                var pago = Math.Min(deudores[i].Resto, acreedores[j].Resto);

                if (pago > 0)
                    resultado.Add(new Liquidacion(deudores[i].Nombre, acreedores[j].Nombre, pago));

                deudores[i] = (deudores[i].Nombre, deudores[i].Resto - pago);
                acreedores[j] = (acreedores[j].Nombre, acreedores[j].Resto - pago);

                if (deudores[i].Resto == 0) i++;
                if (acreedores[j].Resto == 0) j++;
            }

            return resultado;
        }
    }
}