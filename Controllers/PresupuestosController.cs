using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebViajes.Data;
using WebViajes.Models;
using QuestPDF.Fluent;
using WebViajes.Services;

namespace WebViajes.Controllers;

[Authorize]
public class PresupuestosController : Controller
{
    private readonly ApplicationDbContext _context;

    public PresupuestosController(ApplicationDbContext context)
    {
        _context = context;
    }

    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var presupuestos = await _context.Presupuestos
            .Where(p => p.UsuarioId == UsuarioId)
            .Include(p => p.Partidas).ThenInclude(p => p.Detalles)
            .Include(p => p.Participantes)
            .ToListAsync();

        return View(presupuestos);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(string nombre, string? notas)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            ModelState.AddModelError("", "Ponle un nombre al presupuesto.");
            return View();
        }

        var presupuesto = new Presupuesto
        {
            Nombre = nombre.Trim(),
            Notas = notas,
            UsuarioId = UsuarioId
        };

        _context.Presupuestos.Add(presupuesto);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Editar), new { id = presupuesto.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var presupuesto = await _context.Presupuestos
            .Include(p => p.Partidas).ThenInclude(p => p.Detalles)
            .Include(p => p.Participantes)
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == UsuarioId);

        if (presupuesto == null)
            return Content($"No encontrado. Id={id}, usuario={UsuarioId}");

        return View(presupuesto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar([FromBody] PresupuestoDto dto)
    {
        if (dto == null) return BadRequest();

        var presupuesto = await _context.Presupuestos
            .Include(p => p.Partidas).ThenInclude(p => p.Detalles)
            .Include(p => p.Participantes)
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.UsuarioId == UsuarioId);

        if (presupuesto == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Nombre))
            presupuesto.Nombre = dto.Nombre.Trim();
        presupuesto.Notas = dto.Notas;

        _context.Subpartidas.RemoveRange(presupuesto.Partidas.SelectMany(p => p.Detalles));
        _context.Partidas.RemoveRange(presupuesto.Partidas);
        _context.Participantes.RemoveRange(presupuesto.Participantes);

        foreach (var p in dto.Partidas)
        {
            presupuesto.Partidas.Add(new Partida
            {
                Concepto = p.Concepto?.Trim() ?? "",
                ImporteDirecto = p.Detalles.Any() ? null : p.ImporteDirecto,
                Nota = p.Nota,
                Detalles = p.Detalles.Select(d => new Subpartida
                {
                    Concepto = d.Concepto?.Trim() ?? "",
                    Importe = d.Importe ?? 0,
                    Nota = d.Nota
                }).ToList()
            });
        }

        foreach (var nombre in dto.Participantes.Where(n => !string.IsNullOrWhiteSpace(n)))
            presupuesto.Participantes.Add(new Participante { Nombre = nombre.Trim() });

        await _context.SaveChangesAsync();
        return Ok();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrar(int id)
    {
        var presupuesto = await _context.Presupuestos
            .Include(p => p.Partidas).ThenInclude(p => p.Detalles)
            .Include(p => p.Participantes)
            .FirstOrDefaultAsync(p => p.Id == id && p.UsuarioId == UsuarioId);

        if (presupuesto == null) return NotFound();

        _context.Subpartidas.RemoveRange(presupuesto.Partidas.SelectMany(p => p.Detalles));
        _context.Partidas.RemoveRange(presupuesto.Partidas);
        _context.Participantes.RemoveRange(presupuesto.Participantes);
        _context.Presupuestos.Remove(presupuesto);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Pdf(int id) //accion que conviertee los presupuestos a PDF
    {
        var presupuesto = await _context.Presupuestos
            .Include(p => p.Partidas).ThenInclude(x => x.Detalles)
            .Include(p => p.Participantes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (presupuesto == null) return NotFound();

        var bytes = new PresupuestoPdf(presupuesto).GeneratePdf();
        Response.Headers["Content-Disposition"] =
             $"inline; filename=\"Presupuesto_{presupuesto.Nombre}.pdf\"";
        return File(bytes, "application/pdf");
    }
}

