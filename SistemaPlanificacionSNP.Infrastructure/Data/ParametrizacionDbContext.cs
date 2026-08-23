using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Parametrizacion;

namespace SistemaPlanificacionSNP.Infrastructure.Data
{
	public class ParametrizacionDbContext : DbContext
	{
		public ParametrizacionDbContext(DbContextOptions<ParametrizacionDbContext> options)
			: base(options)
		{
		}

		public DbSet<Catalogo> Catalogos { get; set; }
		public DbSet<ItemCatalogo> ItemsCatalogo { get; set; }
		public DbSet<PeriodoPlanificacion> PeriodosPlanificacion { get; set; }
		public DbSet<EntidadPublica> EntidadesPublicas { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Esto evita el error: "The entity type 'ObjetivoDesarrolloSostenible' requires a primary key"
			modelBuilder.Entity<EntidadPublica>().Ignore(e => e.PlanesEstrategicos);

			modelBuilder.Entity<Catalogo>(entity =>
			{
				entity.ToTable("Catalogos");
				entity.HasKey(e => e.CatalogoId);
				entity.HasIndex(e => e.Codigo).IsUnique();
			});

			modelBuilder.Entity<ItemCatalogo>(entity =>
			{
				entity.ToTable("ItemsCatalogo");
				entity.HasKey(e => e.ItemCatalogoId);
				entity.HasOne(d => d.Catalogo)
					  .WithMany(p => p.Items)
					  .HasForeignKey(d => d.CatalogoId);
			});

			modelBuilder.Entity<PeriodoPlanificacion>(entity =>
			{
				entity.ToTable("PeriodosPlanificacion");
				entity.HasKey(e => e.PeriodoPlanificacionId);
				entity.HasIndex(e => e.Codigo).IsUnique();
			});

			modelBuilder.Entity<EntidadPublica>(entity =>
			{
				entity.ToTable("EntidadesPublicas");
				entity.HasKey(e => e.EntidadPublicaId);
				entity.HasIndex(e => e.Codigo).IsUnique();
				entity.Property(e => e.Tipo).HasMaxLength(100).IsRequired();
				entity.Property(e => e.NivelGobierno).HasMaxLength(100).IsRequired();
				entity.HasOne(d => d.PeriodoPlanificacion)
					  .WithMany(p => p.EntidadesPublicas)
					  .HasForeignKey(d => d.PeriodoPlanificacionId)
					  .OnDelete(DeleteBehavior.Restrict);
			});
		}
	}
}