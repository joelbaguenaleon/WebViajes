using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using WebViajes.Data;
using WebViajes.Models;
using WebViajes.Services;

namespace WebViajes.Controllers
{
    [Authorize]
    public class RepartosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RepartoService _service;

        public RepartosController(ApplicationDbContext context, RepartoService service)
        {
            _context = context;
            _service = service;
        }

        private string UsuarioActualId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        // Solo devuelve el reparto si pertenece al usuario logueado
        private async Task<Reparto?> ObtenerRepartoAsync(int id)
        {
            return await _context.Repartos
                .Include(r => r.Miembros)
                .Include(r => r.Gastos).ThenInclude(g => g.PagadoPor)
                .Include(r => r.Gastos).ThenInclude(g => g.Participantes).ThenInclude(p => p.Miembro)
                .FirstOrDefaultAsync(r => r.Id == id && r.UsuarioId == UsuarioActualId);
        }

        // GET: Repartos
        public async Task<IActionResult> Index()
        {
            var repartos = await _context.Repartos
                .Include(r => r.Miembros)
                .Include(r => r.Gastos)
                .Where(r => r.UsuarioId == UsuarioActualId)
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();

            return View(repartos);
        }

        // GET: Repartos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var resumen = await _service.ObtenerResumenAsync(id, UsuarioActualId);
            if (resumen == null) return NotFound();

            return View(resumen);
        }

        // GET: Repartos/Pdf/5
        public async Task<IActionResult> Pdf(int id)
        {
            var resumen = await _service.ObtenerResumenAsync(id, UsuarioActualId);
            if (resumen == null) return NotFound();

            var bytes = new RepartoPdf(resumen).GeneratePdf();


            return File(bytes, "application/pdf");
        }

        // GET: Repartos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Repartos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre")] Reparto reparto)
        {
            if (!ModelState.IsValid) return View(reparto);

            reparto.UsuarioId = UsuarioActualId;
            reparto.FechaCreacion = DateTime.Now;

            _context.Repartos.Add(reparto);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = reparto.Id });
        }

        // GET: Repartos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var reparto = await ObtenerRepartoAsync(id);
            if (reparto == null) return NotFound();

            return View(reparto);
        }

        // POST: Repartos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre")] Reparto datos)
        {
            if (id != datos.Id) return NotFound();

            var reparto = await ObtenerRepartoAsync(id);
            if (reparto == null) return NotFound();

            if (!ModelState.IsValid) return View(reparto);

            reparto.Nombre = datos.Nombre;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Repartos/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var reparto = await ObtenerRepartoAsync(id);
            if (reparto == null) return NotFound();

            return View(reparto);
        }

        // POST: Repartos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reparto = await ObtenerRepartoAsync(id);
            if (reparto == null) return NotFound();

            _context.Repartos.Remove(reparto);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}