using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;

namespace SistemaPlanificacionSNP.Infrastructure.Data;

public partial class AuthDbContext : DbContext
{
    public AuthDbContext()
    {
    }

    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditoriaTransaccional> AuditoriaTransaccionals { get; set; }

    public virtual DbSet<Pantalla> Pantallas { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<RolPermiso> RolPermisos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<UsuarioRol> UsuarioRols { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => _ = optionsBuilder.IsConfigured
            ? optionsBuilder
            : optionsBuilder.UseSqlServer("Server=DEPARTAMENTOTI;Database=SNP_Auth;Trusted_Connection=true;Encrypt=false;User Id=AdminSQLUser;Password=1915.*@Ort.;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditoriaTransaccional>(entity =>
        {
            entity.HasKey(e => e.AuditoriaId);

            entity.ToTable("AuditoriaTransaccional");

            entity.HasIndex(e => e.UsuarioId, "IX_AuditoriaTransaccional_UsuarioId");

            entity.Property(e => e.Entidad).HasMaxLength(100);
            entity.Property(e => e.FechaOperacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.TipoOperacion).HasMaxLength(50);

            entity.HasOne(d => d.Usuario).WithMany(p => p.AuditoriaTransaccionals)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditoriaTransaccional_Usuario");
        });

        modelBuilder.Entity<Pantalla>(entity =>
        {
            entity.ToTable("Pantalla");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Icono).HasMaxLength(100);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Ruta).HasMaxLength(256);

            entity.HasOne(d => d.PantallaPadr).WithMany(p => p.InversePantallaPadr)
                .HasForeignKey(d => d.PantallaPadrId)
                .HasConstraintName("FK_Pantalla_PantallaPadre");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Rol");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<RolPermiso>(entity =>
        {
            entity.ToTable("RolPermiso");

            entity.HasIndex(e => e.PantallaId, "IX_RolPermiso_PantallaId");

            entity.HasIndex(e => e.RolId, "IX_RolPermiso_RolId");

            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Lectura).HasDefaultValue(true);

            entity.HasOne(d => d.Pantalla).WithMany(p => p.RolPermisos)
                .HasForeignKey(d => d.PantallaId)
                .HasConstraintName("FK_RolPermiso_Pantalla");

            entity.HasOne(d => d.Rol).WithMany(p => p.RolPermisos)
                .HasForeignKey(d => d.RolId)
                .HasConstraintName("FK_RolPermiso_Rol");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuario");

            entity.HasIndex(e => e.Email, "IX_Usuario_Email").IsUnique();

            entity.HasIndex(e => e.NombreUsuario, "IX_Usuario_NombreUsuario").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.NombreUsuario).HasMaxLength(100);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
        });

        modelBuilder.Entity<UsuarioRol>(entity =>
        {
            entity.ToTable("UsuarioRol");

            entity.HasIndex(e => e.RolId, "IX_UsuarioRol_RolId");

            entity.HasIndex(e => e.UsuarioId, "IX_UsuarioRol_UsuarioId");

            entity.Property(e => e.FechaAsignacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Rol).WithMany(p => p.UsuarioRols)
                .HasForeignKey(d => d.RolId)
                .HasConstraintName("FK_UsuarioRol_Rol");

            entity.HasOne(d => d.Usuario).WithMany(p => p.UsuarioRols)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_UsuarioRol_Usuario");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
