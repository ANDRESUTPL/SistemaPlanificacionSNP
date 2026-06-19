using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;

namespace SistemaPlanificacionSNP.Infrastructure.JWT
{
    /// <summary>
    /// Servicio para generar y validar JWT tokens
    /// </summary>
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(Usuario usuario, List<Rol> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        DateTime GetTokenExpiration(string token);
    }

    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<JwtTokenGenerator> _logger;

        public JwtTokenGenerator(JwtSettings jwtSettings, ILogger<JwtTokenGenerator> logger)
        {
            _jwtSettings = jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Genera un JWT Access Token con información del usuario y roles
        /// </summary>
        public string GenerateAccessToken(Usuario usuario, List<Rol> roles)
        {
            try
            {
                var secretKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
                var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                    new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim("Nombre", usuario.Nombre),
                    new Claim("Apellido", usuario.Apellido),
                };

                // Agregar roles como claims
                foreach (var rol in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, rol.Nombre));
                    
                    // Agregar permisos específicos del rol
                    foreach (var permiso in rol.RolPermisos)
                    {
                        claims.Add(new Claim("Permiso", $"{permiso.PantallaId}:{permiso.Pantalla.Nombre}"));
                        
                        if (permiso.Lectura)
                            claims.Add(new Claim($"Lectura_{permiso.PantallaId}", "true"));
                        if (permiso.Creacion)
                            claims.Add(new Claim($"Creacion_{permiso.PantallaId}", "true"));
                        if (permiso.Edicion)
                            claims.Add(new Claim($"Edicion_{permiso.PantallaId}", "true"));
                        if (permiso.Eliminacion)
                            claims.Add(new Claim($"Eliminacion_{permiso.PantallaId}", "true"));
                    }
                }

                var token = new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                    signingCredentials: credentials
                );

                var tokenHandler = new JwtSecurityTokenHandler();
                string jwtToken = tokenHandler.WriteToken(token);

                _logger.LogInformation($"JWT token generated for user {usuario.NombreUsuario}");
                return jwtToken;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating JWT token: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Genera un Refresh Token aleatorio
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        /// <summary>
        /// Valida un JWT token y retorna los claims si es válido
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = System.Text.Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Token validation failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene la fecha de expiración de un token
        /// </summary>
        public DateTime GetTokenExpiration(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            return jwtToken?.ValidTo ?? DateTime.MinValue;
        }
    }
}
