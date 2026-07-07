using System.ComponentModel.DataAnnotations;

namespace SistemaPlanificacionSNP.Web.Models
{
    public class ApiEnvelope<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    public class UsuarioApiDto
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<RolApiDto> Roles { get; set; } = new();
    }

    public class RolApiDto
    {
        public int RolId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public List<PermisoApiDto> Permisos { get; set; } = new();
    }

    public class PermisoApiDto
    {
        public int PermisoId { get; set; }
        public int PantallaId { get; set; }
        public string CodigoPermiso { get; set; } = string.Empty;
        public string NombrePantalla { get; set; } = string.Empty;
        public bool Lectura { get; set; }
        public bool Creacion { get; set; }
        public bool Edicion { get; set; }
        public bool Eliminacion { get; set; }
    }

    public class AsignarRolesApiDto
    {
        public List<int> RolIds { get; set; } = new();
    }

    public class RolOptionViewModel
    {
        public int RolId { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class RolesUsuariosEditViewModel
    {
        public int UsuarioId { get; set; }

        [Display(Name = "Usuario")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Display(Name = "Nombre")]
        public string NombreCompleto { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; }

        [Display(Name = "Roles asignados")]
        public List<int> SelectedRolIds { get; set; } = new();

        public List<RolOptionViewModel> RolesDisponibles { get; set; } = new();
        public List<PantallaPermisoResumenViewModel> PermisosPorPantalla { get; set; } = new();
        public List<RolPermisosClienteViewModel> RolesPermisosCliente { get; set; } = new();
    }

    public class RolPermisosClienteViewModel
    {
        public int RolId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public List<PermisoApiDto> Permisos { get; set; } = new();
    }

    public class PantallaPermisoResumenViewModel
    {
        public int PantallaId { get; set; }
        public string PantallaNombre { get; set; } = string.Empty;
        public bool Lectura { get; set; }
        public bool Creacion { get; set; }
        public bool Edicion { get; set; }
        public bool Eliminacion { get; set; }
    }
}
