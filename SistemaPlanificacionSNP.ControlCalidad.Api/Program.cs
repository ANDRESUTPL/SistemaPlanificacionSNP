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

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Cadena de conexión 'DefaultConnection' no configurada");

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings
{
	SecretKey = "eG95U3VyZ3BKSFFXbTV6S2RzN3Z3YlhjTjVuN3BYM2I4Y0RvY0E0bXpVbz0",
	Issuer = "SistemaPlanificacionSNP",
	Audience = "SistemaPlanificacionSNP",
	ExpirationMinutes = 60,
	RefreshTokenExpirationDays = 7
};

builder.Services.AddDbContext<ControlCalidadDbContext>(options =>
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

builder.Services.AddAutoMapper(config =>
{
	config.AddProfile<MappingProfile>();
});

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IControlCalidadUnitOfWork, ControlCalidadUnitOfWork>();
builder.Services.AddScoped<IRevisioneRepository, RevisioneRepository>();
builder.Services.AddScoped<IControlCalidadAuditoriaRepository, ControlCalidadAuditoriaRepository>();
builder.Services.AddScoped<IRevisioneControlCalidadService, RevisioneControlCalidadService>();
builder.Services.AddScoped<IAuditoriaControlCalidadService, AuditoriaControlCalidadService>();

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
	});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
	{
		Title = "ControlCalidad API - Sistema de Planificación SNP",
		Version = "v1",
		Description = "API para gestión de revisiones y auditorías de control de calidad"
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
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "ControlCalidad API v1");
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

app.MapControllers();
app.MapGet("/health", () => new { status = "healthy", service = "ControlCalidad", timestamp = DateTime.UtcNow });

app.Run();