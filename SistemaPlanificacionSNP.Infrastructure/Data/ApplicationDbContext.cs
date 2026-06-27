using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;

namespace SistemaPlanificacionSNP.Infrastructure.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditoriaTransaccional> AuditoriaTransaccionals { get; set; }

    public virtual DbSet<Pantalla> Pantallas { get; set; }

    public virtual DbSet<Rol> Rols { get; set; }

    public virtual DbSet<RolPermiso> RolPermisos { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Usuario1> Usuarios1 { get; set; }

    public virtual DbSet<UsuarioRol> UsuarioRols { get; set; }

    public virtual DbSet<UsuarioRole> UsuarioRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DEPARTAMENTOTI;Database=SNP_Auth;Trusted_Connection=true;Encrypt=false;User Id=AdminSQLUser;Password=1915.*@Ort.;");

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

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("PK__Roles__F92302F1DCECE129");

            entity.HasIndex(e => e.Nombre, "UQ__Roles__75E3EFCF2CF8E5D3").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Nombre).HasMaxLength(60);
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

        modelBuilder.Entity<Usuario1>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("PK__Usuarios__2B3DE7B8F925ED37");

            entity.ToTable("Usuarios");

            entity.HasIndex(e => e.Email, "UQ__Usuarios__A9D105349660851C").IsUnique();

            entity.HasIndex(e => e.Usuario, "UQ__Usuarios__E3237CF7C29C2508").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PasswordHash).HasMaxLength(300);
            entity.Property(e => e.Usuario).HasMaxLength(60);
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

        modelBuilder.Entity<UsuarioRole>(entity =>
        {
            entity.HasKey(e => e.UsuarioRolId).HasName("PK__UsuarioR__C869CDCA1EC74CD7");

            entity.HasIndex(e => new { e.UsuarioId, e.RolId }, "UQ_Auth_UsuarioRoles").IsUnique();

            entity.Property(e => e.FechaAsignacion).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Rol).WithMany(p => p.UsuarioRoles)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Auth_UsuarioRoles_Roles");

            entity.HasOne(d => d.Usuario).WithMany(p => p.UsuarioRoles)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Auth_UsuarioRoles_Usuarios");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
