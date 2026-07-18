using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.JWT;
using SistemaPlanificacionSNP.Infrastructure.Mapping;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;
using System.Text;

internal class Program
{
	private static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		// ==================== CONFIGURACIÓN ====================

		// Obtener cadena de conexión
		var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
			?? throw new InvalidOperationException("Cadena de conexión 'DefaultConnection' no configurada");

		// Obtener configuración JWT
		var jwtSection = builder.Configuration.GetSection("Jwt");
		var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings
		{
			SecretKey = "eG95U3VyZ3BKSFFXbTV6S2RzN3Z3YlhjTjVuN3BYM2I4Y0RvY0E0bXpVbz0",
			Issuer = "SistemaPlanificacionSNP",
			Audience = "SistemaPlanificacionSNP",
			ExpirationMinutes = 60,
			RefreshTokenExpirationDays = 7
		};

		// ==================== SERVICIOS DE INFRAESTRUCTURA ====================

		// Entity Framework DbContext
		builder.Services.AddDbContext<AuthDbContext>(options =>
		{
			options.UseSqlServer(
				connectionString,
				sqlOptions =>
				{
					sqlOptions.MigrationsAssembly("SistemaPlanificacionSNP.Infrastructure");
					sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
				}
			);

			if (builder.Environment.IsDevelopment())
			{
				options.EnableDetailedErrors();
				options.EnableSensitiveDataLogging();
			}
		}
		);
		// AutoMapper
		//builder.Services.AddAutoMapper(typeof(MappingProfile));
		//builder.Services.AddAutoMapper(typeof(Program).Assembly);

		builder.Services.AddAutoMapper(config =>
		{
			config.AddProfile<MappingProfile>(); // Reemplaza MappingProfile con el nombre exacto de tu clase si es diferente
		});

		// JWT Settings y Token Generator
		builder.Services.AddSingleton(jwtSettings);
		builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

		// Servicios de negocio
		builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
		builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
		builder.Services.AddScoped<IMenuService, MenuService>();

		// Patrón Repository y Unit of Work
		builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
		builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
		builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
		builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
		builder.Services.AddScoped<IPlanificacionRepository, PlanificacionRepository>();

		// ==================== AUTENTICACIÓN Y AUTORIZACIÓN ====================

		builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
					ValidateIssuer = true,
					ValidIssuer = jwtSettings.Issuer,
					ValidateAudience = true,
					ValidAudience = jwtSettings.Audience,
					ValidateLifetime = true,
					ClockSkew = TimeSpan.Zero
				};

				options.Events = new JwtBearerEvents
				{
					OnAuthenticationFailed = context =>
					{
						if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
						{
							context.Response.Headers.Append("Token-Expired", "true");
						}
						return Task.CompletedTask;
					},
					OnTokenValidated = context =>
					{
						var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
						logger.LogInformation("Token validado");
						return Task.CompletedTask;
					}
				};
			});

		// ==================== CONTROLADORES Y API ====================

		builder.Services.AddControllers();
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen(options =>
		{
			options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
			{
				Title = "Auth API - Sistema de Planificación SNP",
				Version = "v1",
				Description = "API de autenticación y gestión de usuarios"
			});

			// Seguridad JWT en Swagger
			options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
			{
				Description = "JWT Authorization header usando el esquema Bearer",
				Name = "Authorization",
				In = Microsoft.OpenApi.Models.ParameterLocation.Header,
				Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
				Scheme = "Bearer",
				BearerFormat = "JWT"
			});

			options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
			{
		{
			new Microsoft.OpenApi.Models.OpenApiSecurityScheme
			{
				Reference = new Microsoft.OpenApi.Models.OpenApiReference
				{
					Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			new string[] { }
		}
			});
		});

		// CORS
		builder.Services.AddCors(options =>
		{
			options.AddPolicy("AllowFrontend", policy =>
			{
				policy.WithOrigins("http://localhost:3000", "https://localhost:7010", "http://localhost:5000", "http://localhost:52550")
					  .AllowAnyMethod()
					  .AllowAnyHeader()
					  .AllowCredentials();
			});
		});

		// ==================== APLICACIÓN ====================

		var app = builder.Build();

		using (var scope = app.Services.CreateScope())
		{
			var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
			var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
			var autoMigrate = builder.Configuration.GetValue<bool>("DatabaseMigration:AutoMigrateOnStartup");

			try
			{
				var sqlBuilder = new SqlConnectionStringBuilder(connectionString)
				{
					Password = "***"
				};

				logger.LogInformation("Auth API DB Connection: {Connection}", sqlBuilder.ConnectionString);
				logger.LogInformation(
					"Auth API DB Target => Server: {Server}, Database: {Database}",
					dbContext.Database.GetDbConnection().DataSource,
					dbContext.Database.GetDbConnection().Database
				);

				if (autoMigrate)
				{
					dbContext.Database.Migrate();
					logger.LogInformation("DatabaseMigration: se aplicaron migraciones pendientes en startup.");
				}

				var appliedMigrations = dbContext.Database.GetAppliedMigrations().ToList();
				var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

				logger.LogInformation(
					"Migraciones aplicadas: {CountApplied}; pendientes: {CountPending}",
					appliedMigrations.Count,
					pendingMigrations.Count
				);

				if (pendingMigrations.Count > 0)
				{
					logger.LogWarning("Migraciones pendientes detectadas: {PendingList}", string.Join(", ", pendingMigrations));
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "No se pudo validar la conexión/migraciones de base de datos al inicio.");
			}
		}

		// Swagger en desarrollo
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI(c =>
			{
				c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1");
				c.RoutePrefix = string.Empty;
			});
		}

		// HTTPS redirection
		app.UseHttpsRedirection();

		// CORS
		app.UseCors("AllowFrontend");

		// Autenticación y autorización
		app.UseAuthentication();
		app.UseAuthorization();

		// Mapear controladores
		app.MapControllers();

		// Middleware de error global
		app.Use(async (context, next) =>
		{
			try
			{
				await next();
			}
			catch (Exception ex)
			{
				var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
				logger.LogError(ex, "Excepción no manejada");

				context.Response.StatusCode = StatusCodes.Status500InternalServerError;
				context.Response.ContentType = "application/json";

				await context.Response.WriteAsJsonAsync(new
				{
					success = false,
					message = "Error interno del servidor",
					error = app.Environment.IsDevelopment() ? ex.Message : null,
					timestamp = DateTime.UtcNow
				});
			}
		});

		// Health check
		app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

		// Ejecutar
		app.Run();
	}
}