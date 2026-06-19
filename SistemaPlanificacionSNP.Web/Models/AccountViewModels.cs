namespace SistemaPlanificacionSNP.Web.Models
{
    public class LoginViewModel
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool Recuerdame { get; set; }
    }

    public class ChangePasswordViewModel
    {
        public string PasswordActual { get; set; } = string.Empty;
        public string PasswordNueva { get; set; } = string.Empty;
        public string PasswordConfirmar { get; set; } = string.Empty;
    }

    public class UserProfileViewModel
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
