using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaPlanificacionSNP.Infrastructure;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.JWT;
using SistemaPlanificacionSNP.Infrastructure.Mapping;
using SistemaPlanificacionSNP.Infrastructure.Services; // NECESARIO PARA LOS SERVICIOS
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Cadena de conexión 'DefaultConnection' no configurada");

var jwtSettings = ResolveJwtSettings(builder.Configuration);

// 1. REGISTRO DE BASE DE DATOS
builder.Services.AddDbContext<MacroPlanificacionDbContext>(options =>
{
	options.UseSqlServer(connectionString, sqlOptions =>
	{
		sqlOptions.MigrationsAssembly("SistemaPlanificacionSNP.Infrastructure");
		sqlOptions.EnableRetryOnFailure(3);
	});
});

// 2. REGISTRO DE REPOSITORIOS Y UNIT OF WORK (Extension de Infraestructura)
builder.Services.AddMacroPlanificacionServices();

// 3. REGISTRO EXPLÍCITO DE SERVICIOS (LA SOLUCIÓN AL ERROR)
builder.Services.AddScoped<IMacroPlanNacionalService, MacroPlanNacionalService>();
builder.Services.AddScoped<IMacroObjetivoEstrategicoService, MacroObjetivoEstrategicoService>();

// 4. AUTOMAPPER
builder.Services.AddAutoMapper(config =>
{
	config.AddProfile<MappingProfile>();
});

// 5. JWT Y AUTENTICACIÓN
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
		Title = "MacroPlanificación API - Sistema de Planificación SNP",
		Version = "v1"
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
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "MacroPlanificación API v1");
		c.RoutePrefix = string.Empty;
	});
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => new { status = "healthy", service = "MacroPlanificacion", timestamp = DateTime.UtcNow });

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