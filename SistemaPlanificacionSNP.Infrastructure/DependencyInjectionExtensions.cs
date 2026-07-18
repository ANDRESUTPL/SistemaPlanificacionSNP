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
	/// separadas por Microservicio
	/// </summary>
	public static class DependencyInjectionExtensions
	{
		/// <summary>
		/// Registra servicios ESPECÍFICOS para el microservicio de Autenticación/Seguridad (Auth.Api)
		/// </summary>
		public static IServiceCollection AddAuthInfrastructureServices(this IServiceCollection services, string connectionString)
		{
			// DbContext de Seguridad
			services.AddDbContext<AuthDbContext>(options =>
			options.UseSqlServer(connectionString, sqlOptions =>
			{
				sqlOptions.MigrationsAssembly("SistemaPlanificacionSNP.Infrastructure");
				sqlOptions.EnableRetryOnFailure(3);
			})
		);

			// Repositorios de Seguridad
			services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
			services.AddScoped<IUsuarioRepository, UsuarioRepository>();
			services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

			// Este repositorio de Planificacion parece legado en el Auth, 
			// pero lo dejamos por si la lógica actual del UnitOfWork de Auth lo requiere.
			services.AddScoped<IPlanificacionRepository, PlanificacionRepository>();

			// Unit of Work de Seguridad
			services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

			// Servicios Base
			services.AddScoped<IPasswordHashService, PasswordHashService>();
			services.AddScoped<IAuditoriaService, AuditoriaService>();
			services.AddScoped<IMenuService, MenuService>();

			return services;
		}

		/// <summary>
		/// Registra servicios ESPECÍFICOS del módulo de MacroPlanificación.
		/// Requiere que MacroPlanificacionDbContext sea registrado en el Program.cs de su API.
		/// </summary>
		public static IServiceCollection AddMacroPlanificacionServices(this IServiceCollection services)
		{
			services.AddScoped<IPlanesNacionalesDesarrolloRepository, PlanesNacionalesDesarrolloRepository>();
			services.AddScoped<IObjetivosEstrategicoRepository, ObjetivosEstrategicoRepository>();
			services.AddScoped<IMacroPlanificacionUnitOfWork, MacroPlanificacionUnitOfWork>();

			// Nota: Estos servicios deberían estar idealmente en Application o registrados directamente en el API.
			// Los dejo aquí para no romper tu arquitectura actual.
			// services.AddScoped<IMacroPlanNacionalService, MacroPlanNacionalService>();
			// services.AddScoped<IMacroObjetivoEstrategicoService, MacroObjetivoEstrategicoService>();

			return services;
		}

		/// <summary>
		/// Registra servicios ESPECÍFICOS del módulo de Parametrización.
		/// </summary>
		public static IServiceCollection AddParametrizacionServices(this IServiceCollection services, string connectionString)
		{
			// REGISTRA EL CONTEXTO CORRECTO PARA ESTE MICROSERVICIO
			services.AddDbContext<ParametrizacionDbContext>(options =>
				options.UseSqlServer(connectionString, sqlOptions =>
				{
					sqlOptions.MigrationsAssembly("SistemaPlanificacionSNP.Infrastructure");
					sqlOptions.EnableRetryOnFailure(3);
				})
			);
			return services;
		}
	}
}