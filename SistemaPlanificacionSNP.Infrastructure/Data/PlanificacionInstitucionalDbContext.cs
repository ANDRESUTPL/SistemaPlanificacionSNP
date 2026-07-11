using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;

namespace SistemaPlanificacionSNP.Infrastructure.Data;

public partial class PlanificacionInstitucionalDbContext : DbContext
{
    public PlanificacionInstitucionalDbContext(DbContextOptions<PlanificacionInstitucionalDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<PlanesEstrategico> PlanesEstrategicos { get; set; }

    public virtual DbSet<ProyectosInversion> ProyectosInversions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlanesEstrategico>(entity =>
        {
            entity.HasKey(e => e.PlanEstrategicoId).HasName("PK__PlanesEs__0CD7E22770178EFF");

            entity.Property(e => e.Entidad).HasMaxLength(200);
            entity.Property(e => e.Estado).HasMaxLength(30);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<ProyectosInversion>(entity =>
        {
            entity.HasKey(e => e.ProyectoInversionId).HasName("PK__Proyecto__6EC8344371384DDD");

            entity.ToTable("ProyectosInversion");

            entity.HasIndex(e => e.CodigoProyecto, "UQ_PI_Proyectos_Codigo").IsUnique();

            entity.Property(e => e.CodigoProyecto).HasMaxLength(50);
            entity.Property(e => e.Estado).HasMaxLength(30);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Nombre).HasMaxLength(250);

            entity.HasOne(d => d.PlanEstrategico).WithMany(p => p.ProyectosInversions)
                .HasForeignKey(d => d.PlanEstrategicoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PI_Proyectos_Planes");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
