using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Domain.Entities.Parametrizacion;
using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;
using SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;

namespace SistemaPlanificacionSNP.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        #region DbSets - Seguridad
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<UsuarioRol> UsuarioRoles { get; set; }
        public DbSet<Pantalla> Pantallas { get; set; }
        public DbSet<RolPermiso> RolPermisos { get; set; }
        public DbSet<AuditoriaTransaccional> Auditorias { get; set; }
        #endregion

        #region DbSets - Parametrizacion
        public DbSet<PeriodoPlanificacion> PeriodosPlanificacion { get; set; }
        public DbSet<EntidadPublica> EntidadesPublicas { get; set; }
        public DbSet<Catalogo> Catalogos { get; set; }
        public DbSet<ItemCatalogo> ItemsCatalogo { get; set; }
        #endregion

        #region DbSets - MacroPlanificacion
        public DbSet<ObjetivoDesarrolloSostenible> ObjetivosDesarrolloSostenible { get; set; }
        public DbSet<PlanNacionalDesarrollo> PlanesNacionalesDesarrollo { get; set; }
        #endregion

        #region DbSets - PlanificacionInstitucional
        public DbSet<PlanEstrategicoInstitucional> PlanesEstrategicos { get; set; }
        public DbSet<ObjetivoEstrategico> ObjetivosEstrategicos { get; set; }
        public DbSet<ProgramaPresupuestario> ProgramasPresupuestarios { get; set; }
        public DbSet<MatrizIndicador> MatricesIndicadores { get; set; }
        public DbSet<MetaTerritorial> MetasTerritorial { get; set; }
        public DbSet<ProyectoInversion> ProyectosInversion { get; set; }
        public DbSet<RevisionSNP> Revisiones { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ====== CONFIGURACIÓN SEGURIDAD ======
            ConfigurarUsuario(modelBuilder);
            ConfigurarRol(modelBuilder);
            ConfigurarUsuarioRol(modelBuilder);
            ConfigurarPantalla(modelBuilder);
            ConfigurarRolPermiso(modelBuilder);
            ConfigurarAuditoriaTransaccional(modelBuilder);

            // ====== CONFIGURACIÓN PARAMETRIZACION ======
            ConfigurarPeriodoPlanificacion(modelBuilder);
            ConfigurarEntidadPublica(modelBuilder);
            ConfigurarCatalogo(modelBuilder);
            ConfigurarItemCatalogo(modelBuilder);

            // ====== CONFIGURACIÓN MACROPLANIFICACION ======
            ConfigurarObjetivoDesarrolloSostenible(modelBuilder);
            ConfigurarPlanNacionalDesarrollo(modelBuilder);

            // ====== CONFIGURACIÓN PLANIFICACION INSTITUCIONAL ======
            ConfigurarPlanEstrategicoInstitucional(modelBuilder);
            ConfigurarObjetivoEstrategico(modelBuilder);
            ConfigurarProgramaPresupuestario(modelBuilder);
            ConfigurarMatrizIndicador(modelBuilder);
            ConfigurarMetaTerritorial(modelBuilder);
            ConfigurarProyectoInversion(modelBuilder);
            ConfigurarRevisionSNP(modelBuilder);
        }

        #region Configuraciones - Seguridad
        private void ConfigurarUsuario(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.UsuarioId);
                entity.Property(e => e.NombreUsuario).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Apellido).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RefreshToken).HasMaxLength(500);
                
                entity.HasIndex(e => e.NombreUsuario).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                entity.HasMany(e => e.UsuarioRoles)
                    .WithOne(ur => ur.Usuario)
                    .HasForeignKey(ur => ur.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Auditorias)
                    .WithOne(a => a.Usuario)
                    .HasForeignKey(a => a.UsuarioId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private void ConfigurarRol(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.HasKey(e => e.RolId);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Descripcion).HasMaxLength(255);
                entity.HasIndex(e => e.Nombre).IsUnique();

                entity.HasMany(e => e.UsuarioRoles)
                    .WithOne(ur => ur.Rol)
                    .HasForeignKey(ur => ur.RolId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.RolPermisos)
                    .WithOne(rp => rp.Rol)
                    .HasForeignKey(rp => rp.RolId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigurarUsuarioRol(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioRol>(entity =>
            {
                entity.HasKey(e => e.UsuarioRolId);
                entity.HasIndex(e => new { e.UsuarioId, e.RolId }).IsUnique();
            });
        }

        private void ConfigurarPantalla(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pantalla>(entity =>
            {
                entity.HasKey(e => e.PantallaId);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Ruta).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Icono).HasMaxLength(50);

                entity.HasOne(e => e.PantallaPadre)
                    .WithMany(p => p.PantallasHijas)
                    .HasForeignKey(e => e.PantallaPadrId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(e => e.RolPermisos)
                    .WithOne(rp => rp.Pantalla)
                    .HasForeignKey(rp => rp.PantallaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigurarRolPermiso(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RolPermiso>(entity =>
            {
                entity.HasKey(e => e.RolPermisoId);
                entity.HasIndex(e => new { e.RolId, e.PantallaId }).IsUnique();
            });
        }

        private void ConfigurarAuditoriaTransaccional(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditoriaTransaccional>(entity =>
            {
                entity.HasKey(e => e.AuditoriaId);
                entity.Property(e => e.Entidad).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TipoOperacion).IsRequired().HasMaxLength(20);
                entity.Property(e => e.DatosAnteriores).HasColumnType("nvarchar(max)");
                entity.Property(e => e.DatosNuevos).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Descripcion).HasMaxLength(500);
            });
        }
        #endregion

        #region Configuraciones - Parametrizacion
        private void ConfigurarPeriodoPlanificacion(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PeriodoPlanificacion>(entity =>
            {
                entity.HasKey(e => e.PeriodoPlanificacionId);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Codigo).IsUnique();

                entity.HasMany(e => e.EntidadesPublicas)
                    .WithOne(ep => ep.PeriodoPlanificacion)
                    .HasForeignKey(ep => ep.PeriodoPlanificacionId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private void ConfigurarEntidadPublica(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EntidadPublica>(entity =>
            {
                entity.HasKey(e => e.EntidadPublicaId);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Sigla).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Mision).IsRequired().HasColumnType("nvarchar(max)");
                entity.HasIndex(e => e.Codigo).IsUnique();

                entity.HasMany(e => e.PlanesEstrategicos)
                    .WithOne(pei => pei.EntidadPublica)
                    .HasForeignKey(pei => pei.EntidadPublicaId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private void ConfigurarCatalogo(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Catalogo>(entity =>
            {
                entity.HasKey(e => e.CatalogoId);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descripcion).HasMaxLength(255);
                entity.HasIndex(e => e.Codigo).IsUnique();

                entity.HasMany(e => e.Items)
                    .WithOne(ic => ic.Catalogo)
                    .HasForeignKey(ic => ic.CatalogoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigurarItemCatalogo(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ItemCatalogo>(entity =>
            {
                entity.HasKey(e => e.ItemCatalogoId);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descripcion).HasMaxLength(255);
                entity.HasIndex(e => new { e.CatalogoId, e.Codigo }).IsUnique();
            });
        }
        #endregion

        #region Configuraciones - MacroPlanificacion
        private void ConfigurarObjetivoDesarrolloSostenible(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ObjetivoDesarrolloSostenible>(entity =>
            {
                entity.HasKey(e => e.OdsId);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Descripcion).HasColumnType("nvarchar(max)");
                entity.HasIndex(e => e.Codigo).IsUnique();

                entity.HasMany(e => e.PlanesNacionales)
                    .WithOne(pnd => pnd.Ods)
                    .HasForeignKey(pnd => pnd.OdsId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }

        private void ConfigurarPlanNacionalDesarrollo(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlanNacionalDesarrollo>(entity =>
            {
                entity.HasKey(e => e.PndId);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Descripcion).HasColumnType("nvarchar(max)");
                entity.HasIndex(e => e.Codigo).IsUnique();
            });
        }
        #endregion

        #region Configuraciones - PlanificacionInstitucional
        private void ConfigurarPlanEstrategicoInstitucional(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlanEstrategicoInstitucional>(entity =>
            {
                entity.HasKey(e => e.PeiId);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Descripcion).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Estado).IsRequired().HasMaxLength(20);
                entity.HasIndex(e => e.Codigo).IsUnique();

                entity.HasMany(e => e.ObjetivosEstrategicos)
                    .WithOne(oei => oei.PlanEstrategico)
                    .HasForeignKey(oei => oei.PeiId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Revisiones)
                    .WithOne(r => r.PlanEstrategico)
                    .HasForeignKey(r => r.PeiId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigurarObjetivoEstrategico(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ObjetivoEstrategico>(entity =>
            {
                entity.HasKey(e => e.OeiId);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Descripcion).HasColumnType("nvarchar(max)");

                entity.HasMany(e => e.ProgramasPresupuestarios)
                    .WithOne(pp => pp.ObjetivoEstrategico)
                    .HasForeignKey(pp => pp.OeiId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigurarProgramaPresupuestario(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProgramaPresupuestario>(entity =>
            {
                entity.HasKey(e => e.ProgramaId);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Descripcion).HasColumnType("nvarchar(max)");
                entity.Property(e => e.PresupuestoAsignado).HasPrecision(18, 2);

                entity.HasMany(e => e.MatricesIndicadores)
                    .WithOne(mi => mi.ProgramaPresupuestario)
                    .HasForeignKey(mi => mi.ProgramaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.ProyectosInversion)
                    .WithOne(pi => pi.ProgramaPresupuestario)
                    .HasForeignKey(pi => pi.ProgramaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigurarMatrizIndicador(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MatrizIndicador>(entity =>
            {
                entity.HasKey(e => e.MatrizIndicadorId);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Descripcion).HasColumnType("nvarchar(max)");
                entity.Property(e => e.TipoIndicador).HasMaxLength(50);
                entity.Property(e => e.Unidad).HasMaxLength(50);
                entity.Property(e => e.ValorBase).HasPrecision(18, 4);
                entity.Property(e => e.ValorMeta).HasPrecision(18, 4);

                entity.HasMany(e => e.MetasTerritorial)
                    .WithOne(mt => mt.MatrizIndicador)
                    .HasForeignKey(mt => mt.MatrizIndicadorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigurarMetaTerritorial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MetaTerritorial>(entity =>
            {
                entity.HasKey(e => e.MetaTerritorialId);
                entity.Property(e => e.Territorio).IsRequired().HasMaxLength(100);
                entity.Property(e => e.MetaFisica).HasPrecision(18, 4);
                entity.Property(e => e.MetaFinanciera).HasPrecision(18, 2);
            });
        }

        private void ConfigurarProyectoInversion(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProyectoInversion>(entity =>
            {
                entity.HasKey(e => e.ProyectoId);
                entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Descripcion).HasColumnType("nvarchar(max)");
                entity.Property(e => e.CostoTotal).HasPrecision(18, 2);
                entity.Property(e => e.Estado).IsRequired().HasMaxLength(20);
                entity.HasIndex(e => e.Codigo).IsUnique();
            });
        }

        private void ConfigurarRevisionSNP(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RevisionSNP>(entity =>
            {
                entity.HasKey(e => e.RevisionId);
                entity.Property(e => e.Estado).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Comentarios).HasColumnType("nvarchar(max)");
            });
        }
        #endregion
    }
}
