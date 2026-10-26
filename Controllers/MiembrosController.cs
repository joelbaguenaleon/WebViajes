using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebViajes.Data;
using WebViajes.Models;

namespace WebViajes.Controllers
{
    [Authorize]
    public class MiembrosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MiembrosController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string UsuarioActualId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int repartoId, string? nombre)
        {
            var reparto = await _context.Repartos
                .Include(r => r.Miembros)
                .FirstOrDefaultAsync(r => r.Id == repartoId && r.UsuarioId == UsuarioActualId);
            if (reparto == null) return NotFound();

            nombre = (nombre ?? "").Trim();

            if (nombre.Length == 0 || nombre.Length > 60)
            {
                TempData["Error"] = "El nombre del miembro debe tener entre 1 y 60 caracteres.";
            }
            else if (reparto.Miembros.Any(m => m.Nombre.ToLower() == nombre.ToLower()))
            {
                TempData["Error"] = "Ya hay un miembro con ese nombre.";
            }
            else
            {
                reparto.Miembros.Add(new Miembro { Nombre = nombre });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Repartos", new { id = repartoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var miembro = await _context.Miembros
                .Include(m => m.Reparto)
                .FirstOrDefaultAsync(m => m.Id == id && m.Reparto!.UsuarioId == UsuarioActualId);
            if (miembro == null) return NotFound();

            var repartoId = miembro.RepartoId;

            // La base de datos no deja borrar miembros que aparecen en gastos
            bool enUso = await _context.Gastos.AnyAsync(g => g.PagadoPorId == id)
                      || await _context.GastoParticipantes.AnyAsync(p => p.MiembroId == id);

            if (enUso)
            {
                TempData["Error"] = $"No se puede borrar a {miembro.Nombre}: aparece en algún gasto. Edita o borra antes esos gastos.";
            }
            else
            {
                _context.Miembros.Remove(miembro);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Repartos", new { id = repartoId });
        }
    }
}