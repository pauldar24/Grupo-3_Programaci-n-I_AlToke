using GRUPAL.Models;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<ObjetoPerdido> ObjetosPerdidos => Set<ObjetoPerdido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Correo).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Contraseña).IsRequired();
            entity.HasIndex(u => u.Correo).IsUnique();
        });

        modelBuilder.Entity<ObjetoPerdido>(entity =>
        {
            entity.Property(o => o.Título).IsRequired().HasMaxLength(150);
            entity.Property(o => o.Categoría).IsRequired().HasMaxLength(100);
            entity.Property(o => o.Descripcion).IsRequired();
            entity.Property(o => o.Ubicacion).IsRequired().HasMaxLength(200);
            entity.Property(o => o.Fecha).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(o => o.Usuario)
                  .WithMany(u => u.ObjetosPerdidos)
                  .HasForeignKey(o => o.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}