using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebViajes.Data;
using WebViajes.Models;

namespace WebViajes.Controllers
{
    [Authorize]
    public class GastosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GastosController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string UsuarioActualId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private Task<Reparto?> RepartoDelUsuarioAsync(int repartoId)
        {
            return _context.Repartos
                .Include(r => r.Miembros)
                .FirstOrDefaultAsync(r => r.Id == repartoId && r.UsuarioId == UsuarioActualId);
        }

        private Task<Gasto?> GastoDelUsuarioAsync(int id)
        {
            return _context.Gastos
                .Include(g => g.Participantes)
                .Include(g => g.Reparto).ThenInclude(r => r!.Miembros)
                .FirstOrDefaultAsync(g => g.Id == id && g.Reparto!.UsuarioId == UsuarioActualId);
        }

        // Comprueba que quien pagó y los participantes son miembros de ESTE reparto
        private void Validar(GastoFormViewModel vm, Reparto reparto)
        {
            var ids = reparto.Miembros.Select(m => m.Id).ToHashSet();

            if (!ids.Contains(vm.PagadoPorId))
                ModelState.AddModelError(nameof(vm.PagadoPorId), "Elige quién pagó.");

            if (vm.ParticipantesIds.Count == 0)
                ModelState.AddModelError(nameof(vm.ParticipantesIds), "Marca al menos un participante.");
            else if (vm.ParticipantesIds.Any(id => !ids.Contains(id)))
                ModelState.AddModelError(nameof(vm.ParticipantesIds), "Hay un participante no válido.");
        }

        private static List<Miembro> MiembrosOrdenados(Reparto reparto) =>
            reparto.Miembros.OrderBy(m => m.Nombre).ToList();

        // GET: Gastos/Create?repartoId=5
        public async Task<IActionResult> Create(int repartoId)
        {
            var reparto = await RepartoDelUsuarioAsync(repartoId);
            if (reparto == null) return NotFound();

            if (!reparto.Miembros.Any())
            {
                TempData["Error"] = "Añade al menos un miembro antes de crear gastos.";
                return RedirectToAction("Details", "Repartos", new { id = repartoId });
            }

            var vm = new GastoFormViewModel
            {
                RepartoId = repartoId,
                Miembros = MiembrosOrdenados(reparto),
                // Por defecto participan todos
                ParticipantesIds = reparto.Miembros.Select(m => m.Id).ToList()
            };
            return View(vm);
        }

        // POST: Gastos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GastoFormViewModel vm)
        {
            var reparto = await RepartoDelUsuarioAsync(vm.RepartoId);
            if (reparto == null) return NotFound();

            Validar(vm, reparto);
            if (!ModelState.IsValid)
            {
                vm.Miembros = MiembrosOrdenados(reparto);
                return View(vm);
            }

            var gasto = new Gasto
            {
                RepartoId = vm.RepartoId,
                Concepto = vm.Concepto.Trim(),
                Importe = vm.Importe,
                Fecha = vm.Fecha,
                Notas = string.IsNullOrWhiteSpace(vm.Notas) ? null : vm.Notas.Trim(),
                PagadoPorId = vm.PagadoPorId,
                Participantes = vm.ParticipantesIds.Distinct()
                    .Select(id => new GastoParticipante { MiembroId = id })
                    .ToList()
            };

            _context.Gastos.Add(gasto);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Repartos", new { id = vm.RepartoId });
        }

        // GET: Gastos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var gasto = await GastoDelUsuarioAsync(id);
            if (gasto == null) return NotFound();

            var vm = new GastoFormViewModel
            {
                Id = gasto.Id,
                RepartoId = gasto.RepartoId,
                Concepto = gasto.Concepto,
                Importe = gasto.Importe,
                Fecha = gasto.Fecha,
                Notas = gasto.Notas,
                PagadoPorId = gasto.PagadoPorId,
                ParticipantesIds = gasto.Participantes.Select(p => p.MiembroId).ToList(),
                Miembros = MiembrosOrdenados(gasto.Reparto!)
            };
            return View(vm);
        }

        // POST: Gastos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GastoFormViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            var gasto = await GastoDelUsuarioAsync(id);
            if (gasto == null) return NotFound();

            Validar(vm, gasto.Reparto!);
            if (!ModelState.IsValid)
            {
                vm.RepartoId = gasto.RepartoId;
                vm.Miembros = MiembrosOrdenados(gasto.Reparto!);
                return View(vm);
            }

            gasto.Concepto = vm.Concepto.Trim();
            gasto.Importe = vm.Importe;
            gasto.Fecha = vm.Fecha;
            gasto.Notas = string.IsNullOrWhiteSpace(vm.Notas) ? null : vm.Notas.Trim();
            gasto.PagadoPorId = vm.PagadoPorId;

            // Sincronizar participantes: quitar los desmarcados y añadir los nuevos
            var nuevos = vm.ParticipantesIds.Distinct().ToHashSet();
            var aQuitar = gasto.Participantes.Where(p => !nuevos.Contains(p.MiembroId)).ToList();
            _context.GastoParticipantes.RemoveRange(aQuitar);

            var existentes = gasto.Participantes.Select(p => p.MiembroId).ToHashSet();
            foreach (var miembroId in nuevos.Where(m => !existentes.Contains(m)))
                gasto.Participantes.Add(new GastoParticipante { MiembroId = miembroId });

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Repartos", new { id = gasto.RepartoId });
        }

        // POST: Gastos/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var gasto = await GastoDelUsuarioAsync(id);
            if (gasto == null) return NotFound();

            var repartoId = gasto.RepartoId;
            _context.Gastos.Remove(gasto);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Repartos", new { id = repartoId });
        }
    }
}