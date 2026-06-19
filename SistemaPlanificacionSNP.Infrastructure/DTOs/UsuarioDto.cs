namespace SistemaPlanificacionSNP.Infrastructure.DTOs
{
    /// <summary>
    /// DTO para respuestas de Usuario (sin exponer PasswordHash)
    /// </summary>
    public class UsuarioDto
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = null!;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<RolDto> Roles { get; set; } = new();
    }

    /// <summary>
    /// DTO para crear usuario
    /// </summary>
    public class UsuarioCreateDto
    {
        public string NombreUsuario { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = null!;
    }

    /// <summary>
    /// DTO para actualizar usuario
    /// </summary>
    public class UsuarioUpdateDto
    {
        public string Email { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = null!;
        public bool? Activo { get; set; }
    }

    /// <summary>
    /// DTO para cambiar contraseña
    /// </summary>
    public class CambiarPasswordDto
    {
        public string PasswordActual { get; set; } = null!;
        public string PasswordNueva { get; set; } = null!;
        public string PasswordConfirmar { get; set; } = null!;
    }

    /// <summary>
    /// DTO para login
    /// </summary>
    public class LoginDto
    {
        public string NombreUsuario { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool Recuerdame { get; set; }
    }

    /// <summary>
    /// DTO para respuesta de login (incluye tokens)
    /// </summary>
    public class LoginResponseDto
    {
        public UsuarioDto Usuario { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime AccessTokenExpiration { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
    }

    /// <summary>
    /// DTO para refrescar token
    /// </summary>
    public class RefreshTokenDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }

    /// <summary>
    /// DTO para respuesta de refresh token
    /// </summary>
    public class RefreshTokenResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime AccessTokenExpiration { get; set; }
    }
}
