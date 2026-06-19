using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.JWT;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Auth.Api.Controllers
{
    /// <summary>
    /// Controlador de autenticación y autorización
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHashService _passwordService;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUnitOfWork unitOfWork,
            IPasswordHashService passwordService,
            IJwtTokenGenerator tokenGenerator,
            IAuditoriaService auditoriaService,
            IMapper mapper,
            ILogger<AuthController> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
            _tokenGenerator = tokenGenerator ?? throw new ArgumentNullException(nameof(tokenGenerator));
            _auditoriaService = auditoriaService ?? throw new ArgumentNullException(nameof(auditoriaService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Login con usuario y contraseña
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                // Validar entrada
                if (string.IsNullOrWhiteSpace(loginDto.NombreUsuario) || string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    _logger.LogWarning("Login attempt with missing credentials");
                    return BadRequest(ApiResponse<LoginResponseDto>.FailureWith("Usuario y contraseña requeridos"));
                }

                // Buscar usuario
                var usuario = await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(loginDto.NombreUsuario);
                if (usuario == null)
                {
                    _logger.LogWarning($"Login attempt for non-existent user: {loginDto.NombreUsuario}");
                    return Unauthorized(ApiResponse<LoginResponseDto>.FailureWith("Usuario o contraseña incorrectos"));
                }

                // Verificar que el usuario esté activo
                if (!usuario.Activo)
                {
                    _logger.LogWarning($"Login attempt for inactive user: {loginDto.NombreUsuario}");
                    return Unauthorized(ApiResponse<LoginResponseDto>.FailureWith("Usuario inactivo"));
                }

                // Verificar contraseña
                if (!_passwordService.VerifyPassword(loginDto.Password, usuario.PasswordHash))
                {
                    _logger.LogWarning($"Failed login attempt for user: {loginDto.NombreUsuario}");
                    return Unauthorized(ApiResponse<LoginResponseDto>.FailureWith("Usuario o contraseña incorrectos"));
                }

                // Obtener roles y permisos
                var usuarioConRoles = await _unitOfWork.Usuarios.GetWithRolesAsync(usuario.UsuarioId);
                if (usuarioConRoles == null)
                {
                    return StatusCode(500, ApiResponse<LoginResponseDto>.FailureWith("Error al obtener roles del usuario"));
                }

                // Generar tokens
                var accessToken = _tokenGenerator.GenerateAccessToken(usuarioConRoles, usuarioConRoles.UsuarioRoles.Select(ur => ur.Rol).ToList());
                var refreshToken = _tokenGenerator.GenerateRefreshToken();
                var tokenExpiration = _tokenGenerator.GetTokenExpiration(accessToken);

                // Guardar refresh token en base de datos
                usuarioConRoles.RefreshToken = refreshToken;
                usuarioConRoles.RefreshTokenExpiracion = DateTime.UtcNow.AddDays(7);
                usuarioConRoles.FechaUltimoLogin = DateTime.UtcNow;

                await _unitOfWork.Usuarios.UpdateAsync(usuarioConRoles);
                await _unitOfWork.SaveChangesAsync();

                // Registrar auditoría
                await _auditoriaService.RegistrarActualizacionAsync(
                    usuario.UsuarioId,
                    "Usuario",
                    usuario.UsuarioId,
                    new { FechaUltimoLogin = DateTime.UtcNow },
                    new { FechaUltimoLogin = usuarioConRoles.FechaUltimoLogin }
                );

                // Mapear usuario a DTO
                var usuarioDto = _mapper.Map<UsuarioDto>(usuarioConRoles);

                var response = new LoginResponseDto
                {
                    Usuario = usuarioDto,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpiration = tokenExpiration,
                    RefreshTokenExpiration = usuarioConRoles.RefreshTokenExpiracion ?? DateTime.UtcNow.AddDays(7)
                };

                _logger.LogInformation($"Successful login for user: {loginDto.NombreUsuario}");
                return Ok(ApiResponse<LoginResponseDto>.SuccessWith(response, "Login exitoso"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Login: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<LoginResponseDto>.FailureWith("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Refrescar Access Token usando Refresh Token
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<RefreshTokenResponseDto>>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                // Validar tokens
                if (string.IsNullOrWhiteSpace(refreshTokenDto.AccessToken) || string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
                {
                    return BadRequest(ApiResponse<RefreshTokenResponseDto>.FailureWith("Tokens requeridos"));
                }

                // Validar access token (aunque esté expirado)
                var principal = _tokenGenerator.ValidateToken(refreshTokenDto.AccessToken);
                if (principal == null)
                {
                    // Intentar validar sin considerar expiración
                    // En un caso real, se debería implementar una validación más permisiva
                    return Unauthorized(ApiResponse<RefreshTokenResponseDto>.FailureWith("Token inválido"));
                }

                // Obtener UsuarioId del token
                var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ApiResponse<RefreshTokenResponseDto>.FailureWith("Token inválido"));
                }

                // Buscar usuario y validar refresh token
                var usuario = await _unitOfWork.Usuarios.GetByIdAsync(userId);
                if (usuario == null || usuario.RefreshToken != refreshTokenDto.RefreshToken || usuario.RefreshTokenExpiracion < DateTime.UtcNow)
                {
                    return Unauthorized(ApiResponse<RefreshTokenResponseDto>.FailureWith("Refresh token inválido o expirado"));
                }

                // Obtener roles
                var usuarioConRoles = await _unitOfWork.Usuarios.GetWithRolesAsync(userId);
                if (usuarioConRoles == null)
                {
                    return StatusCode(500, ApiResponse<RefreshTokenResponseDto>.FailureWith("Error al obtener usuario"));
                }

                // Generar nuevo access token
                var newAccessToken = _tokenGenerator.GenerateAccessToken(usuarioConRoles, usuarioConRoles.UsuarioRoles.Select(ur => ur.Rol).ToList());
                var newRefreshToken = _tokenGenerator.GenerateRefreshToken();
                var tokenExpiration = _tokenGenerator.GetTokenExpiration(newAccessToken);

                // Actualizar refresh token
                usuarioConRoles.RefreshToken = newRefreshToken;
                usuarioConRoles.RefreshTokenExpiracion = DateTime.UtcNow.AddDays(7);

                await _unitOfWork.Usuarios.UpdateAsync(usuarioConRoles);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Refresh token granted for user: {usuarioConRoles.NombreUsuario}");

                var response = new RefreshTokenResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    AccessTokenExpiration = tokenExpiration
                };

                return Ok(ApiResponse<RefreshTokenResponseDto>.SuccessWith(response, "Token refrescado"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in RefreshToken: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<RefreshTokenResponseDto>.FailureWith("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Logout (invalidar refresh token)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> Logout()
        {
            try
            {
                // Obtener UsuarioId del token JWT
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ApiResponse<string>.FailureWith("Token inválido"));
                }

                // Buscar usuario y invalidar refresh token
                var usuario = await _unitOfWork.Usuarios.GetByIdAsync(userId);
                if (usuario == null)
                {
                    return NotFound(ApiResponse<string>.FailureWith("Usuario no encontrado"));
                }

                usuario.RefreshToken = null;
                usuario.RefreshTokenExpiracion = null;

                await _unitOfWork.Usuarios.UpdateAsync(usuario);
                await _unitOfWork.SaveChangesAsync();

                // Registrar auditoría
                await _auditoriaService.RegistrarActualizacionAsync(
                    userId,
                    "Usuario",
                    userId,
                    new { RefreshToken = "removed" },
                    new { RefreshToken = (string?)null }
                );

                _logger.LogInformation($"Logout for user: {usuario.NombreUsuario}");
                return Ok(ApiResponse<string>.Succeeded("Logout exitoso"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Logout: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<string>.FailureWith("Error interno del servidor"));
            }
        }

        /// <summary>
        /// Cambiar contraseña (requiere autenticación)
        /// </summary>
        [HttpPost("cambiar-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> CambiarPassword([FromBody] CambiarPasswordDto cambiarPasswordDto)
        {
            try
            {
                // Obtener UsuarioId del token JWT
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(ApiResponse<string>.FailureWith("Token inválido"));
                }

                // Validar entrada
                if (string.IsNullOrWhiteSpace(cambiarPasswordDto.PasswordActual) ||
                    string.IsNullOrWhiteSpace(cambiarPasswordDto.PasswordNueva) ||
                    string.IsNullOrWhiteSpace(cambiarPasswordDto.PasswordConfirmar))
                {
                    return BadRequest(ApiResponse<string>.FailureWith("Todos los campos son requeridos"));
                }

                if (cambiarPasswordDto.PasswordNueva != cambiarPasswordDto.PasswordConfirmar)
                {
                    return BadRequest(ApiResponse<string>.FailureWith("Las contraseñas nuevas no coinciden"));
                }

                if (cambiarPasswordDto.PasswordNueva.Length < 8)
                {
                    return BadRequest(ApiResponse<string>.FailureWith("La contraseña debe tener al menos 8 caracteres"));
                }

                // Buscar usuario
                var usuario = await _unitOfWork.Usuarios.GetByIdAsync(userId);
                if (usuario == null)
                {
                    return NotFound(ApiResponse<string>.FailureWith("Usuario no encontrado"));
                }

                // Verificar contraseña actual
                if (!_passwordService.VerifyPassword(cambiarPasswordDto.PasswordActual, usuario.PasswordHash))
                {
                    _logger.LogWarning($"Failed password change attempt for user: {usuario.NombreUsuario}");
                    return BadRequest(ApiResponse<string>.FailureWith("Contraseña actual incorrecta"));
                }

                // Actualizar contraseña
                usuario.PasswordHash = _passwordService.HashPassword(cambiarPasswordDto.PasswordNueva);

                await _unitOfWork.Usuarios.UpdateAsync(usuario);
                await _unitOfWork.SaveChangesAsync();

                // Registrar auditoría
                await _auditoriaService.RegistrarActualizacionAsync(
                    userId,
                    "Usuario",
                    userId,
                    new { Campo = "PasswordHash" },
                    new { Campo = "PasswordHash" }
                );

                _logger.LogInformation($"Password changed for user: {usuario.NombreUsuario}");
                return Ok(ApiResponse<string>.Succeeded("Contraseña actualizada exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CambiarPassword: {ex.Message}", ex);
                return StatusCode(500, ApiResponse<string>.FailureWith("Error interno del servidor"));
            }
        }
    }
}
