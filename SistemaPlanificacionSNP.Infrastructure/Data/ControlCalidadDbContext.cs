using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;

namespace SistemaPlanificacionSNP.Infrastructure.Data;

public partial class ControlCalidadDbContext : DbContext
{
    public ControlCalidadDbContext()
    {
    }

    public ControlCalidadDbContext(DbContextOptions<ControlCalidadDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Auditoria> Auditorias { get; set; }

    public virtual DbSet<Revisione> Revisiones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Auditoria>(entity =>
        {
            entity.HasKey(e => e.AuditoriaId).HasName("PK__Auditori__095694C3FAF8D0EB");

            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Responsable).HasMaxLength(120);
            entity.Property(e => e.Resultado).HasMaxLength(30);
            entity.Property(e => e.Tipo).HasMaxLength(50);

            entity.HasOne(d => d.Revision).WithMany(p => p.Auditoria)
                .HasForeignKey(d => d.RevisionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CC_Auditorias_Revisiones");
        });

        modelBuilder.Entity<Revisione>(entity =>
        {
            entity.HasKey(e => e.RevisionId).HasName("PK__Revision__B4B1E3D1BF3F9E4C");

            entity.HasIndex(e => e.CodigoRevision, "UQ__Revision__017ED8F8897D82F0").IsUnique();

            entity.Property(e => e.CodigoRevision).HasMaxLength(40);
            entity.Property(e => e.Estado).HasMaxLength(30);
            entity.Property(e => e.FechaRevision).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Modulo).HasMaxLength(100);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
