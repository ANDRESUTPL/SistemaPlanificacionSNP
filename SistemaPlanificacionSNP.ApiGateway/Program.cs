using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Agregar configuración de Ocelot
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", true, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
    .AddJsonFile("ocelot.json", true, true)
    .AddEnvironmentVariables();

// Obtener configuración JWT desde appsettings
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"]
    ?? jwtSection["Key"]
    ?? throw new InvalidOperationException("Jwt SecretKey/Key no configurada");
var issuer = jwtSection["Issuer"] ?? "SistemaPlanificacionSNP";
var audience = jwtSection["Audience"] ?? "SistemaPlanificacionSNP";

// Registrar Ocelot
builder.Services.AddOcelot(builder.Configuration);

// Configurar autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
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
                Console.WriteLine($"Token validado para usuario: {context.Principal?.FindFirst("unique_name")?.Value}");
                return Task.CompletedTask;
            }
        };
    });

// Agregar servicios adicionales
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:7010")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

var app = builder.Build();

// Middleware
app.UseRouting();

// CORS
app.UseCors("AllowFrontend");

// Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Middleware de error personalizado
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
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

// Ocelot middleware
await app.UseOcelot();

app.Run();
