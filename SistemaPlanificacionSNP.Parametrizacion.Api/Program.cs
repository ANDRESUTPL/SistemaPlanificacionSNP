using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SistemaPlanificacionSNP.Infrastructure;
using SistemaPlanificacionSNP.Infrastructure.JWT;
using SistemaPlanificacionSNP.Infrastructure.Mapping;
using SistemaPlanificacionSNP.Parametrizacion.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? builder.Configuration.GetConnectionString("SqlServer")
	?? throw new InvalidOperationException("Cadena de conexión no configurada");

var jwtSettings = ResolveJwtSettings(builder.Configuration);

// Registra DbContext (ApplicationDbContext), Repositorios, UnitOfWork y Servicios Base
builder.Services.AddInfrastructureServices(connectionString);

// Registra los servicios específicos de esta API
builder.Services.AddScoped<IParametrizacionService, ParametrizacionService>();

builder.Services.AddAutoMapper(config =>
{
	config.AddProfile<MappingProfile>();
});

builder.Services.AddSingleton(jwtSettings);

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
			}
		};
	});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
	{
		Title = "Parametrización API - Sistema de Planificación SNP",
		Version = "v1",
		Description = "API para gestión de catálogos, periodos y entidades públicas"
	});

	// Configuración para poder ingresar el Token JWT en Swagger
	options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
	{
		Description = "JWT Authorization header usando el esquema Bearer. Escribe 'Bearer ' seguido de tu token.",
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
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "Parametrización API v1");
		// ¡ESTA ES LA LÍNEA MÁGICA QUE SOLUCIONA TU ERROR DE CARGA EN LA RAÍZ!
		c.RoutePrefix = string.Empty;
	});
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => new { status = "healthy", service = "Parametrizacion", timestamp = DateTime.UtcNow }).AllowAnonymous();

app.Run();

static JwtSettings ResolveJwtSettings(IConfiguration configuration)
{
	var section = configuration.GetSection("Jwt");

	var secret = section["SecretKey"] ?? section["Key"];
	var issuer = section["Issuer"];
	var audience = section["Audience"];

	var expiration = ParseInt(section["ExpirationMinutes"])
		?? ParseInt(section["ExpireMinutes"])
		?? 60;

	var refreshDays = ParseInt(section["RefreshTokenExpirationDays"])
		?? ParseInt(section["RefreshTokenExpireDays"])
		?? 7;

	if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
	{
		throw new InvalidOperationException("La configuración JWT es incompleta en appsettings");
	}

	return new JwtSettings
	{
		SecretKey = secret,
		Issuer = issuer,
		Audience = audience,
		ExpirationMinutes = expiration,
		RefreshTokenExpirationDays = refreshDays
	};
}

static int? ParseInt(string? value)
{
	if (int.TryParse(value, out var parsed))
	{
		return parsed;
	}
	return null;
}