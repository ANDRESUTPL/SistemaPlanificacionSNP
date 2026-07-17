using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Infrastructure
{
    /// <summary>
    /// Extensiones para registrar servicios de infraestructura en DI
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Registra todos los servicios de infraestructura
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
        {
            // DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("SistemaPlanificacionSNP.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                })
            );

            // Repositorios
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
            services.AddScoped<IPlanificacionRepository, PlanificacionRepository>();

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

            // Servicios
            services.AddScoped<IPasswordHashService, PasswordHashService>();
            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddScoped<IMenuService, MenuService>();

            return services;
        }

        /// <summary>
        /// Registra servicios específicos del módulo de MacroPlanificación.
        /// Requiere que MacroPlanificacionDbContext sea registrado por la API.
        /// </summary>
        public static IServiceCollection AddMacroPlanificacionServices(this IServiceCollection services)
        {
            services.AddScoped<IPlanesNacionalesDesarrolloRepository, PlanesNacionalesDesarrolloRepository>();
            services.AddScoped<IObjetivosEstrategicoRepository, ObjetivosEstrategicoRepository>();
            services.AddScoped<IMacroPlanificacionUnitOfWork, MacroPlanificacionUnitOfWork>();
            services.AddScoped<IMacroPlanNacionalService, MacroPlanNacionalService>();
            services.AddScoped<IMacroObjetivoEstrategicoService, MacroObjetivoEstrategicoService>();

            return services;
        }
    }
}
