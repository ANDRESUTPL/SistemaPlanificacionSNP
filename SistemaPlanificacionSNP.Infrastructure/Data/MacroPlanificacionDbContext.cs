using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;

namespace SistemaPlanificacionSNP.Infrastructure.Data;

public partial class MacroPlanificacionDbContext : DbContext
{
    public MacroPlanificacionDbContext()
    {
    }

    public MacroPlanificacionDbContext(DbContextOptions<MacroPlanificacionDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ObjetivosEstrategico> ObjetivosEstrategicos { get; set; }

    public virtual DbSet<PlanesNacionalesDesarrollo> PlanesNacionalesDesarrollos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DEPARTAMENTOTI;Database=SNP_MacroPlanificacion;Trusted_Connection=true;Encrypt=false;User Id=AdminSQLUser;Password=1915.*@Ort.;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ObjetivosEstrategico>(entity =>
        {
            entity.HasKey(e => e.ObjetivoEstrategicoId).HasName("PK__Objetivo__C5D9E69F50C86578");

            entity.HasIndex(e => new { e.PlanNacionalId, e.Codigo }, "UQ_Macro_Objetivos_Codigo").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(30);
            entity.Property(e => e.Descripcion).HasMaxLength(600);
            entity.Property(e => e.Nombre).HasMaxLength(300);

            entity.HasOne(d => d.PlanNacional).WithMany(p => p.ObjetivosEstrategicos)
                .HasForeignKey(d => d.PlanNacionalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Macro_Objetivos_PlanNacional");
        });

        modelBuilder.Entity<PlanesNacionalesDesarrollo>(entity =>
        {
            entity.HasKey(e => e.PlanNacionalId).HasName("PK__PlanesNa__676755B3D50B4609");

            entity.ToTable("PlanesNacionalesDesarrollo");

            entity.Property(e => e.Estado).HasMaxLength(30);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(200);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
