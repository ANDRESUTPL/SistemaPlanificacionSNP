using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SistemaPlanificacionSNP.Infrastructure.JWT
{
    /// <summary>
    /// Extensión centralizada para configurar autenticación JWT con transformación de claims
    /// en todos los microservicios del sistema SNP.
    /// 
    /// Características:
    /// - Mantiene tokens compactos con claims "P" (formato: "13:LCED")
    /// - Transforma claims "P" a formato expandido en OnTokenValidated (Lectura_13, Creacion_13, etc.)
    /// - Centraliza configuración JWT repetida en 5 microservicios
    /// - Maneja SecurityTokenExpiredException y propaga encabezados de expiración
    /// </summary>
    public static class JwtAuthenticationExtensions
    {
        /// <summary>
        /// Registra autenticación JWT con transformación automática de claims.
        /// Debe llamarse DESPUÉS de registrar JwtSettings en DI.
        /// 
        /// Uso en Program.cs:
        /// var jwtSettings = ResolveJwtSettings(builder.Configuration);
        /// builder.Services.AddSingleton(jwtSettings);
        /// builder.Services.AddSnpJwtAuthentication(jwtSettings);  // ← ESTA LÍNEA
        /// </summary>
        public static IServiceCollection AddSnpJwtAuthentication(
            this IServiceCollection services,
            JwtSettings jwtSettings)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (jwtSettings == null)
                throw new ArgumentNullException(nameof(jwtSettings));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // Configuración estándar de validación de token
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    // Manejadores de eventos para transformación y errores
                    options.Events = new JwtBearerEvents
                    {
                        // OnAuthenticationFailed: Manejo de excepciones de seguridad
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                context.Response.Headers["Token-Expired"] = "true";
                            }
                            return Task.CompletedTask;
                        },

                        // OnTokenValidated: Transformación de claims "P" a formato expandido
                        // Aquí es donde ocurre la magia: convertimos claims compactos
                        // "P": "13:LCED" → Lectura_13, Creacion_13, Edicion_13, Eliminacion_13
                        OnTokenValidated = context =>
                        {
                            var identity = context.Principal?.Identity as ClaimsIdentity;
                            if (identity == null)
                                return Task.CompletedTask;

                            // Obtener todos los claims "P" (formato comprimido)
                            var pClaims = identity.FindAll("P").ToList();

                            foreach (var pClaim in pClaims)
                            {
                                // Formato esperado: "13:LCED" donde:
                                // - 13 = PantallaId
                                // - L = Lectura, C = Creacion, E = Edicion, D = Eliminacion (Delete)
                                var parts = pClaim.Value.Split(':');
                                if (parts.Length != 2)
                                    continue;

                                var pantallaId = parts[0];  // "13"
                                var flags = parts[1];       // "LCED"

                                // Agregar claims expandidos según los flags presentes
                                if (flags.Contains('L'))
                                    identity.AddClaim(new Claim($"Lectura_{pantallaId}", "true"));

                                if (flags.Contains('C'))
                                    identity.AddClaim(new Claim($"Creacion_{pantallaId}", "true"));

                                if (flags.Contains('E'))
                                    identity.AddClaim(new Claim($"Edicion_{pantallaId}", "true"));

                                if (flags.Contains('D'))
                                    identity.AddClaim(new Claim($"Eliminacion_{pantallaId}", "true"));
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }
    }
}
