using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebViajes.Models;

namespace WebViajes.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    //migracion de presupuestos//
    public DbSet<Presupuesto> Presupuestos { get; set; }
    public DbSet<Partida> Partidas { get; set; }
    public DbSet<Subpartida> Subpartidas { get; set; }
    public DbSet<Participante> Participantes { get; set; }

    //migracion de reparto de gastod//
    public DbSet<Reparto> Repartos { get; set; }
    public DbSet<Miembro> Miembros { get; set; }
    public DbSet<Gasto> Gastos { get; set; }
    public DbSet<GastoParticipante> GastoParticipantes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Si borras un reparto, se borran sus miembros y gastos
        builder.Entity<Reparto>()
            .HasMany(r => r.Miembros).WithOne(m => m.Reparto)
            .HasForeignKey(m => m.RepartoId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Reparto>()
            .HasMany(r => r.Gastos).WithOne(g => g.Reparto)
            .HasForeignKey(g => g.RepartoId).OnDelete(DeleteBehavior.Cascade);

        // No se puede borrar un miembro que haya pagado un gasto
        builder.Entity<Gasto>()
            .HasOne(g => g.PagadoPor).WithMany()
            .HasForeignKey(g => g.PagadoPorId).OnDelete(DeleteBehavior.Restrict);

        // Al borrar un gasto, se borran sus participantes
        builder.Entity<GastoParticipante>()
            .HasOne(p => p.Gasto).WithMany(g => g.Participantes)
            .HasForeignKey(p => p.GastoId).OnDelete(DeleteBehavior.Cascade);

        // No se puede borrar un miembro que participe en algún gasto
        builder.Entity<GastoParticipante>()
            .HasOne(p => p.Miembro).WithMany()
            .HasForeignKey(p => p.MiembroId).OnDelete(DeleteBehavior.Restrict);

        // Un miembro no puede estar dos veces en el mismo gasto
        builder.Entity<GastoParticipante>()
            .HasIndex(p => new { p.GastoId, p.MiembroId }).IsUnique();
    }
}
