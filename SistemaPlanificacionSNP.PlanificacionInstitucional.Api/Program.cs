using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.JWT;
using SistemaPlanificacionSNP.Infrastructure.Mapping;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;
using System.Text;

namespace SistemaPlanificacionSNP.PlanificacionInstitucional.Api
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
				?? throw new InvalidOperationException("Cadena de conexion 'DefaultConnection' no configurada");

			var jwtSettings = BuildJwtSettings(builder.Configuration);

			builder.Services.AddDbContext<PlanificacionInstitucionalDbContext>(options =>
			{
				options.UseSqlServer(connectionString, sqlOptions =>
				{
					sqlOptions.MigrationsAssembly("SistemaPlanificacionSNP.Infrastructure");
					sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
				});

				if (builder.Environment.IsDevelopment())
				{
					options.EnableDetailedErrors();
					options.EnableSensitiveDataLogging();
				}
			});

			builder.Services.AddSingleton(jwtSettings);
			builder.Services.AddAutoMapper(config =>
			{
				config.AddProfile<MappingProfile>();
			});

			builder.Services.AddScoped<IPlanificacionInstitucionalUnitOfWork, PlanificacionInstitucionalUnitOfWork>();
			builder.Services.AddScoped<IPlanesEstrategicoPiRepository, PlanesEstrategicoPiRepository>();
			builder.Services.AddScoped<IProyectosInversionPiRepository, ProyectosInversionPiRepository>();
			builder.Services.AddScoped<IPlanesEstrategicosPiService, PlanesEstrategicosPiService>();
			builder.Services.AddScoped<IProyectosInversionPiService, ProyectosInversionPiService>();

			builder.Services
				.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
				});

			builder.Services.AddAuthorization();
			builder.Services.AddControllers();
			builder.Services.AddFluentValidationAutoValidation();
			builder.Services.AddValidatorsFromAssemblyContaining<Program>();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(options =>
			{
				options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
				{
					Title = "Planificacion Institucional API - Sistema de Planificacion SNP",
					Version = "v1",
					Description = "API para gestion de planes estrategicos y proyectos de inversion"
				});

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
						Array.Empty<string>()
					}
				});
			});

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

			var app = builder.Build();

			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI(c =>
				{
					c.SwaggerEndpoint("/swagger/v1/swagger.json", "Planificacion Institucional API v1");
					c.RoutePrefix = string.Empty;
				});
			}

			app.UseHttpsRedirection();
			app.UseCors("AllowFrontend");
			app.UseAuthentication();
			app.UseAuthorization();

			app.Use(async (context, next) =>
			{
				try
				{
					await next();
				}
				catch (Exception ex)
				{
					var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
					logger.LogError(ex, "Excepcion no manejada");

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

			app.MapControllers();
			app.MapGet("/health", () => new
			{
				status = "healthy",
				service = "PlanificacionInstitucional",
				timestamp = DateTime.UtcNow
			});

			app.Run();
		}

		private static JwtSettings BuildJwtSettings(IConfiguration configuration)
		{
			var jwtSection = configuration.GetSection("Jwt");
			var settings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

			// Backward compatibility with existing appsettings key names.
			if (string.IsNullOrWhiteSpace(settings.SecretKey))
			{
				settings.SecretKey = jwtSection["SecretKey"] ?? jwtSection["Key"] ?? string.Empty;
			}

			if (string.IsNullOrWhiteSpace(settings.Issuer))
			{
				settings.Issuer = jwtSection["Issuer"] ?? "SistemaPlanificacionSNP";
			}

			if (string.IsNullOrWhiteSpace(settings.Audience))
			{
				settings.Audience = jwtSection["Audience"] ?? "SistemaPlanificacionSNPUsers";
			}

			if (settings.ExpirationMinutes <= 0)
			{
				settings.ExpirationMinutes = int.TryParse(jwtSection["ExpirationMinutes"] ?? jwtSection["ExpireMinutes"], out var minutes)
					? minutes
					: 60;
			}

			if (settings.RefreshTokenExpirationDays <= 0)
			{
				settings.RefreshTokenExpirationDays = int.TryParse(jwtSection["RefreshTokenExpirationDays"] ?? jwtSection["RefreshTokenExpireDays"], out var days)
					? days
					: 7;
			}

			if (string.IsNullOrWhiteSpace(settings.SecretKey))
			{
				throw new InvalidOperationException("Configuracion JWT invalida: falta SecretKey/Key en appsettings.");
			}

			return settings;
		}
	}
}