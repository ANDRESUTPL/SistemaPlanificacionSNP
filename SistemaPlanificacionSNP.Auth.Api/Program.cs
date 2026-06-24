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

// ==================== CONFIGURACIÓN ====================

// Obtener cadena de conexión
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Cadena de conexión 'DefaultConnection' no configurada");

// Obtener configuración JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings
{
    SecretKey = "super-secret-key-change-in-production-this-must-be-at-least-32-chars",
    Issuer = "SistemaPlanificacionSNP",
    Audience = "SistemaPlanificacionSNP",
    ExpirationMinutes = 60,
    RefreshTokenExpirationDays = 7
};

// ==================== SERVICIOS DE INFRAESTRUCTURA ====================

// Entity Framework DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)
    )
);

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// JWT Settings y Token Generator
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Servicios de negocio
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();

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
                    context.Response.Headers.Add("Token-Expired", "true");
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
        policy.WithOrigins("http://localhost:3000", "https://localhost:7010", "http://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ==================== APLICACIÓN ====================

var app = builder.Build();

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
