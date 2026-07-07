using System.ComponentModel.DataAnnotations;

namespace SistemaPlanificacionSNP.Web.Models
{
    public class UsuarioListItemViewModel
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<RolOptionViewModel> Roles { get; set; } = new();
    }

    public class UsuariosIndexViewModel
    {
        public List<UsuarioListItemViewModel> Usuarios { get; set; } = new();
        public string? Buscar { get; set; }
        public bool? Activo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; } = 1;
    }

    public class UsuarioCreateViewModel
    {
        [Display(Name = "Usuario")]
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede superar los 50 caracteres.")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Display(Name = "Correo")]
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo ingresado no es valido.")]
        [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Contrasena")]
        [Required(ErrorMessage = "La contrasena es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Apellido")]
        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
        public string Apellido { get; set; } = string.Empty;
    }

    public class UsuarioEditViewModel
    {
        public int UsuarioId { get; set; }

        [Display(Name = "Usuario")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Display(Name = "Correo")]
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo ingresado no es valido.")]
        [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Apellido")]
        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }
        public List<RolOptionViewModel> Roles { get; set; } = new();
        public List<PantallaPermisoResumenViewModel> PermisosPorPantalla { get; set; } = new();
    }

    public class UsuarioDetailViewModel
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<RolOptionViewModel> Roles { get; set; } = new();
        public List<PantallaPermisoResumenViewModel> PermisosPorPantalla { get; set; } = new();
    }

    public class UsuarioCreateApiDto
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
    }

    public class UsuarioUpdateApiDto
    {
        public string Email { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public bool? Activo { get; set; }
    }
}
