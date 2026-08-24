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

    public virtual DbSet<AuditoriaDocumento> AuditoriaDocumentos { get; set; }

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

            modelBuilder.Entity<AuditoriaDocumento>(entity =>
            {
                entity.HasKey(e => e.AuditoriaDocumentoId).HasName("PK_CC_AuditoriaDocumentos");

                entity.HasIndex(e => e.AuditoriaId, "IX_AuditoriaDocumentos_AuditoriaId");

                entity.Property(e => e.FechaCarga).HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.NombreArchivo).HasMaxLength(255);
                entity.Property(e => e.RutaArchivo).HasMaxLength(500);
                entity.Property(e => e.TipoContenido).HasMaxLength(150);

                entity.HasOne(d => d.Auditoria).WithMany(p => p.Documentos)
                .HasForeignKey(d => d.AuditoriaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CC_AuditoriaDocumentos_Auditorias");
            });

        modelBuilder.Entity<Revisione>(entity =>
        {
            entity.HasKey(e => e.RevisionId).HasName("PK__Revision__B4B1E3D1BF3F9E4C");

            entity.HasIndex(e => e.CodigoRevision, "UQ__Revision__017ED8F8897D82F0").IsUnique();
            entity.HasIndex(e => e.PlanEstrategicoId, "IX_Revisiones_PlanEstrategicoId");
            entity.HasIndex(e => e.ProyectoInversionId, "IX_Revisiones_ProyectoInversionId");
            entity.HasIndex(e => e.EntidadPublicaId, "IX_Revisiones_EntidadPublicaId");

            entity.Property(e => e.CodigoRevision).HasMaxLength(40);
            entity.Property(e => e.Estado).HasMaxLength(30);
            entity.Property(e => e.FechaRevision).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Modulo).HasMaxLength(100);
            entity.Property(e => e.PlanEstrategicoId);
            entity.Property(e => e.ProyectoInversionId);
            entity.Property(e => e.EntidadPublicaId);
            entity.Property(e => e.EntidadNombre).HasMaxLength(200);
            entity.Property(e => e.CodigoProyecto).HasMaxLength(50);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
